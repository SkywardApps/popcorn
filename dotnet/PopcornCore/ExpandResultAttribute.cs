using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Buffers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace Skyward.Popcorn.Core
{
    public class ExpandResultAttribute : ActionFilterAttribute
    {
        static Expander _expander;
        static Dictionary<string, object> _context;
        static Func<object, object, object> _inspector;
        public enum SortDirection { Ascending, Descending, Unknown }

        public ExpandResultAttribute() { }

        public ExpandResultAttribute(Expander expander, Dictionary<string, object> expandContext = null, Func<object, object, object> inspector = null) :
            base()
        {
            _expander = expander;
            _context = expandContext;
            _inspector = inspector;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult)
            {
                var resultObject = ((ObjectResult)context.Result).Value;
                if (_expander.WillExpand(resultObject))
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
                    resultObject = _expander.Expand(resultObject, _context, PropertyReference.Parse(includes));
                }

                // Validate sortDirection first to error out before starting if necessary
                var sortDirectionKey = SortDirection.Ascending; // Default value if not sort applied
                if (context.HttpContext.Request.Query.ContainsKey("sortDirection"))
                {
                    // Assign the proper sort value
                    string sortDirectionText = context.HttpContext.Request.Query["sortDirection"];
                    if (sortDirectionText == "ascending")
                    {
                        sortDirectionKey = SortDirection.Ascending;
                    } else if (sortDirectionText == "descending") {
                        sortDirectionKey = SortDirection.Descending;
                    } else
                    {
                        //TODO: Maybe consider making a custom exception here
                        throw new InvalidCastException(sortDirectionText);
                    }
                }

                // Do any sorting as specified
                if (context.HttpContext.Request.Query.ContainsKey("sort") && resultObject != null)
                {
                    resultObject = _expander.Sort(resultObject, context.HttpContext.Request.Query["sort"], (int)sortDirectionKey);
                }

                // Apply our inspector to the expanded content
                if (_inspector != null)
                    resultObject = _inspector(resultObject, _context);

                context.Result = new JsonResult(resultObject,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
            }
            base.OnActionExecuted(context);
        }
    }

    /// <summary>
    /// Some useful extensions for Web App style configuration
    /// </summary>
    public static class PopcornExtensions
    {
        /// <summary>
        /// Configure the AspNet Core MVC options to include an Api Expander.  Allow the caller to configure it with an action.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        public static void UsePopcorn(this MvcOptions options, Action<PopcornConfiguration> configure = null)
        {
            // Create an expander object
            var expander = new Expander();
            var configuration = new PopcornConfiguration(expander);

            // optionally configure this expander
            if (configure != null)
            {
                configure(configuration);
            }

            // Assign a global expander that'll run on all endpoints
            if (configuration.ApplyToAllEndpoints)
            {
                options.Filters.Add(new ExpandResultAttribute(expander, configuration.Context, configuration.Inspector));
            }
        }

    }
}