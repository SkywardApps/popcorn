# [Popcorn](../../README.md) > [Quick Start](../QuickStart.md) > DotNet

There are two methods to get the code into your solution:
1. Check out the project from [GitHub](https://github.com/SkywardApps/popcorn).  Add the PopcornStandard and (probably) PopcornCore projects to your solution, and add references to them as needed.
2. (Preferred) Nuget! Open up your package manager console and type in ``` Install-Package Skyward.Api.Popcorn.DotNetCore ```. 

Now that the project is available, you can quickly get up and running by configuring the MvcOptions in your UseMvc call.

```csharp
	services.AddMvc((mvcOptions) =>
	{
		mvcOptions.UsePopcorn((popcornConfig) => {
			popcornConfig
				.Map<*SourceType*, *DestinationType*>()
				*...Repeat...*;
		});
	});
```