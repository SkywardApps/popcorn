using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornCoreExample.Projections
{
    public class EmployeeProjection
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }

        public string Birthday { get; set; }
        public int? VacationDays { get; set; }

        public List<CarProjection> Vehicles { get; set; }
    }
}
