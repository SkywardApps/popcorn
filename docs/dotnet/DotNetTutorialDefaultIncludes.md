# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Default Includes

[Table Of Contents](../../docs/TableOfContents.md)

If you are unfamiliar with how to 'project' a data entity into another type please complete the [Getting Started](DotNetTutorialGettingStarted.md) first.

Popcorn's projections are very powerful, but you probably don't want to have to specify what should be included 
in a response object every time.

This tutorial will walk you through the two ways you can declare default properties on your projections - while also discussing a way
to include subproperties on an object by default.

First let's start with an example!
Let's look at our intial Employee object and its projection
```csharp
public class Employee
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public DateTimeOffset Birthday { get; set; }
    public int VacationDays { get; set; }

    public List<Car> Vehicles { get; set; }
}
```

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

That is a lot of information, but what if we know that the standard request for employees should only include the "FirstName" and "LastName" by default, as our database may be enormous and returning all information unnecessarily is time consuming.

The answer is default properties.

### Option 1: Declaring default properties on the projection

This is the recommended way to declare defaults as it is very easily maintained and makes it clear which defaults have been set.
Plus, it blends seamlessly with the subproperty default include system.



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

Much more streamlined, and we can still send a request out with ?include=[...] and access
all of the properties exposed on the projection, overriding the DefaultInclude statement.

*Important FYI: Derived classes will inherit their base class' [IncludeByDefault]'s by default and can be overridden 
in the usual fashion with a specific ?include in a request.* 

### Option 2: Including a DefaultIncludes string at "Map" time

You'll remember in our [Advanced Projections Tutorial](DotNetTutorialAdvancedProjections.md) we explained translations on the mappings.
In addition to that functionality, you can also pass in a defaultIncludes string at the time of a mapping to include certain properties 
by default.

Let's shift to the Car object and projection and see how that is applied there
```csharp
public class Car
{
    public string Model { get; set; }
    public string Make { get; set; }
    public int Year { get; set; }

    public enum Colors
    {
        Black,
        Red,
        Blue,
        Gray,
        White,
        Yellow,
    }
    public Colors Color { get; set; }
}

```csharp
public class CarProjection
{
	public string Model { get; set; }
	public string Make { get; set; }
	public int? Year { get; set; }
	public string Color { get; set; }
}
```

As you've seen above with the Employee Projection, with no default includes, making a request to the "cars" endpoint 
will return all 4 exposed properties. Again, let us say we only care to see Make, Model, and Year by default on our returned objects.

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

Voila! Default inclusion complete.

## Important caveat
It is important to mention that both option 1 and option 2 can't be used at the same time on any single projection in order to help maintain clarity of design.


### Default includes on subproperties
We've also created a "SubPropertyDefaultIncludes" property that can be put on a projection to have any subordinate object
returned with a set of default properties.

Things to remember:
1. The SubPropertyDefaultIncludes will override any DefaultIncludes (be it mapped or a property) on the actual projection being referenced.
2. Just like with DefaultIncludes, the sub-entities properties can still be included in the ?include=[..[..]] statement to override the SubPropertyDefaultIncludes.
	
Again, we return to EmployeeProjection to see this feature in use:
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

Now let's see what happens with the addition of the "SubPropertyIncludeByDefault" property.

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
