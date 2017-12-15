using PopcornNetFrameworkExample.Projections;
using PopcornNetFramework.WebApi;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using PopcornNetFrameworkExample.Models;
using Unity;

namespace PopcornNetFrameworkExample
{
    public static class WebApiConfig
    {
        private static EmployeeProjection EmployeeFactory(Dictionary<string, object> context)
        {
            return new EmployeeProjection
            {
                Employment = context["defaultEmployment"] as EmploymentType?
            };
        }

        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.UsePopcorn((popcornConfig) => {
                var database = UnityConfig.Container.Resolve<ExampleContext>();
                var userContext = new UserContext();
                popcornConfig
                    .EnableBlindExpansion(true)
                    .SetDefaultApiResponseInspector()
                    .Map<Employee, EmployeeProjection>(config: (employeeConfig) =>
                    {
                        // For employees we will determine a full name and reformat the date to include only the day portion.
                        employeeConfig
                            .Translate(ep => ep.FullName, (e) => e.FirstName + " " + e.LastName)
                            .Translate(ep => ep.Birthday, (e) => e.Birthday.ToString("MM/dd/yyyy"));
                    })
                    .Map<Car, CarProjection>(defaultIncludes: "[Model,Make,Year]", config: (carConfig) =>
                    {
                        // For cars we will query to find out the Employee who owns the car.
                        carConfig
                            .Translate(cp => cp.Owner, (car, context) =>
                                // The car parameter is the source object; the context parameter is the dictionary we configure below.
                                (context["database"] as ExampleContext).Employees.FirstOrDefault(e => e.Vehicles.Contains(car)));
                    })
                    .Map<Models.Employee, EmployeeProjection>(config: (employeeConfig) =>
                    {
                        employeeConfig
                            .Translate(ep => ep.InsuredVehicles, (e) => e.GetInsuredCars());
                    })
                    .AssignFactory<EmployeeProjection>((context) => EmployeeFactory(context))
                    // Pass in our 'database' via the context
                    .SetContext(new Dictionary<string, object>
                    {
                        ["database"] = database,
                        ["defaultEmployment"] = EmploymentType.Employed,
                        ["activeUser"] = userContext.user
                    })
                    .Authorize<Car>((source, context, value) => {
                        return value.User == (string)context["activeUser"];
                    });
            });
        }
    }
}
