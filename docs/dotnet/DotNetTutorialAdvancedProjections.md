# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Advanced Projections

[Table Of Contents](../TableOfContents.md)

By now you've learned how to 'project' a data entity into another type, allowing you to filter out any properties that your api consumer didn't want.
If that process is unclear, we recommend that you complete [Getting Started](DotNetTutorialGettingStarted.md) first.


Next we'll explore some of the other options that using a projection exposes.  We will start out with something simple; attaching a new property.  
We'll add it to the class EmployeeProjection:

```csharp
public string FullName { get; set; }
```

Our 'FullName' with be FirstName and LastName concatenated together with a space in between.  To incorporate this, we need to add a 'Translation'.
A translation is a custom unit of code you can add to convert the source object into a property on the destination projection.  It can translate an
existing property on the source, aggreate or combine multiple properties, or create data that doesn't exist at all on the source object.

Let's put a translation into the configuration object to demonstrate:

```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .Map<Employee, EmployeeProjection>(config: (employeeConfig) => {
            employeeConfig.Translate(ep => ep.FullName, (e) => e.FirstName + " " + e.LastName);
        })
        .Map<Car, CarProjection>(defaultIncludes: "[Model,Make,Year]");
});
```

New use the Employee API endpoint, and you should get something like:

```javascript
http://localhost:50353/api/example/employees?include=[FirstName,LastName,FullName,Birthday]

{
    "FirstName": "Liz",
    "LastName": "Lemon",
    "FullName": "Liz Lemon",
    "Birthday": "19810501T00:00:00 GMT"
},
...

```

Suddenly the client no longer has to figure out the employee's full name on their own! 

The above example is probably better represented as a calculated property on the projection itself:

```csharp
public string FullName { get { return this.FirstName + " " + this.LastName; }
```

Which would have resulted in exactly the same output, but is a bit easier to maintain. 

### Situation 1: Altering incoming data

Our 'Birthday' output format is a standard ISO datetime, which includes a time.  Time doesn't really make sense to track for a birthday, so we'll update the generated output to display *only* the day/month/year part by changing the projection's property to a
string type, and adding a translation to convert:

```csharp
public string Birthday { get; set; }
```

```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .Map<Employee, EmployeeProjection>(config: (employeeConfig) => {
            employeeConfig
                .Translate(ep => ep.FullName, (e) => e.FirstName + " " + e.LastName)
                .Translate(ep => ep.Birthday, (e) => e.Birthday.ToString("MM/dd/yyyy"));
        })
        .Map<Car, CarProjection>(defaultIncludes: "[Model,Make,Year]");
});
```

Spin up that same endpoint and witness the transformation our data has undergone:

```javascript
http://localhost:50353/api/example/employees?include=[FirstName,LastName,FullName,Birthday]

{
    "FirstName": "Liz",
    "LastName": "Lemon",
    "FullName": "Liz Lemon",
    "Birthday": "05/01/1981"
},
...
```

### Situation 2: Integrating data external to the source object

Sometimes we want to add information to a projection that simply wasn't contained in the source object.  In order to get access to the additional
information, Popcorn has a concept of a 'context' -- that is, a Dictionary<string,object> -- that can be passed into your custom translations.  
Your translations can use this entry point to access whatever they want outside of the source object.

We can demonstrate this by adding an 'Owner' property to the CarProjection.  This owner will reference the Employee whose 'vehicles' list contains this
car.  The car entity itself has no knowledge of this, so we'll have to iterate over each employee to figure the relationship out.

```csharp
public EmployeeProjection Owner { get; set; }
```

```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .Map<Employee, EmployeeProjection>(config: (employeeConfig) => {
            // For employees we will determine a full name and reformat the date to include only the day portion.
            employeeConfig
                .Translate(ep => ep.FullName, (e) => e.FirstName + " " + e.LastName)
                .Translate(ep => ep.Birthday, (e) => e.Birthday.ToString("MM/dd/yyyy"));
        })
        .Map<Car, CarProjection>(defaultIncludes: "[Model,Make,Year]", config: (carConfig) => {
            // For cars we will query to find out the Employee who owns the car.
            carConfig.Translate(cp => cp.Owner, (car, context) => 
                // The car parameter is the source object; the context parameter is the dictionary we configure below.
                (context["database"] as ExampleContext).Employees.FirstOrDefault(e => e.Vehicles.Contains(car)));
        })
        // Pass in our 'database' via the context
        .SetContext(new Dictionary<string, object> {
            ["database"] = database
        });
});
```

Now if we request the Owner property, we'll get the details of the employee too!

```javascript
http://localhost:50353/api/example/cars?include=[Model,Make,Year,Color,Owner[FullName]]

[
    {
        "Owner": {
            "FullName": "Liz Lemon"
        },
        "Model": "Firebird",
        "Make": "Pontiac",
        "Year": 1981,
        "Color": "Blue"
    },
    {
        "Owner": {
            "FullName": "Jack Donaghy"
        },
        "Model": "250 GTO",
        "Make": "Ferrari N.V.",
        "Year": 1962,
        "Color": "Red"
    },
    {
        "Owner": {
            "FullName": "Jack Donaghy"
        },
        "Model": "Cayman",
        "Make": "Porsche",
        "Year": 2005,
        "Color": "Yellow"
    }
]
```
## Factories<a name="factories"/>
With Factories it's possible to extract the projection class instantiation. You can either use a plain factory or one that takes a context object as an input parameter. The big advantage of using a context object is that the instantiation can be configured to its needs.

In the following example the Employee model consists of a property that describes the employment type (eg. FullTime/PartTime). If this employment type is not explicitly requested, using the include parameter or by requesting the entire model, it depends on its initial value. With a context-based factory, this value can be set according to the current configuration.

```csharp
public enum EmploymentType
{
    Employed,
    PartTime,
    FullTime
}
```

```csharp
public class EmployeeProjection
{
	...
	public EmploymentType Employment { get;set; }
}
```

```csharp
popcornConfig
    .Map<Employee, EmployeeProjection>()
    .AssignFactory<EmployeeProjection>((context) => EmployeeFactory(context))
    .SetContext(new Dictionary<string, object>
    {
        ["defaultEmployment"] = EmploymentType.Employed
	});

...

private EmployeeProjection EmployeeFactory(Dictionary<string, object> context)
    {
        return new EmployeeProjection
        {
            Employment = context["defaultEmployment"] as EmploymentType?
        };
    }
```

According to this context configuration every EmployeeProjection object will be instantiated with a Employment value of `EmploymentType.Employed`.
Requesting only FirstName and LastName will provide us with a default EmploymentType:
```javascript
http://localhost:35632/api/example/employees?include=[FirstName,LastName]

[
    {
        "FirstName": "Liz",
        "LastName": "Lemon",
        "Employment": "Employed"
    },
    {
        "FirstName": "Jack",
        "LastName": "Donaghy",
        "Employment": "Employed"
    }
]
```

By including the Employment property in the request, you'll get the exact EmploymentTypes.
```javascript
http://localhost:35632/api/example/employees?include=[FirstName,LastName,Employment]
 [
	{
		"FirstName": "Liz",
        "LastName": "Lemon",
        "Employment": "FullTime"
     },
     {
		"FirstName": "Jack",
        "LastName": "Donaghy",
        "Employment": "PartTime"
     }
]
```
