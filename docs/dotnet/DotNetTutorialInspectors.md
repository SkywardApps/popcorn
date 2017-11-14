# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Inspectors

[Table Of Contents](../../docs/TableOfContents.md)

We feel that it is important for developers to have the opportunity to inspect their Popcorn response objects in a consistent fashion so that 
they can format or process the responses according to the needs of their users. So with that we've extended "inspector" functionality to Popcorn users! 
An example of an "inspector" that we often use would be setting a standard response object wrapper to make an
API easier to consume. While there are many ways an inspector could be used, we've made an effort to expand our custom inspector options while also providing
a default inspector that we think will meet most (if not all) of your out of the box needs.

This tutorial will walk you through the two ways of setting an inspector and briefly review how that changes your response results.

## "SetDefaultApiResponseInspector" and its usage

### Example
First let's start with an example of how our responses vary with the usage of an inspector versus not using one.

Here is a standard response from popcorn with no inspector set, targeting a GET cars endpoint:
```csharp
[HttpGet, Route("cars")]
public List<Car> Cars()
{
    return _context.Cars;
}
```

```javascript
http://localhost:49699/api/example/cars

[
    {
        "Model": "Firebird",
        "Make": "Pontiac",
        "Year": 1981
    },
    {
        "Model": "250 GTO",
        "Make": "Ferrari N.V.",
        "Year": 1962
    },
    {
        "Model": "Cayman",
        "Make": "Porsche",
        "Year": 2005
    }
]
```

Note that a simple array of "Car" objects is returned. Let's say we make an include call on the same endpoint
for a property "Fishy" - which isn't a property on our "Car" object (that probably didn't need explaining).


We would expect an error of some sort to be returned so lets see what the response object looks like now:

```javascript
http://localhost:49699/api/example/cars?include=[Fishy]

... cricket... cricket...
```

Now you won't actually see "cricket" as your response, but you will see a 500 status code and be stuck guessing what it is that went wrong 
as there would be an error thrown within Popcorn, but that is not presented to the user unless it is instructed to do so.


Enter the power of the inspector! We don't want to have to handle every single error possiblity in its own unique API response - rather
we want to have one central response wrapper that handles all of the heavy lifting for us so our consumers get consistent responses, every time
- be those errors coming from something to do with Popcorn or a server logic error.


Here are both requests from above using our default inspector:

```javascript
http://localhost:49699/api/example/cars

{
    "Success": true,
    "Data": [
        {
            "Model": "Firebird",
            "Make": "Pontiac",
            "Year": 1981
        },
        {
            "Model": "250 GTO",
            "Make": "Ferrari N.V.",
            "Year": 1962
        },
        {
            "Model": "Cayman",
            "Make": "Porsche",
            "Year": 2005
        }
    ]
}
```

```javascript
http://localhost:49699/api/example/cars?include=[Fishy]

{
    "Success": false,
    "ErrorCode": "System.ArgumentOutOfRangeException",
    "ErrorMessage": "Specified argument was out of the range of valid values.\r\nParameter name: Fishy",
    "ErrorDetails": "System.ArgumentOutOfRangeException: Specified argument was out of the range of valid values.\r\nParameter name: Fishy\r\n  ... etc. ... etc."
}
```

We now have a consistent response wrapper that can be consumed with ease by other services and/or our own front end.

### Putting it into action

You can view the full documentation around the ApiResponse object [here](PopcornStandard/Implementation/ApiResponse.cs) so we won't dive too much into that now.

To set the default inspector we just refer back to our Startup.cs file and add the SetDefaultApiResponseInspector method to our UsePopcorn call as seen below.
```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .SetDefaultApiResponseInspector()
		....
```

Done! That was easy. You've now implemented a standard inspector to wrap all of your API responses with ease.


## Set your own custom inspector

Our default inspector may not meet all of the needs of your organization, so we also offer the option to define your own inspector.

We can add a custom response object and then pass it to Popcorn in our configuration declaration:
```csharp
public class Response
{
    [Required]
    public bool Success { get; set; }

	// We set this as an option to ALWAYS include a Data object, regardless if there is no data passed from the server
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public object Data { get; set; }
}
```

Back to Startup.cs where we configure our custom inspector!

```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .SetInspector((data, context, resultException) => new Wire.Response 
		{ 
			Data = resultException == null ? data : resultException.Message, 
			Success = resultException == null ? true : false 
		})
		....
```

You'll see that our inspector requires two objects (the data and context) as well as an exception to be passed into the consuming function. 
How you choose to wrap Responses and use them within the inspector is your choice.

We give you a very simple example here where we've added a Response object that we set with the appropriate results we would like returned.

And there you have it - your two options to set an inspector from within Popcorn. As always, please submit any issues or PRs should we be missing something
that will support you and/or your team.