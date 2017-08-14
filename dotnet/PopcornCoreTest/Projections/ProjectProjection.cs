
using System;
using System.Collections.Generic;
using System.Linq;

namespace PopcornCoreTest.Projections
{
    public class ProjectProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        

        public List<SectionProjection> Sections { get; set; }
        public List<EnvironmentProjection> Environments { get; set; }
        public List<CredentialDefinitionProjection> CredentialDefinitions { get; set; }
    }
}
