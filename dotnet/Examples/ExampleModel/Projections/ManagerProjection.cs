using ExampleModel.Models;
using Skyward.Popcorn;
using System.Collections.Generic;

namespace ExampleModel.Projections
{
    [ExpandFrom(typeof(Manager))]
    public class ManagerProjection : EmployeeProjection
    {
        [IncludeByDefault]
        public List<EmployeeProjection> Subordinates { get; set; }
    }
}
