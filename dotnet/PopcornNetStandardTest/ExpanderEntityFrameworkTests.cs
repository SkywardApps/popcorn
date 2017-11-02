using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using PopcornNetStandardTest.Models;
using Shouldly;
using PopcornNetStandardTest.Projections;
using PopcornNetStandardTest.Utilities;
using Skyward.Popcorn;
using Microsoft.EntityFrameworkCore;

namespace PopcornNetStandardTest
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    [TestClass]
    public class ExpanderEntityFrameworkTests
    {
        Expander _expander;

        [TestInitialize]
        public void Setup()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);

            config.Map<Models.Environment, EnvironmentProjection>(
                config: (configuration) =>
                {
                    configuration.PreparePropertyDbContext<Models.Environment, EnvironmentProjection>("CredentialDefinitionNames",
                        (destType, propertyInfo, sourceType, contextType, dbContext) =>
                        {
                            dbContext.Entry(sourceType).Collection("Credentials").Load();
                            ((TestModelContext)dbContext).Credentials.Where(c => c.Id == sourceType.Id).Include(c => c.Definition).Load();
                        }
                    );
                });
            //config.Map<Credential, CredentialProjection>(
            //    config: (configuration) =>
            //    {
            //        configuration.PreparePropertyDbContext<Credential, CredentialProjection>("Definition",
            //            (destType, propertyInfo, sourceType, contextType, dbContext) =>
            //            {
            //                dbContext.Entry(sourceType).Property("Definition");
            //            }
            //        );
            //    });
            config.MapEntityFramework<Project, ProjectProjection, TestModelContext>(TestModelContext.ConfigureOptions(), null, (definition) => { definition.Translate(o => o.Id, () => Guid.NewGuid()); });
            config.MapEntityFramework<Models.Environment, EnvironmentProjection, TestModelContext>(TestModelContext.ConfigureOptions());
                //,
                //config: (configuration) => {
                //    configuration.PreparePropertyDbContext<Models.Environment, EnvironmentProjection>("CredentialDefinitionNames",
                //        (destType, propertyInfo, sourceType, contextType, dbContext) =>
                //        {
                //            dbContext.Entry(sourceType).Collection("Credentials").Load();
                //        }
                //    );
                //    //configuration.PreparePropertyDbContext<Models.CredentialDefinition, EnvironmentProjection>("CredentialDefinitionNames",
                //    //    (destType, propertyInfo, sourceType, contextType, dbContext) =>
                //    //    {
                //    //        var x = dbContext.Entry(sourceType).Member("CredentialDefinitions").CurrentValue;
                //    //    }
                //    // );
                //});
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
        public void DatabaseSingleItem()
        {
            Guid projectId = ProjectTestUtilities.CreateFullDbHierarchy();
            Project sourceObject = null;

            using (var db = new TestModelContext())
            {
                sourceObject = db.Projects.Find(projectId);
            }
            object result = _expander.Expand(sourceObject, includes: PropertyReference.Parse($"[{nameof(ProjectProjection.Name)}]"));
            result.ShouldNotBeNull();


            ProjectProjection projection = result as ProjectProjection;
            projection.ShouldNotBeNull();

            // Verify the properties in this object were projected correctly
            projection.Name.ShouldBe(sourceObject.Name);
            projection.Description.ShouldBeNull();

            // And verify the navigation property was retrieved correctly
            projection.Environments.ShouldBeNull();
            projection.CredentialDefinitions.ShouldBeNull();
            projection.Sections.ShouldBeNull();
        }


        // Database collection navigation property
        [TestMethod]
        public void DatabaseNestedList()
        {
            Guid projectId = ProjectTestUtilities.CreateFullDbHierarchy();
            Project sourceObject = null;

            using (var db = new TestModelContext())
            {
                sourceObject = db.Projects.Find(projectId);
            }

            var includeValues = $"{nameof(CredentialKeyValueProjection.Key)}";
            var includeCredentials = $"{nameof(CredentialProjection.Values)}[{includeValues}]";
            var includeEnvironments = $"{nameof(EnvironmentProjection.Name)},{nameof(EnvironmentProjection.Credentials)}[{includeCredentials}]";
            var includeCredentialDefinitions = $"{nameof(CredentialDefinitionProjection.Name)},{nameof(CredentialDefinitionProjection.Type)}";
            var includeProject = $"{nameof(ProjectProjection.Name)},{nameof(ProjectProjection.CredentialDefinitions)}[{includeCredentialDefinitions}],{nameof(ProjectProjection.Environments)}[{includeEnvironments}]";
            object result = _expander.Expand(sourceObject, includes: PropertyReference.Parse($"[{includeProject}]"));
            result.ShouldNotBeNull();

            ProjectProjection projection = result as ProjectProjection;
            projection.ShouldNotBeNull();
            projection.ShouldNotBeNull();

            // Verify the properties in this object were projected correctly
            projection.Name.ShouldBe(sourceObject.Name);
            projection.Description.ShouldBeNull();

            // And verify each navigation property was retrieved correctly
            projection.CredentialDefinitions.ShouldNotBeNull();
            projection.CredentialDefinitions.Count.ShouldBe(1);
            projection.CredentialDefinitions.First().Name.ShouldBe("Creds");
            projection.CredentialDefinitions.First().DisplayName.ShouldBeNull();
            projection.CredentialDefinitions.First().Type.ShouldNotBeNull();
            projection.CredentialDefinitions.First().Type.Name.ShouldBe("CredType");

            projection.Environments.ShouldNotBeNull();
            projection.Environments.Count.ShouldBe(1);
            projection.Environments.First().Name.ShouldBe("EnvironmentName");
            projection.Environments.First().BaseUrl.ShouldBeNull();

            projection.Environments.First().Credentials.ShouldNotBeNull();
            projection.Environments.First().Credentials.Count.ShouldBe(1);
            projection.Environments.First().Credentials.First().Id.ShouldBe(Guid.Empty);
            projection.Environments.First().Credentials.First().Definition.ShouldBeNull();

            projection.Environments.First().Credentials.First().Values.ShouldNotBeNull();
            projection.Environments.First().Credentials.First().Values.Count.ShouldBe(1);
            projection.Environments.First().Credentials.First().Values.First().Key.ShouldBe("Key");
            projection.Environments.First().Credentials.First().Values.First().Value.ShouldBeNull();
        }

        // Testing a simple translation on the entity framework expansion
        [TestMethod]
        public void EntityFrameworkMappingConfig()
        {
            Guid projectId = ProjectTestUtilities.CreateFullDbHierarchy();
            Project sourceObject = null;

            using (var db = new TestModelContext())
            {
                sourceObject = db.Projects.Find(projectId);
            }

            var includeProject = $"{nameof(ProjectProjection.Name)},{nameof(ProjectProjection.Id)}";
            object result = _expander.Expand(sourceObject, includes: PropertyReference.Parse($"[{includeProject}]"));
            result.ShouldNotBeNull();

            ProjectProjection projection = result as ProjectProjection;
            projection.ShouldNotBeNull();
            projection.ShouldNotBeNull();

            // Verify the properties in this object were projected correctly
            projection.Name.ShouldBe(sourceObject.Name);
            projection.Description.ShouldBeNull();
        }

        [TestMethod]
        public void EntityFrameworkMappingPlainFunctions()
        {
            Guid projectId = ProjectTestUtilities.CreateFullDbHierarchy();
            Models.Environment sourceObject = null;

            using (var db = new TestModelContext())
            {
                sourceObject = db.Environments.First();
            }

            object result = _expander.Expand(sourceObject, includes: PropertyReference.Parse($"[CredentialDefinitionNames]"));
            result.ShouldNotBeNull();
        }

        [TestMethod, Ignore]
        public void TopLevelCollection()
        {
            // This should test the DatabaseObjectList property
        }
    }
}
