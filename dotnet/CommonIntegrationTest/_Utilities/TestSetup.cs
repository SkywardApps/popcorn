using System.Net.Http;

namespace CommonIntegrationTest
{
    public class TestSetup
    {
        public static HttpClient Client { get; protected set; }
    }
}
