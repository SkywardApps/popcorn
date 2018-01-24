using System;
using System.Collections.Generic;
using System.Text;

namespace PopcornNetStandardTest.Models
{
    public class CredentialKeyValue
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        // FK
        public Guid CredentialId { get; set; }
    }
}
