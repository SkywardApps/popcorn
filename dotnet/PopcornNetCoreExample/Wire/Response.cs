using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PopcornCoreExample.Wire
{
    public class Response
    {
        [Required]
        public bool Success { get; set; }

        /// <summary>
        /// @TODO: Document this for the general user so they know to do this.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public object Data { get; set; }
    }
}
