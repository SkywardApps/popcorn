using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Skyward.Popcorn.Abstractions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;

namespace Skyward.Popcorn
{
    public class ExpandServiceFilter : IActionFilter
    {
        private readonly IPopcorn _popcorn;
        private readonly IPopcornContextAccessor _popcornContext;

        private bool _isExpanding;

        public ExpandServiceFilter(IPopcorn popcorn, IPopcornContextAccessor popcornAccessor) :
            base()
        {
            _popcorn = popcorn ?? throw new ArgumentNullException(nameof(popcorn));
            _popcornContext = popcornAccessor ?? throw new ArgumentNullException(nameof(popcornAccessor));
        }


        public void OnActionExecuting(ActionExecutingContext context)
        {
            _isExpanding = false;

            var filterDescriptor = context
                .ActionDescriptor
                .FilterDescriptors
                .SingleOrDefault(d => d.Filter.GetType() == typeof(ExpandResultAttribute));

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

            _popcornContext.Popcorn = _popcorn;
            _popcornContext.PropertyReferences = PropertyReference.Parse(includes);
            _isExpanding = true;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if(!_isExpanding)
            {
                return;
            }

            // Set the error out of the gate should something have gone wrong coming into Popcorn
            if (context.Exception != null)
            {
                return;
            }
            else if (context.Result is ObjectResult) // Dissect the response if there is something to unfold and no exception
            {
                try
                {
                    var originalResult = (ObjectResult)context.Result;
                    var resultObject = originalResult.Value;
                    var expandedObject = _popcorn.Expand(resultObject?.GetType(), resultObject, new List<PropertyReference>(_popcornContext.PropertyReferences));
                    
                    var newResult = new JsonResult(expandedObject);
                    newResult.StatusCode = originalResult.StatusCode;   //Keep the same status code otherwise it will default to 200 OK
                    
                    context.Result = newResult;
                }
                catch (Exception ex)
                {
                    // Report back the error because it's something related to the API request
                    context.HttpContext.Response.StatusCode = 500;
                    context.Result = new JsonResult(new
                    {
                        ErrorCode = ex.GetType().Name,
                        ErrorMessage = ex.Message
                    });
                }
            }
        }
    }
}