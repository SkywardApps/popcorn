using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Skyward.Popcorn
{

    /// <summary>
    /// Apply this attribute to ensure a result is always expanded or optionally pass a boolean specifying behaviour
    /// </summary>
    public class ExpandResultAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Apply this attribute to specify whether a result is to always expand or never expand
        /// </summary>
        /// <param name="shouldExpand">Defaults to <c>true</c>. If set to false, result will not be expanded. If passing <c>false</c>, you can also use <seealso cref="DoNotExpandResultAttribute"/></param>
        public ExpandResultAttribute(bool shouldExpand = true)
        {
            ShouldExpand = shouldExpand;
        }

        public ExpandResultAttribute(Type destinationType)
        {
            DestinationType = destinationType;
            ShouldExpand = true;
        }

        public Type DestinationType { get; private set; }
        public bool ShouldExpand { get; private set; }
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
}