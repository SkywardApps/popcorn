using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornCoreTest.Projections
{
    public class EnvironmentProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public List<CredentialProjection> Credentials { get; set; }
        public bool EmailOnError { get; set; }
        public string AdditionalNotifications { get; set; }
        public Guid ProjectId { get; set; }
    }
}
