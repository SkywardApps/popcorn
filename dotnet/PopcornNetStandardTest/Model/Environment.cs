using System;
using System.Collections.Generic;
using System.Text;

namespace PopcornNetStandardTest.Models
{
    public class Environment
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string BaseUrl { get; set; }

        public bool EmailOnError { get; set; }

        // List of comma separated emails
        public string AdditionalNotifications { get; set; }

        // FKs
        public Guid ProjectId { get; set; }

        // Nav
        public Project Project { get; set; }
        public virtual List<Credential> Credentials { get; set; }

    }
}
