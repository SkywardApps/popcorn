using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornCoreExample.Models
{
    public class InternalFieldClassException
    {
        [InternalOnly(true)]
        public string Field1 { get; set; }
    }
}
