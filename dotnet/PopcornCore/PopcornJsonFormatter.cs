using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Buffers;

namespace Skyward.Popcorn.Core
{
    /// <summary>
    /// This formatter provides an interception point on data that is returned by an API method.  It is the point of conversion into the serialized format.
    /// This allows us to affect and control what actually ends up being serialized and returned as a Response.
    /// 
    /// It relies on an existing JsonFormatter which we will pass through to.
    /// </summary>
    internal class PopcornJsonFormatter : TextOutputFormatter
    {
        TextOutputFormatter _innerFormatter;
        Expander _expander;
        Dictionary<string, object> _context;
        Func<object, object, object> _inspector;

        /// <summary>
        /// The only constructor
        /// </summary>
        /// <param name="innerFormatter">the formatter to replace</param>
        /// <param name="expander">The constructed and configured expander</param>
        /// <param name="expandContext">Any context to be passed in</param>
        /// <param name="inspector">Any inspector to wrap around the results.</param>
        public PopcornJsonFormatter(Expander expander, Dictionary<string, object> expandContext = null, Func<object, object, object> inspector = null) :
            base()
        {
            _innerFormatter = new JsonOutputFormatter(
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                },
                ArrayPool<Char>.Shared);

            _expander = expander;
            _context = expandContext;
            _inspector = inspector;

            // Duplicate the underlying supported types and encodings
            foreach (var mediaType in _innerFormatter.SupportedMediaTypes)
                SupportedMediaTypes.Add(mediaType);

            foreach (var encoding in _innerFormatter.SupportedEncodings)
                SupportedEncodings.Add(encoding);

        }

        /// <summary>
        /// Pass through the query to our inner formatter
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return _innerFormatter.CanWriteResult(context);
        }

        /// <summary>
        /// Handle receiving an object and writing a response
        /// </summary>
        /// <param name="context"></param>
        /// <param name="selectedEncoding"></param>
        /// <returns></returns>
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            // See if our expander will replace this object
            var replacementContext = context;
            if (_expander.WillExpand(context.Object))
            {
                // see if we can find some include statements
                string includes = "[]";
                if (context.HttpContext.Request.Query.ContainsKey("include"))
                {
                    includes = context.HttpContext.Request.Query["include"];
                }
                else if (context.HttpContext.Request.Headers?.ContainsKey("API-INCLUDE") ?? false)
                {
                    includes = context.HttpContext.Request.Headers["API-INCLUDE"];
                }

                // Use our expander and expand the object
                var expanded = _expander.Expand(context.Object, _context, PropertyReference.Parse(includes));
                
                // Apply our inspector to the expanded content
                if (_inspector != null)
                    expanded = _inspector(expanded, _context);

                // And create a new context that we'll pass into our inner formatter
                replacementContext = new OutputFormatterWriteContext(
                    context.HttpContext,
                    context.WriterFactory,
                    context.ObjectType,
                    expanded
                    );
            }

            return _innerFormatter.WriteResponseBodyAsync(replacementContext, selectedEncoding);
        }
    }

    /// <summary>
    /// Some useful extensions for Web App style configuration
    /// </summary>
    public static class ApiExpanderJsonFormatterExtensions
    {
        /// <summary>
        /// Configure the AspNet Core MVC options to include an Api Expander.  Allow the caller to configure it with an action.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        public static void UsePopcorn(this Microsoft.AspNetCore.Mvc.MvcOptions options, Action<PopcornConfiguration> configure = null)
        {
            // Inject our Api Expander
            // First we remove the existing one
            var existingJsonFormatter = options.OutputFormatters.First(of => of is JsonOutputFormatter) as JsonOutputFormatter;
            options.OutputFormatters.RemoveType<JsonOutputFormatter>();

            // Create an expander object
            var expander = new Expander();
            var configuration = new PopcornConfiguration(expander);
            // optionally configure this expander
            if (configure != null)
            {
                configure(configuration);
            }

            // And add a Json Formatter that will utilize that expander when appropriate
            options.OutputFormatters.Add(new PopcornJsonFormatter(expander, configuration.Context, configuration.Inspector));
        }
    }
}