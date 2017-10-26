using System;
using System.Collections.Generic;
using System.Text;

namespace PopcornNetStandardTest.Models
{
    public class CredentialDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }

        // FK
        public Guid CredentialTypeId { get; set; }
        public Guid ProjectId { get; set; }

        // Nav
        public virtual CredentialType Type { get; set; }
        public virtual Project Project { get; set; }
    }
}
