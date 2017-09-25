using System.ComponentModel.DataAnnotations;

namespace PopcornCoreExample.Wire
{
    public class Response
    {
        [Required]
        public bool Success { get; set; }

        public object Data { get; set; }
    }
}
