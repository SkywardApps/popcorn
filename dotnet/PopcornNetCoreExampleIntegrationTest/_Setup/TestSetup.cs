using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using PopcornNetCoreExample;
using System.Net.Http;

namespace PopcornNetCoreExampleIntegrationTest
{
    [TestClass]
    class TestSetup
    {
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

        public static HttpClient Client
        {
            get { return LazyClient.Value; }
        }

        public static TestServer Server
        {
            get { return LazyServer.Value; }
        }
    }
}
