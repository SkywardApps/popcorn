using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopcornSpecTests.Models
{
    public class SampleBasicClass
    {
        public int Int { get; set; } = 51781;
        public byte Byte { get; set; } = 5;
        public string String { get; set; } = nameof(SampleBasicClass);
        public DateTime DateTime { get; set; } = new DateTime(2002, 03, 02, 12, 11, 10);
        public Guid Guid { get; set; } = Guid.Parse("3b03ed39-320a-48bb-aa3a-3d94516cb7af");
    }
}
