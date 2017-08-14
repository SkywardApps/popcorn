using System;
using System.Collections.Generic;
using System.Linq;

namespace PopcornCoreTest.Projections
{
    public class SectionProjection
    {       
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid ProjectId { get; set; } 
    }
}
