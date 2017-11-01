using PopcornCoreExample.Models;
using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornCoreExample.Projections
{
    public class EmployeeProjection
    {
        [IncludeByDefault]
        public string FirstName { get; set; }
        [IncludeByDefault]
        public string LastName { get; set; }
        public string FullName { get; set; }

        public int SocialSecurityNumber { get; set; }

        public string Birthday { get; set; }
        public int? VacationDays { get; set; }
        public EmploymentType? Employment { get; set; }

        [SubPropertyIncludeByDefault("[Make,Model,Color]")]
        public List<CarProjection> Vehicles { get; set; }
    }
}
