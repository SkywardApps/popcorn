using System;
using System.Collections.Generic;
using System.Linq;

namespace PopcornNetStandardTest.Projections
{
    public class CredentialProjection
    {
        public Guid Id { get; set; }
        public Guid DefinitionId { get; set; }
        public Guid EnvironmentId { get; set; }
        public CredentialDefinitionProjection Definition { get; set; }
        public List<CredentialKeyValueProjection> Values { get; set; }
    }
}
