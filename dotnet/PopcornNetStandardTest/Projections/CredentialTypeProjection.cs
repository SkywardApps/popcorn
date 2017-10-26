using System;

namespace PopcornNetStandardTest.Projections
{
    public class CredentialTypeProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RequiredValues { get; set; }
    }
}
