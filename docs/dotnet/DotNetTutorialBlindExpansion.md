# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Blind Expansion

We may not want to actually have to project out all of your objects and map them as that may not be a layer of abstraction 
you or your team care to have. We recognize that while projections do offer another layer of protection and 
more flexiblity within Popcorn, our users need to be able to quickly get started and get moving.

Enter blind expansions! By simply designating EnableBlindExpansion to true within the project config, we are able to skip over the 
projecting and mapping steps completely.

## Usage
Let's go back to our example project and add a new class to our Models, "Business"
```csharp
public class Business
{
    public string Name { get; set; }
    public string StreetAddress { get; set; }
    public int ZipCode { get; set; }
        
    public List<Employee> Employees { get; set; }

    public enum Purposes
    {
        Shoes,
        Vehicles,
        Clothes,
        Tools
    }
    public Purposes Purpose { get; set; }
}
```

We also need to add example data to our context and add a new endpoint, "businesses" to our example controller.
```csharp
// Endpoint addition
[HttpGet, Route("businesses")]
public List<Business> Businesses()
{
    return _context.Businesses;
}
```

Updating the context:
```csharp
public class ExampleContext
{
    public List<Car> Cars { get; } = new List<Car>();
    public List<Employee> Employees { get; } = new List<Employee>();
    public List<Business> Businesses { get; } = new List<Business>();
}
```

Lastly adding some mock data:
```csharp
        private ExampleContext CreateExampleDatabase()
        {
            var context = new ExampleContext();
            var liz = new Employee
            {
                FirstName = "Liz",
                LastName = "Lemon",
                Employment = EmploymentType.FullTime,
                Birthday = DateTimeOffset.Parse("1981-05-01"),
                VacationDays = 0,
                Vehicles = new List<Car>()
            };
            context.Employees.Add(liz);

            var jack = new Employee
            {
                FirstName = "Jack",
                LastName = "Donaghy",
                Employment = EmploymentType.PartTime,
                Birthday = DateTimeOffset.Parse("1957-07-12"),
                VacationDays = 300,
                Vehicles = new List<Car>()
            };
            context.Employees.Add(jack);

            var shoeBusiness = new Business
            {
                Name = "Shoe Biz",
                StreetAddress = "1234 Street St",
                ZipCode = 55555,
                Purpose = Business.Purposes.Shoes,
                Employees = new List<Employee>()
            };
            shoeBusiness.Employees.Add(jack);
            shoeBusiness.Employees.Add(liz);
            context.Businesses.Add(shoeBusiness);

            var carBusiness = new Business
            {
                Name = "Car Biz",
                StreetAddress = "4321 Avenue Ave",
                ZipCode = 44444,
                Purpose = Business.Purposes.Vehicles,
                Employees = new List<Employee>()
            };
            context.Businesses.Add(carBusiness);

            return context;
        }
```


If we want to get started with using Popcorn from here all we have to do is add the EnableBlindExpansion setting to our configuration 
as shown below and we truly are off to the races!
```csharp
services.AddMvc((mvcOptions) =>
{
    mvcOptions.UsePopcorn((popcornConfig) => {
        popcornConfig
            .EnableBlindExpansion(true)
```

Now we fire up our example project and make a call to our businesses endpoint as shown below. That's it! You'll see that the appropriate properties are returned without 
adding a mapping or a Business projection.

```javascript
http://localhost:49699/api/example/businesses

{
    "Success": true,
    "Data": [
        {
            "Name": "Shoe Biz",
            "StreetAddress": "1234 Street St",
            "ZipCode": 55555,
            "Employees": [
                {
                    "FirstName": "Jack",
                    "LastName": "Donaghy",
                    "Employment": "Employed"
                },
                {
                    "FirstName": "Liz",
                    "LastName": "Lemon",
                    "Employment": "Employed"
                }
            ],
            "Purpose": 0
        },
        {
            "Name": "Car Biz",
            "StreetAddress": "4321 Avenue Ave",
            "ZipCode": 44444,
            "Employees": [],
            "Purpose": 1
        }
    ]
}
```

Of course all good things have their benefits and limitations, so let's list them here for blind expansion:

#### Benefits
+ Very quick setup
+ Easy to configure and manage
+ Context and other mappings/defaults for other objects still respected

#### Limitations
+ No easy way to configure defaults
+ Sorting is not supported for blind objects
+ More risk as there is no sure fire way to 100% guarantee that your object being requested is able to be expanded
	+ We do take steps to minimize this and most custom object types should be expandable.