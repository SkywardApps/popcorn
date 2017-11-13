# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Wildcard Includes

[Table Of Contents](../../docs/TableOfContents.md)

With Popcorn, you have the ability to set [Default Includes](DotNetTutorialDefaultIncludes.md) and make 
specific ?include=[...] requests with your API calls to condense response objects. There is also a wildcard option "*" that can be sent with the ?include 
statement to get *all* non-null values from an endpoint, regardless of the defaults.

Let's go through an example of what that looks like in action:
```csharp
public class Employee
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [InternalOnly(true)]
    public long SocialSecurityNumber { get; set; }

    public DateTimeOffset Birthday { get; set; }
    public EmploymentType Employment { get; set; }
    public int VacationDays { get; set; }

    public List<Car> Vehicles { get; set; }

    public List<Car> GetInsuredCars() {
        return Vehicles.Where(c => c.Insured == true).ToList();
    }
}

public class EmployeeProjection
{
    [IncludeByDefault]
    public string FirstName { get; set; }
    [IncludeByDefault]
    public string LastName { get; set; }
    public string FullName { get; set; }

    public long? SocialSecurityNumber { get; set; }

    public string Birthday { get; set; }
    public int? VacationDays { get; set; }
    public EmploymentType? Employment { get; set; }

    [SubPropertyIncludeByDefault("[Make,Model,Color]")]
    public List<CarProjection> Vehicles { get; set; }

    public List<CarProjection> InsuredVehicles { get; set; }
}
```

You'll see on the EmployeeProjection the FirstName and LastName properties are set to be included by default. 
If we make a generic request to a GET Employees endpoint we see the following response
```javascript
http://localhost:50353/api/example/employees

{
    "Success": true,
    "Data": [
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
}
```

If, for example, we actually want to know everything about our employees to render all of their information on an admin page, we can make the following call
**Note: We ignored the SocialSecurityNumber because it's marked InternalOnly**
```javascript
http://localhost:49699/api/example/employees?include=[FirstName,LastName,FullName,Birthday,VacationDays,Employment,Vehicles,InsuredVehicles]
```

While adding properties dynamically to an include statement with some nice front end logic isn't that difficult to maintain, 
making calls that include long strings of includes can get cumbersome when debugging, making one off requests, testing, or in simple use cases.

The wildcard "*" can be used as a replacement to return all non-null values on response objects:
```javascript
http://localhost:49699/api/example/employees?include=[*]

{
    "Success": true,
    "Data": [
        {
            "FirstName": "Liz",
            "LastName": "Lemon",
            "FullName": "Liz Lemon",
            "Birthday": "05/01/1981",
            "VacationDays": 0,
            "Employment": "FullTime",
            "Vehicles": [
                {
                    "Model": "Firebird",
                    "Make": "Pontiac",
                    "Color": "Blue",
                    "Insured": false
                }
            ],
            "InsuredVehicles": [
                {
                    "Model": "Firebird",
                    "Make": "Pontiac",
                    "Year": 1981,
                    "Insured": false
                }
            ]
        },
        {
            "FirstName": "Jack",
            "LastName": "Donaghy",
            "FullName": "Jack Donaghy",
            "Birthday": "07/12/1957",
            "VacationDays": 300,
            "Employment": "PartTime",
            "Vehicles": [
                {
                    "Model": "250 GTO",
                    "Make": "Ferrari N.V.",
                    "Color": "Red",
                    "Insured": false
                },
                {
                    "Model": "Cayman",
                    "Make": "Porsche",
                    "Color": "Yellow",
                    "Insured": false
                }
            ],
            "InsuredVehicles": [
                {
                    "Model": "Cayman",
                    "Make": "Porsche",
                    "Year": 2005,
                    "Insured": false
                }
            ]
        }
    ]
}
```

#### Important Notes
+ The wildcard can be used on an expandable sub property as well (like Vehicles in the example above)
	+ It also can be used on BlindExpanded or mapped objects, assuming BlindExpansion is enabled
+ Nesting wildcards will throw an error i.e. "?include=[FirstName, *, LastName]"