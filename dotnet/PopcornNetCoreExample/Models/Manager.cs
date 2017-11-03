using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PopcornNetCoreExample.Models
{
    public class Manager : Employee
    {
        public List<Employee> Subordinates { get; set; }
    }
}
