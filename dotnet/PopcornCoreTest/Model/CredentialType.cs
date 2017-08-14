using System;
using System.Collections.Generic;
using System.Text;

namespace PopcornCoreTest.Models
{
    public class CredentialType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RequiredValues { get; set; }
    }
}
