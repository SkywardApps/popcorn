using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornCoreExample.Projections
{
    public class CarProjection
    {
        [SubPropertyIncludeByDefault("[FullName,Birthday]")]
        public EmployeeProjection Owner { get; set; }

        public string Model { get; set; }
        public string Make { get; set; }
        public int? Year { get; set; }
        public string Color { get; set; }

        public string User { get; set; }
    }
}
