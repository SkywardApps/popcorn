# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Authorizers

Restricting access to information is so important and can often be incredibly complex. We recognize this inevitable struggle
and are now offering an "Authorize" setting within your Popcorn configuration to restrict certain objects as you see fit 
and hopefully mitigate part of that trouble.

## How does it work?
In PopcornConfiguration there is now the method shown below:
```csharp
public PopcornConfiguration Authorize<TSourceType>(Func<object, ContextType, TSourceType, bool> authorizer)
```

What we are able to do with this is specify a function that takes in an object that is compared based on some context 
against the rules set for a source object type, while returning a boolean that says whether the source object should be authorized or
not.

## Example
As always, let's spend more time showing and less time telling.

Let's take the example we already have created of Cars and imagine two things changed in our example program:
1. We implemented a user system.
2. Not all cars should be visible to all users as they have private information to each user.

Well, actually let's not imagine - we actually did that (albeit in a very trivial way) so as to drive home the point of how
we can restrict access to things using the "Authorize" function.

We are going to create a new UserContext in UserContext.cs that has a fixed active user "Alice" (a high tech user system, we know). 
```csharp
namespace PopcornCoreExample.Models
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
```

