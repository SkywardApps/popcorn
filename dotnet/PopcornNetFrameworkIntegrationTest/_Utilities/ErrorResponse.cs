using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PopcornNetCoreExampleIntegrationTest._Utilities
{
    public class ErrorResponse
    {
        // A wrapper for our default error response to use during testing
        public bool Success { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public string ErrorDetails { get; set; }
    }
}
