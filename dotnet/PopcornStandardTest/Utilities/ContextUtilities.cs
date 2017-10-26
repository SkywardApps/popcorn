using Microsoft.EntityFrameworkCore;
using PopcornNetStandardTest.Models;
using PopcornNetStandardTest.Projections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace PopcornNetStandardTest.Utilities
{
    /// <summary>
    /// This class contains boiler plate functions and data for testing purposes.
    /// </summary>
    public static class ProjectTestUtilities
    { 
        public static Guid CreateFullDbHierarchy()
        {
            Guid projectId;
            using (var db = new TestModelContext())
            {
                var newProject = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = "ProjectName",
                    Description = "ProjectDescription"
                };
                db.Projects.Add(newProject);
                projectId = newProject.Id;

                var newCredentialType = new CredentialType
                {
                    Id = Guid.NewGuid(),
                    Name = "CredType",
                    RequiredValues = "None"
                };
                db.CredentialTypes.Add(newCredentialType);

                var newCredentialsDefinition = new CredentialDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Creds",
                    DisplayName = "Creds",
                    Project = newProject,
                    Type = newCredentialType,
                };
                db.CredentialDefinitions.Add(newCredentialsDefinition);
                newProject.CredentialDefinitions.Add(newCredentialsDefinition);

                var newEnvironment = new Models.Environment
                {
                    Id = Guid.NewGuid(),
                    Name = "EnvironmentName",
                    Project = newProject,
                    BaseUrl = "https://skywardapps.us"
                };
                db.Environments.Add(newEnvironment);
                newProject.Environments.Add(newEnvironment);

                var newCredentials = new Credential
                {
                    Id = Guid.NewGuid(),
                    Environment = newEnvironment,
                    Definition = newCredentialsDefinition,
                    Values = new List<CredentialKeyValue> {
                        new CredentialKeyValue
                        {
                            Id = Guid.NewGuid(),
                            Key = "Key",
                            Value = "Value",
                        }
                    }
                };
                db.Credentials.Add(newCredentials);
                newEnvironment.Credentials.Add(newCredentials);

                db.SaveChanges();
            }

            return projectId;
        }
    }
}
