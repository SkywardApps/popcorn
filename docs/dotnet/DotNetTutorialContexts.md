# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Setting Contexts

Popcorn does not know about everything in your application on its own. Actually, it's aware of virtually nothing without being specifically told and thus 
we use .NET MVC options with the Popcorn Configuration as an abstraction layer between whatever it is you're doing on your back end and your API.

Enter the need for the ability to give context to Popcorn so it has access to the necessary bits and pieces to serve your API appropriately.

## Usage
This is actually extremely simple in explanation, but very complex in the way it can be used. All you have to do is declare "SetContext" as seen 
below and in Startup.cs with a Dictionary<string, object> of all the contexts you'd like popcorn to be aware of. The generic object usage means 
that virtually anything can be passed in as a context.

```csharp
services.AddMvc((mvcOptions) =>
{
    mvcOptions.UsePopcorn((popcornConfig) => {
        popcornConfig
            .SetContext(new Dictionary<string, object>
            {
                ["database"] = database,
                ["defaultEmployment"] = EmploymentType.Employed,
                ["activeUser"] = userContext.user
            })
```

We don't limit your options on what you can use as a context beyond requiring the contexts be entered in a Dictionary format
(enter the complexity) because contexts will look quite different from project to project.

You'll see in our tutorials on [Authorizers](docs/dotnet/DotNetTutorialAuthorizers.md) and [Advanced Projections](docs/dotnet/DotNetTutorialAdvancedProjections.md) that we give a few trivial examples on how you can set some contexts to be used 
within Popcorn itself. Authorizers uses a context to see what the "active user" is for each request, while Advanced Projections shows how a default Employment 
context can be used with factories. We also shouldn't forget the obvious that you'll probably need to make your database accessible to Popcorn as a context!

