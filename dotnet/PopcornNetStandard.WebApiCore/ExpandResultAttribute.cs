using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Skyward.Popcorn
{
    public class ExpandActionFilter : IActionFilter
    {
        Expander _expander;
        Dictionary<string, object> _context;
        Func<object, object, Exception, object> _inspector;
        bool _expandAllEndpoints;
        private readonly JsonSerializerSettings _jsonOptions;

        public ExpandActionFilter(Expander expander, Dictionary<string, object> expandContext, Func<object, object, Exception, object> inspector, bool expandAll, JsonSerializerSettings jsonOptions = null) :
            base()
        {
            _expander = expander;
            _context = expandContext;
            _inspector = inspector;
            _expandAllEndpoints = expandAll;
            _jsonOptions = jsonOptions;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            Exception exceptionResult = null;
            object resultObject = null;
            Type destinationType = null;

            var expandAttribute = context
                .ActionDescriptor
                .FilterDescriptors
                .SingleOrDefault(d => d.Filter.GetType() == typeof(ExpandResultAttribute));

            if (!_expandAllEndpoints && expandAttribute == null)
                return;

            if (expandAttribute != null)
            {
                destinationType = ((ExpandResultAttribute)expandAttribute.Filter).DestinationType;
            }

            var doNotExpandAttribute = context
                .ActionDescriptor
                .FilterDescriptors
                .SingleOrDefault(d => d.Filter.GetType() == typeof(DoNotExpandResultAttribute));

            if (doNotExpandAttribute != null)
                return;

            // Set the error out of the gate should something have gone wrong coming into Popcorn
            if (context.Exception != null)
            {
                exceptionResult = context.Exception;
                if (context.HttpContext.Response.StatusCode == 200)
                {
                    context.HttpContext.Response.StatusCode = 500;
                }
                context.ExceptionHandled = true; // Setting this so the inspector is still respected
            }
            else if (context.Result is ObjectResult) // Disect the response if there is something to unfold and no exception
            {
                resultObject = ((ObjectResult)context.Result).Value;

                // Wrap the main work here in a try/catch that we can then pass to our inspector
                try
                {
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
                        resultObject = _expander.Expand(resultObject, _context, PropertyReference.Parse(includes), destinationTypeHint: destinationType);
                    }

                    // Sort should there be anything to sort
                    if (resultObject != null)
                    {
                        // Assign sortDirection where necessary, but default to Ascending if nothing passed in
                        SortDirection sortDirection = SortDirection.Ascending;
                        if (context.HttpContext.Request.Query.ContainsKey("sortDirection"))
                        {
                            // Assign the proper sort direction, but invalidate an invalid value
                            try
                            {
                                sortDirection = (SortDirection)Enum.Parse(typeof(SortDirection), context.HttpContext.Request.Query["sortDirection"]);
                            }
                            catch (ArgumentException)
                            {
                                throw new ArgumentException(context.HttpContext.Request.Query["sortDirection"]);
                            }
                        }

                        // Do any sorting as specified
                        if (context.HttpContext.Request.Query.ContainsKey("sort"))
                        {
                            resultObject = _expander.Sort(resultObject, context.HttpContext.Request.Query["sort"], sortDirection);
                        }
                    }
                }
                catch (Exception e)
                {
                    exceptionResult = e;
                    // Set the response code as appropriate for a caught error
                    context.HttpContext.Response.StatusCode = 500;
                }
            }

            // Apply our inspector to the expanded content
            if (_inspector != null)
            {
                resultObject = _inspector(resultObject, _context, exceptionResult);
            }
            else if (exceptionResult != null) // Have to rethrow the error if there is no inspector set so as to not return false positives
            {
                throw exceptionResult;
            }

            var settings = _jsonOptions?.DeepCopy() ?? new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            context.Result = new JsonResult(resultObject, settings);
            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }

    /// <summary>
    /// Apply this attribute to ensure a result is always expanded
    /// </summary>
    public class ExpandResultAttribute : ActionFilterAttribute
    {
        public ExpandResultAttribute(Type destinationType)
        {
            DestinationType = destinationType;
        }

        public ExpandResultAttribute()
        {
        }

        public Type DestinationType { get; private set; }
    }

    /// <summary>
    /// Apply this attribute to ensure a result is never expanded
    /// </summary>
    public class DoNotExpandResultAttribute : ActionFilterAttribute
    {
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
            options.Filters.Add(new ExpandActionFilter(expander, configuration.Context, configuration.Inspector, configuration.ApplyToAllEndpoints, configuration.JsonOptions));
        }
    }
}