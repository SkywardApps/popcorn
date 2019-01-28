using Newtonsoft.Json;
using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PopcornNetFramework.WebApi
{
    public class ExpandActionFilter : ActionFilterAttribute
    {
        Expander _expander;
        Dictionary<string, object> _context;
        Func<object, object, Exception, object> _inspector;
        bool _expandAllEndpoints;

        public ExpandActionFilter(Expander expander, Dictionary<string, object> expandContext, Func<object, object, Exception, object> inspector, bool expandAll) :
            base()
        {
            _expander = expander;
            _context = expandContext;
            _inspector = inspector;
            _expandAllEndpoints = expandAll;
        }
        
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            if (!_expandAllEndpoints)
            {
                var filterDescriptor = context.ActionContext
                    .ActionDescriptor
                    .GetFilters()
                    .SingleOrDefault(f => f.GetType() == typeof(ExpandResultAttribute));

                //Cast the filter to ExpandResultAttribute
                var attributeInstance = filterDescriptor as ExpandResultAttribute;

                //If the attribute is null, i.e. not present, or false, it shouldn't expand and we return here
                if (!(attributeInstance?.ShouldExpand ?? false))
                {
                    return;
                }
            }

            var doNotExpandAttribute = context.ActionContext
                .ActionDescriptor
                .GetFilters()
                .SingleOrDefault(f => f.GetType() == typeof(DoNotExpandResultAttribute));

            if (doNotExpandAttribute != null)
                return;

            Exception exceptionResult = null;
            object resultObject = null;

            // Set the error out of the gate should something have gone wrong coming into Popcorn
            if (context.Exception != null)
            {
                exceptionResult = context.Exception;
                if (context.Response == null || context.Response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    context.Response = context.Request.CreateResponse(System.Net.HttpStatusCode.InternalServerError); ;
                }
            }
            else if (context.Response.Content is ObjectContent) // Disect the response if there is something to unfold and no exception
            {
                resultObject = ((ObjectContent)context.Response.Content).Value;

                // Wrap the main work here in a try/catch that we can then pass to our inspector
                try
                {
                    var queryParams = context.Request.GetQueryNameValuePairs().ToDictionary((kv) => kv.Key, (kv) => kv.Value);
                    if (_expander.WillExpand(resultObject))
                    {
                        // see if we can find some include statements
                        string includes = "[]";
                        if (queryParams.ContainsKey("include"))
                        {
                            includes = queryParams["include"];
                        }
                        else if (context.Request.Headers?.Contains("API-INCLUDE") ?? false)
                        {
                            includes = context.Request.Headers.GetValues("API-INCLUDE").FirstOrDefault() ?? "";
                        }

                        // Use our expander and expand the object
                        resultObject = _expander.Expand(resultObject, _context, PropertyReference.Parse(includes));
                    }

                    // Sort should there be anything to sort
                    if (resultObject != null)
                    {
                        // Assign sortDirection where necessary, but default to Ascending if nothing passed in
                        SortDirection sortDirection = SortDirection.Ascending;
                        if (queryParams.ContainsKey("sortDirection"))
                        {
                            // Assign the proper sort direction, but invalidate an invalid value
                            try
                            {
                                sortDirection = (SortDirection)Enum.Parse(typeof(SortDirection), queryParams["sortDirection"]);
                            }
                            catch (ArgumentException)
                            {
                                throw new ArgumentException(queryParams["sortDirection"]);
                            }
                        }

                        // Do any sorting as specified
                        if (queryParams.ContainsKey("sort"))
                        {
                            resultObject = _expander.Sort(resultObject, queryParams["sort"], sortDirection);
                        }
                    }
                }
                catch (Exception e)
                {
                    exceptionResult = e;
                    // Set the response code as appropriate for a caught error
                    context.Response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
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

            var json = JsonConvert.SerializeObject(resultObject,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            context.Response.Content = new StringContent(json, Encoding.UTF8, "application/json");

            base.OnActionExecuted(context);
        }
    }

    /// <summary>
    /// Apply this attribute to ensure a result is always expanded or optionally pass a boolean specifying behaviour
    /// </summary>
    public class ExpandResultAttribute : ActionFilterAttribute
    {
        public bool ShouldExpand { get; private set; }
        /// <summary>
        /// Apply this attribute to specify whether a result is to always expand or never expand
        /// </summary>
        /// <param name="shouldExpand">Defaults to <c>true</c>. If set to false, result will not be expanded. If passing <c>false</c>, you can also use <seealso cref="DoNotExpandResultAttribute"/></param>
        public ExpandResultAttribute(bool shouldExpand = true)
        {
            ShouldExpand = shouldExpand;
        }
    }

    /// <summary>
    /// Apply this attribute to ensure a result is never expanded
    /// </summary>
    public class DoNotExpandResultAttribute : ExpandResultAttribute
    {
        public DoNotExpandResultAttribute() : base(false)
        {

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
        public static void UsePopcorn(this HttpConfiguration options, Action<PopcornConfiguration> configure = null)
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
            options.Filters.Add(new ExpandActionFilter(expander, configuration.Context, configuration.Inspector, configuration.ApplyToAllEndpoints));
            
        }
    }
}
