# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Expand From

[Table Of Contents](../../docs/TableOfContents.md)

The power of Popcorn comes in its ability to expand objects dynamically based on the specified object's properties.

There are currently 3 ways that a developer can declare an object to be "Mapped" so as to have it able to be expanded 
by Popcorn.
 1. [Blind expansion](DotNetTutorialBlindExpansion.md) - The most limiting option, but the quickest to configure
 2. [Mapping on the Popcorn configuration](DotNetQuickStart.md) - The most permissive and customizable option, but the slowest to configure
 3. ExpandFrom declaration on a projection - Arguably the best combination of quick setup with sturdy type declarations

 Let's get right to explaining the ExpandFrom attribute

 ## Overview
 
 The ExpandFrom attribute is declared on an object's projection to tell Popcorn specifically where to look when attempting to 
 expand an object.
 The ExpandFrom object takes two properties:
  1. The type of the class to expand from
  2. Properties to include by default

*This is where the limitations show a little. It is easy to configure the ExpandFrom property, but it doesn't extend 
as many customizable options as mapping on the popcorn configuration declaration does*

#### Potential Gotcha
+ Declaring default includes can get messy as they can be declared in a few different places so declaring properties to be included by default 
within the ExpandFrom attribute while also declaring IncludeByDefault attributes on properties within that class will throw an error.

### Example
Let's say we have a Manager class that inherits from an Employee class:
```csharp
public class Employee
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public DateTimeOffset Birthday { get; set; }
    public EmploymentType Employment { get; set; }
    public int VacationDays { get; set; }

    public List<Car> Vehicles { get; set; }
}

public class Manager : Employee
{
    public List<Employee> Subordinates { get; set; }
}
```

We want to declare a projection:
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
    public EmploymentType? Employment { get; set; }

    [SubPropertyIncludeByDefault("[Make,Model,Color]")]
    public List<CarProjection> Vehicles { get; set; }
}

public class ManagerProjection : EmployeeProjection
{
    [IncludeByDefault]
    public List<EmployeeProjection> Subordinates { get; set; }
}
```

Now, it doesn't really matter how we declare the mapping of the base EmployeeProjection class, but let's say we quickly want to declare the mapping 
for the ManagerProjection.
All we need to do is add the ExpandFrom attribute to our Manager projection and we are done!
```csharp
    [ExpandFrom(typeof(Manager))]
public class ManagerProjection : EmployeeProjection
{
    [IncludeByDefault]
    public List<EmployeeProjection> Subordinates { get; set; }
}
```

Now, an important thing to notice here is we elected to not declare default includes in our ExpandFrom attribute for two reasons:
 1. As Manager is an inherited class we prefer to use the IncludeByDefault attributes on the base and inherited classes respectively 
 so we don't have to track properties in two places.
 2. As mentioned above, we can't declare both IncludByDefault and default includes - error city!

That's really it! If we make a call to our GET "Managers" endpoint we see the below as our base response and we can customize it with include 
statements to our heart's content.
```javascript
http://localhost:49699/api/example/managers

{
    "Success": true,
    "Data": [
        {
            "Subordinates": [
                {
                    "FirstName": "Liz",
                    "LastName": "Lemon",
                    "Employment": "Employed"
                }
            ],
            "FirstName": "Stacy",
            "LastName": "Hughes"
        },
        {
            "Subordinates": [
                {
                    "Subordinates": [
                        {
                            "FirstName": "Liz",
                            "LastName": "Lemon",
                            "Employment": "Employed"
                        }
                    ],
                    "FirstName": "Stacy",
                    "LastName": "Hughes"
                },
                {
                    "FirstName": "Jack",
                    "LastName": "Donaghy",
                    "Employment": "Employed"
                }
            ],
            "FirstName": "Jamal",
            "LastName": "Henry"
        }
    ]
}
```