using Skyward.Popcorn;
using System;
using System.Collections.Generic;

namespace PopcornCoreExample.Models
{
    public class Employee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [InternalOnly(true)]
        public int SocialSecurityNumber { get; set; }

        public DateTimeOffset Birthday { get; set; }
        public EmploymentType Employment { get; set; }
        public int VacationDays { get; set; }

        public List<Car> Vehicles { get; set; }
    }
}
