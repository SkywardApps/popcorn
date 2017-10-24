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
You can see this setup within ExampleContext.cs, ExampleController.cs, and Startup.cs, but we won't detail it here as by now, you've seen a multitude of 
examples in our other tutorials around Cars and Employees.

If we want to get started with using Popcorn from here all we have to do is add the EnableBlindExpansion setting to our configuration 
as shown below and we truly are off to the races!
```csharp
services.AddMvc((mvcOptions) =>
{
    mvcOptions.UsePopcorn((popcornConfig) => {
        popcornConfig
            .EnableBlindExpansion(true)
```

Now we fire up our example project and make a call to our businesses endpoint as shown below. That's it!
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
+ No way to configure defaults
+ Sorting is not supported for blind objects
+ More risk as there is no sure fire way to 100% guarantee that your object being requested is able to be expanded
	+ We do take steps to minimize this and most custom object types should be expandable.