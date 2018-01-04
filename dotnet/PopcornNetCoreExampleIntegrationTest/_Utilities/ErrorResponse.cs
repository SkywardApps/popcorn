using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PopcornNetCoreExampleIntegrationTest._Utilities
{
    public class ErrorResponse
    {
        // A wrapper for our default error response to use during testing
        [Required]
        public bool Success { get; set; }

        [Required]
        public string ErrorCode { get; set; }

        [Required]
        public string ErrorMessage { get; set; }

        [Required]
        public string ErrorDetails { get; set; }
    }
}
