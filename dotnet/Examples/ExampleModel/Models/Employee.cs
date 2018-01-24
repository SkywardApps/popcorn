using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExampleModel.Models
{
    public class Employee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [InternalOnly(true)]
        public long SocialSecurityNumber { get; set; }

        public DateTimeOffset Birthday { get; set; }
        public EmploymentType Employment { get; set; }
        public int VacationDays { get; set; }

        public List<Car> Vehicles { get; set; }

        public List<Car> GetInsuredCars() {
            return Vehicles.Where(c => c.Insured == true).ToList();
        }
    }
}
