using System;
using System.Collections.Generic;
using System.Text;

namespace PopcornNetStandardTest.Models
{
    public class Section
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        // FKs
        public Guid ProjectId { get; set; }

        // Nav
        public Project Project { get; set; }
    }
}
