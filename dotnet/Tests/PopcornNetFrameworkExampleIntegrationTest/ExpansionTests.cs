using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace PopcornNetFrameworkExampleIntegrationTest
{
    [TestClass]
    public class ExpansionTests : CommonIntegrationTest.ExpansionTests
    {
        // A complete error response object returned from a server side error (i.e. not Popcorn itself)
        [TestMethod, Ignore("We can't set a custom response code in the self-hosted environment")]
        new public async Task ResponseObjectServerError()
        {
            await (this as CommonIntegrationTest.ExpansionTests).ResponseObjectServerError();
        }
    }
}
