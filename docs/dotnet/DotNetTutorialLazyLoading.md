# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Lazy Loading

[Lazy Loading](https://en.wikipedia.org/wiki/Lazy_loading) is a design pattern used to save processing power when creating or retrieving objects. It does this by only loading associated entities when required to do so.

An example of where lazy loading is beneficial is a list of authors on a blog site. There could be many authors and each author could have many blog posts associated with them. A view is created to show all authors that have
a post on the site. Without lazy loading, you run the risk of pulling back all posts for all authors in that list. For a large list, this could be very expensive and wasteful if the purpose of the view is not to show posts
but only author information. Lazy loading ensures that posts will only be retrieved when asked for by the user.

Popcorn performs lazy loading for you on anything that is recognized as a [navigation property](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/navigation-property). In order to use lazy loading, simply call the
mapEntityFramwork() method on a PopcornConfiguration object.

```csharp
 var config = new PopcornConfiguration(_expander);

 config.MapEntityFramework<Project, ProjectProjection, TestModelContext>(TestModelContext.ConfigureOptions());
 config.MapEntityFramework<PopcornCoreTest.Models.Environment, EnvironmentProjection, TestModelContext>(TestModelContext.ConfigureOptions());
 config.MapEntityFramework<Credential, CredentialProjection, TestModelContext>(TestModelContext.ConfigureOptions());

```

Simply pass in two classes, a source type and a destination type, along with the DbContext used to load the data. In the above example, the source type is an Entity Framework entity and the destination type is 
a projection used by Popcorn to return data in the format required for the client.

That is it! Popcorn handles the lazy loading for you with this one call. Now you can load your data with confidence and ensure great performance for your clients.