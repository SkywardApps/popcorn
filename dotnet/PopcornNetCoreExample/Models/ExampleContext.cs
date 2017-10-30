using System.Collections.Generic;

namespace PopcornCoreExample.Models
{
    public class ExampleContext
    {
        public List<Car> Cars { get; } = new List<Car>();
        public List<Employee> Employees { get; } = new List<Employee>();
        public InternalClass InternalClass { get; } = new InternalClass();
        public InternalFieldsClass InternalFieldsClass { get; } = new InternalFieldsClass
        {
            Field1 = "Field1",
            Field2 = "Field2"
        };
        public InternalFieldClassException InternalFieldClassException { get; } = new InternalFieldClassException
        {
            Field1 = "Field1"
        };
    }
}
