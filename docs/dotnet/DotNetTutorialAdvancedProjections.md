# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Advanced Projections

By now you've learned how to 'project' a data entity into another type, allowing you to filter out any properties that your api consumer didn't want.
If that sentence didn't make sense, you should probably go back and complete [Gettings Started](DotNetTutorialGettingStarted.md) first.
We'll still be here when you get back.

Next we'll explore some of the other options that using a projection exposes.  We will start out with something simple; attaching a new property.  
We'll have to add it to the class EmployeeProjection:

```csharp
public string FullName { get; set; }
```

Our 'FullName' with be FirstName and LastName concatenated together with a space in between.  To incorporate this, we need to add a 'Translation'.
A transltion is a custom unit of code you can add to convert the source object into a property on the destination projection.  It can translate an
existing property on the source, aggreate or combine multiple properties, or create data that doesn't exist at all on the source object.

But as every English teacher I've ever had has drilled into my head, "Show don't tell", so go ahead an throw a translation into the configuration object:

```csharp
mvcOptions.UsePopcorn((popcornConfig) => {
    popcornConfig
        .Map<Employee, EmployeeProjection>(config: (employeeConfig) => {
            employeeConfig.Translate(ep => ep.FullName, (e) => e.FirstName + " " + e.LastName);
        })
        .Map<Car, CarProjection>(defaultIncludes: "[Model,Make,Year]");
});
```

Send that Employee Api endpoint out on a test drive, and you should get something like:

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

Suddenly the client no longer has to figure out the employee's full name on their own! That'll save them nanoseconds of effort!

Ok, I have a confession -- the above example is probably better represented as a calculated property on the projection itself:

```csharp
public string FullName { get { return this.FirstName + " " + this.LastName; }
```

Which would have resulted in exactly the same output, but is a bit easier to maintain. But it sure did provide a demonstration of adding a translation!
Don't worry, there are a couple of actually pertinent situations that a translator is directly useful.

### Situation 1: Altering incoming data

Our 'Birthday' output format is a standard ISO datetime, which includes a time -- which doesn't really make sense to track for a birthday (Unless we have the 
most obsessive HR department ever). We can update the generated output to only display the day/month/year part by changing the projection's property to a
string type, and add a translation to convert:

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

Spin up that same endpoint and witness the magestic transformation our data has undergone:

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
car.  The car entity itself has no knowledge of this, so we'll have to iterate over each employee to figure the relationship out.  (This is not a recommended
solution as the performance here for any non-trivial number of entities will be like walking through a wet marsh in clown shoes, but again, demonstration
of a concept at work here).

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

