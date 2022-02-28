using ExampleModel.Models;
using Skyward.Popcorn;
using System.Collections.Generic;

namespace ExampleModel.Projections
{
    public class ManagerProjection : EmployeeProjection
    {
        [IncludeByDefault]
        public List<EmployeeProjection> Subordinates { get; set; }
    }
}
