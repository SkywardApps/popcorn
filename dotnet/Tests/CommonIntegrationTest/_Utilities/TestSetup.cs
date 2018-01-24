using System.Net.Http;

namespace CommonIntegrationTest._Utilities
{
    public class TestSetup
    {
        public static HttpClient Client { get; protected set; }
    }
}
