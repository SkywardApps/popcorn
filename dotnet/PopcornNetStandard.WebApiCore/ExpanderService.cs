using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Skyward.Popcorn
{
    /// <summary>
    ///  A wrapper service class to expose an expander along with pertinent configuration items.
    /// </summary>
    public interface IExpanderService
    {

        /// <summary>
        /// The underlying configuration
        /// </summary>
        PopcornConfiguration Config { get; }

        /// <summary>
        /// The configured expander object
        /// </summary>
        Expander Expander { get; }

        /// <summary>
        /// The set context for the expander (provides access to data in expansion methods)
        /// </summary>
        Dictionary<string, object> Context { get; }

        /// <summary>
        /// The inspector assigned for the result
        /// </summary>
        Func<object, object, Exception, object> Inspector { get; }
    }

    /// <summary>
    /// A wrapper service class to expose an expander along with pertinent configuration items.
    /// </summary>
    public class ExpanderService : IExpanderService
    {
        private readonly PopcornConfiguration _config;

        public ExpanderService(IOptions<PopcornConfiguration> config)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));

        }

        /// <summary>
        /// The underlying configuration
        /// </summary>
        public PopcornConfiguration Config => _config;

        /// <summary>
        /// The configured expander object
        /// </summary>
        public Expander Expander => _config.Expander;

        /// <summary>
        /// The set context for the expander (provides access to data in expansion methods)
        /// </summary>
        public Dictionary<string, object> Context => _config.Context;

        /// <summary>
        /// The inspector assigned for the result
        /// </summary>
        public Func<object, object, Exception, object> Inspector => _config.Inspector;
    }
}