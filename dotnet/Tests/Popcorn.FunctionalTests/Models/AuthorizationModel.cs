using System.Collections.Generic;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class OwnedResource
    {
        [Default]
        public int Id { get; set; }

        [Default]
        public string OwnerId { get; set; } = string.Empty;

        [Default]
        public string Title { get; set; } = string.Empty;
    }

    public class OwnedResourceList
    {
        [Default]
        public List<OwnedResource> Resources { get; set; } = new();
    }
}
