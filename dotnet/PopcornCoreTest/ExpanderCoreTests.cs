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
        Expander _expander;

        [TestInitialize]
        public void Setup()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);

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

        [TestMethod, Ignore]
        public void TopLevelCollection()
        {
            // This should test the DatabaseObjectList property
        }
    }
}
