## CommonIntegrationTests

This class library contains the actual suite of integration tests.

This is done so we can easily apply coverage to all individual implementations of the application
layer of popcorn.

In order to utilize this test set:
* Define classes that inherit each of the text fixtures you want applied.  Add the [TestClass] attribute. 
This will pull in all test cases defined in that base class.
* If you need to ignore specific tests, create a new function in your class with the same implementation to hide
the base class's test, and add the [Ignore] property.
* Define a TestSetup class that inherits from CommonIntegrationTest.TestSetup.  Add a AssemblyInitialize class
that assigns the static HttpClient Client property.  Integration tests will use this Client to run.  Example:
```csharp 
        [AssemblyInitialize]
        public static void AssemblySetup(TestContext context)
        {
            Assert.IsNotNull(LazyServer.Value);
            Client = LazyClient.Value;
        }
```