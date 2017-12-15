## Getting started with our example projects
Popcorn comes with an example project that can be a helpful starting point for anyone new to Popcorn. 
The main purpose of this project is to act as a testbed for integrating Popcorn, 
and to demonstrate the features of Popcorn. Once you are up and running with the project, 
you will be able to begin sending queries and receiving JSON objects back as a result.

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
<a name="popcorn=dependencies"></a>
### Popcorn dependencies
+ [.Net Standard 1.6](https://www.microsoft.com/net/download/windows)
<a name="visual-studio"></a>
### Visual Studio
We recommend using [Visual Studio 2017](https://www.visualstudio.com/downloads/). Open the Popcorn solution and build all.
<a name="running-the-example"></a>
## Running the example
Once you have Popcorn building, you should be able to run a debug build of the **PopcornNetCoreExample** to demonstrate a .Net Core 1.1 project or **PopcornNetFrameworkExample** for a .Net Framework 4.6.1 project. 
This project has a few examples of how to use Popcorn, and can be used to troubleshoot issues with your configuration if you encounter any problems running/using Popcorn.
<a name="how-to-debug"></a>
### How to debug
To build/run a debug version of our example, just load the solution with Visual Studio and build all. 
Then choose **PopcornNetCoreExample** in the Startup Project dropdown menu, and **PopcornNetCoreExample** in the debug profile dropdown. 
You should now be able to begin debugging by using `Debug -> Start Debugging`. Dropping a breakpoint at one of the endpoints in `/Controllers/ExampleController.cs` 
should serve as a good entry point for tracing the process that takes place when a query is submitted.
<a name="submitting-queries"></a>
## Submitting queries
Once the project is running in debug, you can submit queries and begin digging in. 
_Although it is possible to use your web browser_ to submit queries to Popcorn, we suggest using [Postman](#using-postman).
<a name="using-your-browser"></a>
### Using your browser
You _can_ use your browser, if Postman is not an option for you. 
You just need to enter the url for the endpoint of your choosing at ```localhost:49699/api/example/...```. 
For example ```http://localhost:49699/api/example/employees```.
<a name="using-postman"></a>
### Using Postman
Postman makes working with Popcorn much easier. You can easily submit queries, read results, and much more. 
To submit a query to popcorn you will need to talk to it on your **local port 49699**. An example endpoint that you can query would be 
```http://localhost:49699/api/example/employees?include=[FirstName,LastName,Employment,Vehicles[Make,Year,Color]]```
<a name="understanding-results"></a>
## Understanding results
For our example, we have provided some sample data models, a controller, and projections for the data models. 
You can see the endpoints provided by looking in `Controllers/ExampleController.cs`. Our example data models are populated in `/Startup.cs`. 
<a name="example-response"></a>
### Example response 
The expected response from the query given in the [Using Postman](#using-postman) section, should be the following:
```
{
    "Success": true,
    "Data": [
        {
            "FirstName": "Liz",
            "LastName": "Lemon",
            "Employment": "FullTime",
            "Vehicles": [
                {
                    "Make": "Pontiac",
                    "Year": 1981,
                    "Color": "Blue"
                }
            ]
        },
        {
            "FirstName": "Jack",
            "LastName": "Donaghy",
            "Employment": "PartTime",
            "Vehicles": [
                {
                    "Make": "Ferrari N.V.",
                    "Year": 1962,
                    "Color": "Red"
                },
                {
                    "Make": "Porsche",
                    "Year": 2005,
                    "Color": "Yellow"
                }
            ]
        }
    ]
}
```

