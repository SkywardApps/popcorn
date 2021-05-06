using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Skyward.Popcorn
{

    public class ExpandServiceFilter : IActionFilter
    {
        private readonly PopcornConfiguration _config;
        private readonly IExpanderService _expanderService;
        private readonly IPopcornContextAccessor _popcornContext;

        private bool _isExpanding;

        public ExpandServiceFilter(IOptions<PopcornConfiguration> config, IExpanderService expanderService, IPopcornContextAccessor popcornAccessor) :
            base()
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
            _expanderService = expanderService ?? throw new ArgumentNullException(nameof(expanderService));
            _popcornContext = popcornAccessor ?? throw new ArgumentNullException(nameof(popcornAccessor));
        }


        public void OnActionExecuting(ActionExecutingContext context)
        {
            _isExpanding = false;

            Type destinationType = null;


            var filterDescriptor = context
                .ActionDescriptor
                .FilterDescriptors
                .SingleOrDefault(d => d.Filter.GetType() == typeof(ExpandResultAttribute));

            if (!_config.ApplyToAllEndpoints)
            {
                //Cast the filter property to ExpandResultAttribute
                var attributeInstance = filterDescriptor?.Filter as ExpandResultAttribute;

                //If the attribute is null, i.e. not present, or false, it shouldn't expand and we return here
                if (!(attributeInstance?.ShouldExpand ?? false))
                {
                    return;
                }
            }

            if (filterDescriptor != null)
            {
                destinationType = ((ExpandResultAttribute)filterDescriptor.Filter).DestinationType;
            }

            var doNotExpandAttribute = context
                .ActionDescriptor
                .FilterDescriptors
                .SingleOrDefault(d => d.Filter.GetType() == typeof(DoNotExpandResultAttribute));

            if (doNotExpandAttribute != null)
                return;

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
                _popcornContext.SortTarget = context.HttpContext.Request.Query["sort"];
            }

            _popcornContext.PropertyReferences = PropertyReference.Parse(includes);
            _popcornContext.DestinationType = destinationType;
            _popcornContext.SortDirection = sortDirection;

            _isExpanding = true;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if(!_isExpanding)
            {
                return;
            }

            Exception exceptionResult = null;
            object resultObject = null;
            ObjectResult originalResult = null;


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
                originalResult = (ObjectResult)context.Result;
                resultObject = originalResult.Value;

                // Wrap the main work here in a try/catch that we can then pass to our inspector
                try
                {
                    var expander = _expanderService.Expander;
                    if (expander.WillExpand(resultObject))
                    {
                        // Use our expander and expand the object
                        resultObject = expander.Expand(resultObject, _expanderService.Context, _popcornContext.PropertyReferences, destinationTypeHint: _popcornContext.DestinationType);
                    }

                    // Sort should there be anything to sort
                    if (resultObject != null)
                    {

                        // Do any sorting as specified
                        if (_popcornContext.SortTarget != null)
                        {
                            resultObject = expander.Sort(resultObject, _popcornContext.SortTarget, _popcornContext.SortDirection);
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
            else
            {
                return;
            }

            // Apply our inspector to the expanded content
            if (_expanderService.Inspector != null)
            {
                resultObject = _expanderService.Inspector(resultObject, _expanderService.Context, exceptionResult);
            }
            else if (exceptionResult != null) // Have to rethrow the error if there is no inspector set so as to not return false positives
            {
                throw exceptionResult;
            }

            if(originalResult != null)
            {
                originalResult.Value = resultObject;
            }
            else
            {
                context.Result = new ObjectResult(resultObject);
            }
        }
    }
}