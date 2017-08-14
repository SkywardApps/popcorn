using Skyward.Popcorn;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PopcornCoreTest.Models;
using Shouldly;
using PopcornCoreTest.Projections;
using PopcornCoreTest.Utilities;
using Skyward.Popcorn.Core;

namespace PopcornCoreTest
{

    [TestClass]
    public class ExpanderCoreTests
    {
        /// <summary>
        /// A test root object with a myriad of properties used to test direct implementations.
        /// The property names should generally be named for what they are being used to test.
        /// </summary>
        public class RootObject
        {
            public Guid Id { get; set; }
            public Guid? Nullable { get; set; }
            public string StringValue { get; set; }
            public string NonIncluded { get; set; }
            public string ExcludedFromProjection { get; set; }
            public int Upconvert { get; set; }
            public double Downconvert { get; set; }

            public ChildObject Child { get; set; }
            public List<ChildObject> Children { get; set; }
            public IEnumerable<ChildObject> ChildrenInterface { get; set; }
            public HashSet<ChildObject> ChildrenSet { get; set; }
            public DerivedChildObject SubclassInOriginal { get; set; }
            public ChildObject SuperclassInOriginal { get; set; }
            public Guid InvalidCastType { get; set; }

            public string FromMethod() { return nameof(FromMethod); }
            public ChildObject ComplexFromMethod() { return new ChildObject { Id = Guid.NewGuid(), Name = "ComplexFromMethod ChildObject", Description = "This proves that an object returned from a method will also be projected." }; }

            public PopcornCoreTest.Models.Project DatabaseObject { get; set; }
            public IEnumerable<PopcornCoreTest.Models.Project> DatabaseObjectList { get; set; }

        }


        /// <summary>
        /// A sub-entity used to test collections
        /// </summary>
        public class ChildObject
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// A subclass of the child entity so we can test polymorphism
        /// </summary>
        public class DerivedChildObject : ChildObject
        {
            public string ExtendedProperty { get; set; }
        }

        /// <summary>
        /// A projection of our testing class.  This mostly contains the same properties, sometimes with different types.
        /// There will be some values here that are calculated just from 'translators'.
        /// </summary>
        public class RootObjectProjection
        {
            //public string Excluded { get; set; } // this one doesn't exist in the projection
            public string Additional { get; set; } // this one doesn't exist in the root


            public Guid Id { get; set; } // a non-simple type
            public Guid? Nullable { get; set; }
            public string StringValue { get; set; }
            public string NonIncluded { get; set; }

            public double? Upconvert { get; set; } // we sneakily changed the type here
            public int? Downconvert { get; set; }

            public double? ValueFromTranslator { get; set; }
            public ChildObjectProjection ComplexFromTranslator { get; set; }

            public ChildObjectProjection Child { get; set; }
            public List<ChildObjectProjection> Children { get; set; }
            public IEnumerable<ChildObjectProjection> ChildrenInterface { get; set; }
            public HashSet<ChildObjectProjection> ChildrenSet { get; set; }
            public ChildObjectProjection SubclassInOriginal { get; set; }
            public DerivedChildObjectProjection SuperclassInOriginal { get; set; }
            public string InvalidCastType { get; set; }

            public string FromMethod { get; set; }
            public ChildObjectProjection ComplexFromMethod { get; set; }

            public ProjectProjection DatabaseObject { get; set; }
            public IEnumerable<ProjectProjection> DatabaseObjectList { get; set; }
        }

        public class ChildObjectProjection
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

        }

        public class DerivedChildObjectProjection : ChildObjectProjection
        {
            public string ExtendedProperty { get; set; }
        }

        Expander _expander;

        [TestInitialize]
        public void Setup()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            config.Map<RootObject, RootObjectProjection>($"[{nameof(RootObjectProjection.Id)},{nameof(RootObjectProjection.StringValue)},{nameof(RootObjectProjection.NonIncluded)}]",
                (definition) =>
                {
                    definition.Translate(o => o.ValueFromTranslator, () => 5.2);
                });

            config.Map<ChildObject, ChildObjectProjection>();
            config.Map<DerivedChildObject, DerivedChildObjectProjection>();
            config.MapEntityFramework<Project, ProjectProjection, TestModelContext>(TestModelContext.ConfigureOptions());
            config.MapEntityFramework<PopcornCoreTest.Models.Environment, EnvironmentProjection, TestModelContext>(TestModelContext.ConfigureOptions());
            config.MapEntityFramework<Credential, CredentialProjection, TestModelContext>(TestModelContext.ConfigureOptions());
            config.MapEntityFramework<CredentialDefinition, CredentialDefinitionProjection, TestModelContext>(TestModelContext.ConfigureOptions());
            config.MapEntityFramework<CredentialType, CredentialTypeProjection, TestModelContext>(TestModelContext.ConfigureOptions());
            config.MapEntityFramework<CredentialKeyValue, CredentialKeyValueProjection, TestModelContext>(TestModelContext.ConfigureOptions());

            using (var db = new TestModelContext())
            {
                db.Database.EnsureDeleted();
            }

            using (var db = new TestModelContext())
            {
                db.Database.EnsureCreated();
            }
        }

        [TestCleanup]
        public void Teardown()
        {
            using (var db = new TestModelContext())
            {
                db.Database.EnsureDeleted();
            }
        }

        // Database collection navigation property
        [TestMethod]
        public void DatabaseFlatList()
        {
            Guid projectId = ProjectTestUtilities.CreateFullDbHierarchy();

            RootObject root = null;
            using (var db = new TestModelContext())
            {
                root = new RootObject
                {
                    DatabaseObject = db.Projects.Find(projectId)
                };
                root.DatabaseObject.Environments.ShouldBeNull();
            }

            object result = _expander.Expand(root, includes: PropertyReference.Parse($"[{nameof(RootObjectProjection.DatabaseObject)}[{nameof(ProjectProjection.Name)}]]"));
            result.ShouldNotBeNull();


            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.DatabaseObject.ShouldNotBeNull();

            // Verify the properties in this object were projected correctly
            projection.DatabaseObject.Name.ShouldBe(root.DatabaseObject.Name);
            projection.DatabaseObject.Description.ShouldBeNull();

            // And verify the navigation property was retrieved correctly
            projection.DatabaseObject.Environments.ShouldBeNull();
            projection.DatabaseObject.CredentialDefinitions.ShouldBeNull();
            projection.DatabaseObject.Sections.ShouldBeNull();
        }


        // Database collection navigation property
        [TestMethod]
        public void DatabaseNestedList()
        {
            Guid projectId = ProjectTestUtilities.CreateFullDbHierarchy();

            RootObject root = null;
            using (var db = new TestModelContext())
            {
                root = new RootObject
                {
                    DatabaseObject = db.Projects.Find(projectId)
                };
                root.DatabaseObject.Environments.ShouldBeNull();
            }

            var includeValues = $"{nameof(CredentialKeyValueProjection.Key)}";
            var includeCredentials = $"{nameof(CredentialProjection.Values)}[{includeValues}]";
            var includeEnvironments = $"{nameof(EnvironmentProjection.Name)},{nameof(EnvironmentProjection.Credentials)}[{includeCredentials}]";
            var includeCredentialDefinitions = $"{nameof(CredentialDefinitionProjection.Name)}";
            var includeProject = $"{nameof(ProjectProjection.Name)},{nameof(ProjectProjection.CredentialDefinitions)}[{includeCredentialDefinitions}],{nameof(ProjectProjection.Environments)}[{includeEnvironments}]";
            object result = _expander.Expand(root, includes: PropertyReference.Parse($"[{nameof(RootObjectProjection.DatabaseObject)}[{includeProject}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.DatabaseObject.ShouldNotBeNull();

            // Verify the properties in this object were projected correctly
            projection.DatabaseObject.Name.ShouldBe(root.DatabaseObject.Name);
            projection.DatabaseObject.Description.ShouldBeNull();

            // And verify each navigation property was retrieved correctly
            projection.DatabaseObject.CredentialDefinitions.ShouldNotBeNull();
            projection.DatabaseObject.CredentialDefinitions.Count.ShouldBe(1);
            projection.DatabaseObject.CredentialDefinitions.First().Name.ShouldBe("Creds");
            projection.DatabaseObject.CredentialDefinitions.First().DisplayName.ShouldBeNull();

            projection.DatabaseObject.Environments.ShouldNotBeNull();
            projection.DatabaseObject.Environments.Count.ShouldBe(1);
            projection.DatabaseObject.Environments.First().Name.ShouldBe("EnvironmentName");
            projection.DatabaseObject.Environments.First().BaseUrl.ShouldBeNull();

            projection.DatabaseObject.Environments.First().Credentials.ShouldNotBeNull();
            projection.DatabaseObject.Environments.First().Credentials.Count.ShouldBe(1);
            projection.DatabaseObject.Environments.First().Credentials.First().Id.ShouldBe(Guid.Empty);
            projection.DatabaseObject.Environments.First().Credentials.First().Definition.ShouldBeNull();

            projection.DatabaseObject.Environments.First().Credentials.First().Values.ShouldNotBeNull();
            projection.DatabaseObject.Environments.First().Credentials.First().Values.Count.ShouldBe(1);
            projection.DatabaseObject.Environments.First().Credentials.First().Values.First().Key.ShouldBe("Key");
            projection.DatabaseObject.Environments.First().Credentials.First().Values.First().Value.ShouldBeNull();
        }
    }
}
