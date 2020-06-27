using Skyward.Popcorn;
using System.Collections.Generic;

namespace ExampleModel.Models
{
    public class Business
    {
        [IncludeByDefault]
        public string Name { get; set; }
        public string StreetAddress { get; set; }
        public int ZipCode { get; set; }

        [IncludeByDefault]
        public List<Employee> Employees { get; set; }

        public enum Purposes
        {
            Shoes,
            Vehicles,
            Clothes,
            Tools
        }
        [IncludeByDefault]
        public Purposes Purpose { get; set; }
    }
}
