using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.Owin.Hosting;
using System.Net.Http;
using Owin;
using System.Web.Http;
using PopcornNetFrameworkExample;
using Microsoft.Practices.Unity;

namespace PopcornNetCoreExampleIntegrationTest
{
    [TestClass]
    class TestSetup
    {
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

        public static HttpClient Client
        {
            get {
                Assert.IsNotNull(LazyServer.Value);
                return LazyClient.Value;
            }
        }

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
