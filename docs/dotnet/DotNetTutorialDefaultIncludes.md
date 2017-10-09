# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Default Includes

By now you've learned how to 'project' a data entity into another type and as we've said before 
if that sentence didn't make sense, you should probably go back and complete [Getting Started](DotNetTutorialGettingStarted.md) first.
We'll still be here when you get back.

Ok, so now you understand the power of projections, but you don't want to have to specify what should be included 
in a response object by default - well we've got an answer.

This tutorial will walk you through the two ways you can declare default properties on your projections - while also discussing a way
to include subproperties on an object by default.

First let's start with an example of the reason we are talking about this at all!
Let's look back at our intial Employee projection
```csharp
public class EmployeeProjection
{
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string FullName { get; set; }

	public string Birthday { get; set; }
	public int? VacationDays { get; set; }

	public List<CarProjection> Vehicles { get; set; }
}
```

Now, assuming you've mapped the projection as we've discussed in [Getting Started](DotNetTutorialGettingStarted.md) let's see what
comes back from our "Employee" endpoint with no include statement

```javascript
http://localhost:50353/api/example/employees

{
	"FirstName": "Liz",
	"LastName": "Lemon",
	"FullName": "Liz Lemon",
	"Birthday": "05/01/1981",
	"VacationDays": 0,
	"Vehicles": [
		{
			"Model": "Firebird",
			"Make": "Pontiac",
			"Year": 1981
		}
	]
},
{
	"FirstName": "Jack",
	"LastName": "Donaghy",
	"FullName": "Jack Donaghy",
	"Birthday": "07/12/1957",
	"VacationDays": 300,
	"Vehicles": [
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

Yowza! What if we know that the standard request for employees should only include the "FirstName" and "LastName" by default as our database may be
enormous and returning all information unnecessarily may be frivilous.

Enter default properties.

### Option 1: Declaring default properties on the projection

This is our personal favorite way to declare defaults as it is very easily maintained and allows a lot of visibility into defaults.
Plus, [SPOILER ALERT] it blends seamlessly with the subproperty default include system.

So let's do this!

Go back to your Employee projection and add the property [IncludeByDefault] to "FirstName" and "LastName"
```csharp
public class EmployeeProjection
{
	[IncludeByDefault]
	public string FirstName { get; set; }
	[IncludeByDefault]
	public string LastName { get; set; }
	public string FullName { get; set; }

	public string Birthday { get; set; }
	public int? VacationDays { get; set; }

	public List<CarProjection> Vehicles { get; set; }
}
```

Now boot up the project and make the employees request again with no include statement.
```javascript
http://localhost:50353/api/example/employees

{
	"FirstName": "Liz",
	"LastName": "Lemon"
},
{
	"FirstName": "Jack",
	"LastName": "Donaghy"
}
```

Ahhh, that's better. Simple and to the point. Now have no fear, you can still send a request out with ?include=[...] and access
all of the properties exposed on the projection that you would like, completely overriding the DefaultInclude statement.

### Option 2: Including a DefaultIncludes string at "Map" time

"Map" time is not to be confused with nap time because there is nothing short of pure, thrilling excitement happening here, so 
let's go through the second option.
You'll remember in our [Advanced Projections Tutorial](DotNetTutorialAdvancedProjections.md) we explained translations on the mappings.
We didn't give you all the good news there because you can also pass in a defaultIncludes string at the time of a mapping to include certain properties 
by default.

Let's shift to the Car Projection and see how that is applied there
```csharp
public class CarProjection
{
	public EmployeeProjection Owner { get; set; }

	public string Model { get; set; }
	public string Make { get; set; }
	public int? Year { get; set; }
	public string Color { get; set; }
}
```

As you've seen above with the Employee Projection, with no default includes set a request of the "cars" endpoint with no include statement 
will return all 5 exposed properties. Again, let us say we only care to see Make, Model, and Year by default on our returned objects.

We return to our mapping statement in Startup.cs and configure it as seen below, ignoring the translation for clarity.
```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
	popcornConfig
		.Map<Car, CarProjection>(defaultIncludes: "[Model,Make,Year]", config: (carConfig) => {
			...
		}
```

Now if we fired up our program again and made the "cars" request we would see a response like the below
```javascript
http://localhost:50353/api/example/employees

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
```

Voila! Default inclusion complete

## Important caveat
It is important to mention that both option 1 and option 2 can't be used at the same time on any single projection - mostly because we are trying to 
prevent a disaster of managing references in your future.

"Did I set the defaults in my Startup file or projection for this object? CTRL+ALT+DELETE.. I quit"


### Default includes on subproperties
We've also created a handy "SubPropertyDefaultIncludes" property that can be put on a projection to have any subordinate object
returned with a set of default properties.

Things to remember:
1. The SubPropertyDefaultIncludes will override any DefaultIncludes (be it mapped or a property) on the actual projection being referenced.
2. Just like with DefaultIncludes, the sub-entities properties can still be included in the ?include=[..[..]] statement to override the SubPropertyDefaultIncludes.
	
Again, we return to our trusty EmployeeProjection to see this feature in use:
```csharp
public class EmployeeProjection
{
	[IncludeByDefault]
	public string FirstName { get; set; }
	[IncludeByDefault]
	public string LastName { get; set; }
	public string FullName { get; set; }

	public string Birthday { get; set; }
	public int? VacationDays { get; set; }

	[SubPropertyIncludeByDefault("[Make,Model,Color]")]
	public List<CarProjection> Vehicles { get; set; }
}
```

You'll see that we added a "SubPropertyIncludeByDefault" to our Vehicles property that specifies a standard "include" format list of properties.
As mentioned above, by default the "employees?include=[Vehicle]" request will return the Make, Model, and Year as that
is the default set to the car projection.

Now look at what happens with the addition of the "SubPropertyIncludeByDefault" property.

```javascript
http://localhost:49695/api/example/employees?include=[Vehicles]
{
	"Vehicles": [
		{
			"Model": "Firebird",
			"Make": "Pontiac",
			"Color": "Blue"
		}
	]
},
{
	"Vehicles": [
		{
			"Model": "250 GTO",
			"Make": "Ferrari N.V.",
			"Color": "Red"
		},
		{
			"Model": "Cayman",
			"Make": "Porsche",
			"Color": "Yellow"
		}
	]
}
```

And that is our tutorial and the various of ways of setting defaults.