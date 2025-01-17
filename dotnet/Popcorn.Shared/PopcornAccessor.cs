using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Popcorn.Shared
{
    public interface IPopcornAccessor
    {
        IReadOnlyList<PropertyReference> PropertyReferences { get; }
        ApiResponse<T> CreateResponse<T>(T data);
    }

    public class PopcornAccessor : IPopcornAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IReadOnlyList<PropertyReference>? _propertyReferences;

        public PopcornAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IReadOnlyList<PropertyReference> PropertyReferences
        {
            get
            {
                if (_propertyReferences == null)
                {
                    var context = _httpContextAccessor.HttpContext;
                    _propertyReferences = PropertyReference.ParseIncludeStatement(context?.Request.Query["include"]);
                }
                return _propertyReferences;
            }
        }

        public ApiResponse<T> CreateResponse<T>(T data)
        {
            return new ApiResponse<T>(PropertyReferences, data);
        }
    }
}
