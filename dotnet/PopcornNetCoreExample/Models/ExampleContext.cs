using System.Collections.Generic;

namespace PopcornCoreExample.Models
{
    public class ExampleContext
    {
        public List<Car> Cars { get; } = new List<Car>();
        public List<Employee> Employees { get; } = new List<Employee>();
    }
}
