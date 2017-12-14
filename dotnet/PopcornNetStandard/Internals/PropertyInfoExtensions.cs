using System;
using System.Reflection;

namespace Skyward.Popcorn
{
    /// <summary>
    /// Provides utility extensions for the PropertyInfo type.
    /// </summary>
    internal static class PropertyInfoExtensions
    { 
        /// <summary>
        /// Set a value to a property on an object.  This will handle: converting types (eg int -> double) and nullable properties (int -> int?)
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="inputObject"></param>
        /// <param name="propertyVal"></param>
        /// <returns></returns>
        public static bool TrySetValueHandleConvert(this PropertyInfo propertyInfo, object inputObject,  object propertyVal)
        {
            try
            {
                //Convert.ChangeType does not handle conversion to nullable types
                //if the property type is nullable, we need to get the underlying type of the property
                var targetType = propertyInfo.PropertyType.IsNullableType() ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;

                if (propertyVal is IConvertible)
                {
                    //Returns an System.Object with the specified System.Type and whose value is
                    //equivalent to the specified object.
                    propertyVal = Convert.ChangeType(propertyVal, targetType);
                }

                //Set the value of the property
                propertyInfo.SetValue(inputObject, propertyVal, null);

                return true;
            }
            catch (Exception)
            {
                // If any exception happened we couldn't set the property, so return false.
                return false;
            }
        }
    }
}
