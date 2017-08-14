using PopcornCoreTest.Models;
using System;

namespace PopcornCoreTest.Projections
{
    public class CredentialDefinitionProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Guid CredentialTypeId { get; set; }
        public CredentialTypeProjection Type { get; set; }
        public Guid ProjectId { get; set; }     
    }
}
