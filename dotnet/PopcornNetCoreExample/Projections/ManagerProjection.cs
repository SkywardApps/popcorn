using PopcornNetCoreExample.Models;
using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornNetCoreExample.Projections
{
    [ExpandFrom(typeof(Manager))]
    public class ManagerProjection : EmployeeProjection
    {
        [IncludeByDefault]
        public List<EmployeeProjection> Subordinates { get; set; }
    }
}
