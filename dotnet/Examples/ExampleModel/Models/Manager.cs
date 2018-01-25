using System.Collections.Generic;

namespace ExampleModel.Models
{
    public class Manager : Employee
    {
        public List<Employee> Subordinates { get; set; }
    }
}
