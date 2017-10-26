using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skyward.Popcorn
{
    /// <summary>
    ///     A generic response object
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        ///     Whether or not the method succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     The generic response object provided on success.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        ///     If the method did not succeed, the type name of the exception.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        ///     If the method did not succeed, the human-readable message of the error returned.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// If the method did not succeed, this may contain additional details for debugging or reporting purposes.
        /// </summary>
        public string ErrorDetails { get; set; }
    }
}
