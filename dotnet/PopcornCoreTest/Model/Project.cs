using System;
using System.Collections.Generic;
using System.Text;

namespace PopcornCoreTest.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }


        // Nav
        public virtual List<CredentialDefinition> CredentialDefinitions { get; set; }
        public virtual List<Section> Sections { get; set; }
        public virtual List<Environment> Environments { get; set; }
    }
}
