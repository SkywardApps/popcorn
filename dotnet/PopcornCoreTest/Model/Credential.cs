using System;
using System.Collections.Generic;
using System.Text;

namespace PopcornCoreTest.Models
{
    public class Credential
    {
        public Guid Id { get; set; }

        // FK
        public Guid DefinitionId { get; set; }
        public Guid EnvironmentId { get; set; }

        // Nav
        public virtual CredentialDefinition Definition { get; set; }
        public virtual Environment Environment { get; set; }
        public List<CredentialKeyValue> Values { get; set; }

        // Convenience

        /// <summary>
        /// Return the values array as a dictionary. Assumes all value keys are unique.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> ValuesAsDictionary()
        {
            if (Values == null)
            {
                return null;
            }

            var dict = new Dictionary<string, string>();
            foreach (var credentialPair in Values)
            {
                dict[credentialPair.Key] = credentialPair.Value;
            }
            return dict;
        }
    }
}
