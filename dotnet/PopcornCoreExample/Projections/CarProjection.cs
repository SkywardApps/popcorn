using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornCoreExample.Projections
{
    public class CarProjection
    {
        [Skyward.Popcorn.DefaultIncludes("[FullName]")]
        public EmployeeProjection Owner { get; set; }

        [IncludeByDefault]
        public string Model { get; set; }
        public string Make { get; set; }
        public int? Year { get; set; }
        public string Color { get; set; }
    }
}
