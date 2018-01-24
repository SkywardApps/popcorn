using System.Collections.Generic;

namespace ExampleModel.Models
{
    public class Business
    {
        public string Name { get; set; }
        public string StreetAddress { get; set; }
        public int ZipCode { get; set; }
        
        public List<Employee> Employees { get; set; }

        public enum Purposes
        {
            Shoes,
            Vehicles,
            Clothes,
            Tools
        }
        public Purposes Purpose { get; set; }
    }
}
