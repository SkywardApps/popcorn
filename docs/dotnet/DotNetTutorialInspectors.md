# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Inspectors

We recognize that wrapping reponses in standard response objects, what we call an "inspector", is one of the easiest ways to make an API
more user friendly and generally consumable. With that said, we've made an effort to expand our custom inspector options while also providing
a default inspector object that we think will meet most (if not all) of your out of the box needs.

This tutorial will walk you through the two ways of setting an inspector and briefly review how that changes your response results.

## "SetDefaultApiResponseInspector" and its usage

### Example
First let's start with an example of our responses vary from the usage of an inspector versus not using one.

Here is a standard response from popcorn with no inspector set, targeting our GET cars endpoint we've talked about before:
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

Note that a simple array of "Car" objects is returned, as we stated as the response on our Car endpoint. Let's say we make an include call on the same endpoint
for a property "Fishy" - which isn't our "Car" object (that probably didn't need explaining).

We would expect an error of some sort to be returned so lets see what the response object looks like now:

```javascript
http://localhost:49699/api/example/cars?include=[Fishy]

... cricket... cricket...
```

Now you won't actually see "cricket" as you're response, but you will see a 500 status code and be stuck guessing what it is that went wrong.

Enter the power of the inspector! We don't want to have to handle every single error possiblity in it's own unique API response, rather
we want to have one central response wrapper that handles all of the heavy lifting for us so our consumers get consistent responses, every time.

Ok, doing this all over again. Here are both requests from above using our default inspector:

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

So, so, so much better. We now have a consistent response wrapper that can be consumed with ease by other services and/or our own front end.

### Putting it into action

You can view the full documentation around the ApiResponse object [here](PopcornStandard/Implementation/ApiResponse.cs) so we won't dive too much into that here.

To set the default inspector we just refer back to our Startup.cs file and add the SetDefaultApiResponseInspector method to our UsePopcorn call as seen below.
```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .SetDefaultApiResponseInspector()
		....
```

Done! That easy. You've now implemented a standard inspector to wrap all of your API responses with ease. You have to love implementations that take one line.


## Set your own custom inspector

Now we get that our default inspector may not meet the needs of your organization so we also offer the option to define your own inspector.

Back to Startup.cs!
```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .SetInspector((data, context, resultException) => new Wire.Response { Data = resultException == null ? data : resultException.Message, Success = resultException == null ? true : false })
		....
```

You'll see that our inspector requires two objects (the data and context) as well as an exception to be passed into the consuming fucntion. 
How you choose to wrap Responses and use them within the inspector is your choice!

We give you a very simple example here where we've added a Response object that we set with the appropriate results we would like returned.

And there you have it - your two options to set an inspector from within Popcorn and as always please submit any issues or PRs should we be missing something
that will support you and/or your team.