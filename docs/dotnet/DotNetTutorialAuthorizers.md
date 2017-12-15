# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Authorizers

[Table Of Contents](../../docs/TableOfContents.md)

Restricting access to information in our APIs is so important and can often be incredibly complex. We recognize this inevitable struggle
and are now offering an "Authorize" setting within your Popcorn configuration to restrict responses as you see fit 
and hopefully mitigate part of that trouble.

## How does it work?
In PopcornConfiguration there is now the method shown below:
```csharp
public PopcornConfiguration Authorize<TSourceType>(Func<object, ContextType, TSourceType, bool> authorizer)
```

What we are able to do with this is specify a function when we initialize our Popcorn Configuration that takes in an object that is compared based on some context 
against the rules set for a source object type, while returning a boolean that says whether the entire source object should be authorized to be returned or not.

Makes sense, right?

## Example
Let's take the example we already have created of Cars and imagine two things changed in our example program:
1. We implemented a user system.
2. Not all cars should be visible to all users as they have private information to each user.

Well, actually let's not imagine - we actually did that (albeit in a very trivial way) so as to drive home the point of how
we could restrict access to things using the "Authorize" function.

We are going to create a new UserContext in UserContext.cs that has a fixed active user "Alice" (a high tech user system, we know). 
```csharp
namespace PopcornNetCoreExample.Models
{
    public class UserContext
    {
        public string user = "Alice";
    }
}
```

We also added a new "User" property to our "Cars" model and projection.
```csharp
public string User { get; set; }
public class Car
{
    public string Model { get; set; }
    public string Make { get; set; }
    public int Year { get; set; }
	...

    public string User { get; set; }
}

public class CarProjection
{
    public string Model { get; set; }
    public string Make { get; set; }
    public int? Year { get; set; }
	...

    public string User { get; set; }
}
```

```

Then, we assigned a "User" value to the mock entries we have in our CreateExampleDatabase function in Startup.cs.
You'll see all users are set to 'Alice' currently in the example project, but just change the values as shown below to 
see the effects of the Authorize function.
```csharp
var context = new ExampleContext();
...
var firebird = new Car
{
	...
    User = "Alice"
};
...

var ferrari = new Car
{
	...
    User = "Steve"
};
...

var porsche = new Car
{
	...
    User = "Damon"
};
```

Lastly, the Authorize function was set on our Popcorn configuration in Startup.cs and we also had to add our new UserContext to our 
contexts in SetContext
```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
	popcornConfig
		...
		.SetContext(new Dictionary<string, object>
		{
			...
			["activeUser"] = userContext.user
		})
		.Authorize<Car>((source, context, value) => {
			return value.User == context["activeUser"];
		});
});
```

Prior to adding the authorization setting, a call to our cars endpoint would have returned all car objects in the database:
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

Let's see what happens now with the addition of authorization.
```javascript
http://localhost:49699/api/example/cars

{
    "Success": true,
    "Data": [
        {
            "Model": "Firebird",
            "Make": "Pontiac",
            "Year": 1981
        }
    ]
}
```

Now what happened to the other two cars? Simply put, because Alice was only permitted access to the "Firebird" that was the only 
car returned here.
The Authorize function went through every response result and looked into our UserContext as we told it to, seeing if the 
"activeUser" on the response object was the same as the one specified in the UserContext, i.e. Alice.

### Pro-Tip: No context is necessary
It is not required that you provide a context to compare against, so we could have dropped UserContext entirely and simply 
made the Authorize function read as below:
```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
	popcornConfig
		...
		.SetContext(new Dictionary<string, object>
		{
			...
		})
		.Authorize<Car>((source, context, value) => {
			return value.User == "Alice";
		});
});
```

There are obvious limitations with hardcoding magic values into your program, but the flexibility is there to do as you see fit.

Boom! That's Authorize in action and we hope you find it useful.