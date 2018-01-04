using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PopcornNetCoreExample.Wire;
using Shouldly;
using System.Threading.Tasks;

namespace PopcornNetCoreExampleIntegrationTest
{
    [TestClass]
    public class IntegrationSanity
    {

        [TestMethod]
        public async Task TestIntegrationTests()
        {
            var response = await TestSetup.Client.GetAsync("api/example/status");

            string responseBody = await response.Content.ReadAsStringAsync();

            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            json.Success.ShouldBeTrue();
        }
    }
}
