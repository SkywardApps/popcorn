## Getting started with our example project
Popcorn comes with an example project that can be a helpful starting point for anyone new to Popcorn. Once you are up and running with the project, you will be able to begin sending queries and receiving JSON objects back as a result. 

### Topics
+ [Building the example](#building-the-example)
  + [Popcorn dependencies](#popcorn-dependencies)
  + [Visual Studio](#visual-studio)
+ [Running the example](#running-the-example)
  + [How to debug](#how-to-debug)
+ [Submitting queries](#submitting-queries)
  + [Using your browser](#using-your-browser)
  + [Using Postman](#using-postman)
+ [Understanding results](#understanding-results)
  + [Example response](#example-response)


<a name="building-the-example"></a>
## Building the example
To build the example project, simply load the Popcorn solution and build the entire solution.
<a name="popcorn-dependencies"></a>
### Popcorn dependencies
+ [NuGet 1.6](https://docs.microsoft.com/en-us/nuget/guides/install-nuget#nuget-package-manager-in-visual-studio)
+ [.Net Core v2](https://www.microsoft.com/net/learn/get-started/windows)
+ [.Net Framework 4.6.01038](https://www.microsoft.com/en-us/download/details.aspx?id=48130)
<a name="visual-studio"></a>
### Visual Studio
We recommend using [Visual Studio 2017](https://www.visualstudio.com/downloads/). Open the Popcorn solution and build all.

<a name="running-the-example"></a>
## Running the example
Once you have Popcorn building, you should be able to run a debug build of the **PopcornNetCoreExample** project. This project has a few examples of how to use Popcorn, and can be used to troubleshoot issues with your configuration if you encounter any problems running/using Popcorn.
<a name="how-to-debug"></a>
### How to debug
To build/run a debug version of our PorcornNetCoreExample, just load the solution with Visual Studio and build all. Then choose **PopcornNetCoreExample** in the Startup Project dropdown menu, and **PopcornCoreExample** in the debug profile dropdown. You should now be able to begin debugging by using `Debug -> Start Debugging`. Dropping a breakpoint at one of the endpoints in `/Controllers/ExampleController.cs` is a good place to start.
<a name="submitting-queries"></a>
## Submitting queries
Once the PopcornNetCoreExample project is running as debug, you can submit queries and begin digging in. _Although it is possible to use your web browser_ to submit queries to Popcorn, we suggest using [Postman](#using-postman).
<a name="using-your-browser"></a>
### Using your browser
You _can_ use your browser, if Postman is not an option for you. You just need to enter the url for the endpoint of your choosing at ```localhost:49699/api/example/...```. For example ```http://localhost:49699/api/example/employees```.
<a name="using-postman"></a>
### Using Postman
Postman makes working with Popcorn much easier. You can easily submit queries, read results, and much more. To submit a query to popcorn you will need to talk to it on your **local port 49699**. An example endpoint that you can query would be ```http://localhost:49699/api/example/employees```
<a name="understanding-results"></a>
## Understanding results
For our example, we have provided some sample data models, a controller, and projections for the data models. You can see the endpoints provided by looking in `Controllers/ExampleController.cs`. Our example data models are populated in `/Startup.cs`. 
<a name="example-response"></a>
### Example response 
The expected response from the query given in the [Using Postman](#using-postman) section, should be the following:
```
[
    {
        "FirstName": "Liz",
        "LastName": "Lemon",
        "Birthday": "1981-05-01T00:00:00-04:00",
        "Employment": "FullTime",
        "VacationDays": 0,
        "Vehicles": [
            {
                "Model": "Firebird",
                "Make": "Pontiac",
                "Year": 1981,
                "Color": 2,
                "User": "Alice"
            }
        ]
    },
    {
        "FirstName": "Jack",
        "LastName": "Donaghy",
        "Birthday": "1957-07-12T00:00:00-04:00",
        "Employment": "PartTime",
        "VacationDays": 300,
        "Vehicles": [
            {
                "Model": "250 GTO",
                "Make": "Ferrari N.V.",
                "Year": 1962,
                "Color": 1,
                "User": "Alice"
            },
            {
                "Model": "Cayman",
                "Make": "Porsche",
                "Year": 2005,
                "Color": 5,
                "User": "Alice"
            }
        ]
    }
]
```


## Further Reading

+ [Quick Start](docs/QuickStart.md)
+ [Documentation](docs/Documentation.md)
+ [Roadmap](docs/Roadmap.md)
+ [Releases and Release Notes](docs/Releases.md)
+ [Contributing](docs/Contributing.md)
+ [License](LICENSE)
+ [Meet the Maintainers](docs/Maintainers.md)
