using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopcornNetCoreExample;
using System;
using System.Net.Http;

namespace PopcornNetCoreExampleIntegrationTest
{
    [TestClass]
    public class TestSetup : CommonIntegrationTest._Utilities.TestSetup
    {
        [AssemblyInitialize]
        public static void AssemblySetup(TestContext context)
        {
            Assert.IsNotNull(LazyServer.Value);
            Client = LazyClient.Value;
        }

        /// <summary>
        /// This will create the TestServer on demand
        /// </summary>
        public static readonly Lazy<TestServer> LazyServer = new Lazy<TestServer>(() =>
        {
            var server = new TestServer(new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>());

            return server;
        });

        /// <summary>
        /// Uses the test server to create an HttpClient on demand
        /// </summary>
        public static readonly Lazy<HttpClient> LazyClient = new Lazy<HttpClient>(() => {
            var client = Server.CreateClient();
            return client;
        });

        public static TestServer Server
        {
            get { return LazyServer.Value; }
        }
    }
}
