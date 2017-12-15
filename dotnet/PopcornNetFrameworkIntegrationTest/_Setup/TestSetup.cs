using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;
using PopcornNetFrameworkExample;
using System;
using System.Net.Http;
using System.Web.Http;

namespace PopcornNetCoreExampleIntegrationTest
{
    [TestClass]
    public class TestSetup : CommonIntegrationTest.TestSetup
    {
        [AssemblyInitialize]
        public static void AssemblySetup(TestContext context)
        {
            Assert.IsNotNull(LazyServer.Value);
            Client = LazyClient.Value;
        }

        static readonly string baseUri = "http://localhost:46588";

        /// <summary>
        /// This will create the TestServer on demand
        /// </summary>
        public static readonly Lazy<IDisposable> LazyServer = new Lazy<IDisposable>(() =>
        {
            var app = WebApp.Start<Startup>(new StartOptions(url: baseUri));
            return app;
        });

        /// <summary>
        /// Uses the test server to create an HttpClient on demand
        /// </summary>
        public static readonly Lazy<HttpClient> LazyClient = new Lazy<HttpClient>(() => {
            var client = new HttpClient
            {
                BaseAddress = new Uri(baseUri)
            };
            return client;
        });
        

        public static IDisposable Server
        {
            get { return LazyServer.Value; }
        }


        class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                HttpConfiguration config = new HttpConfiguration();

                UnityConfig.RegisterComponents();
                config.DependencyResolver = UnityConfig.unityWebApiResolver;
                WebApiConfig.Register(config);

                app.UseWebApi(config);
            }
        }
    }
}
