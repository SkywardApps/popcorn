using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Skyward.Popcorn;

namespace PopcornCoreExample.Models
{
    public class InternalFieldsClass
    {
        [InternalOnly(false)]
        public string Field1 { get; set; }
        public string Field2 { get; set; }

        [InternalOnly(false)]
        public string Method1() { return "method1"; }
    }
}
