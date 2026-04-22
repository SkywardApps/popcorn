using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class PersonWithComputed
    {
        [Default]
        public string FirstName { get; set; } = string.Empty;

        [Default]
        public string LastName { get; set; } = string.Empty;

        [Default]
        public string FullName => $"{FirstName} {LastName}";
    }
}
