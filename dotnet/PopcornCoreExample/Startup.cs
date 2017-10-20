﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PopcornCoreExample.Models;
using Skyward.Popcorn.Core;
using PopcornCoreExample.Projections;

namespace PopcornCoreExample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var database = CreateExampleDatabase();
            services.AddSingleton<ExampleContext>(database);

            // Add framework services.
            services.AddMvc((mvcOptions) =>
            {
                mvcOptions.UsePopcorn((popcornConfig) => {
                    popcornConfig
                        .SetInspector((data, context) => new Wire.Response { Data = data, Success = true })
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
                        .AssignFactory<EmployeeProjection>((context) => EmployeeFactory(context))
                        // Pass in our 'database' via the context
                        .SetContext(new Dictionary<string, object>
                        {
                            ["database"] = database,
                            ["defaultEmployment"] = EmploymentType.Employed
                        });
                });
            });
        }
        
        private EmployeeProjection EmployeeFactory(Dictionary<string, object> context)
        {
            return new EmployeeProjection
            {
                Employment = context["defaultEmployment"] as EmploymentType?
            };
        }

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
            var firebird = new Car
            {
                Make = "Pontiac",
                Model = "Firebird",
                Year = 1981,
                Color = Car.Colors.Blue
            };
            context.Cars.Add(firebird);
            liz.Vehicles.Add(firebird);
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
            var ferrari = new Car
            {
                Make = "Ferrari N.V.",
                Model = "250 GTO",
                Year = 1962,
                Color = Car.Colors.Red
            };
            context.Cars.Add(ferrari);
            jack.Vehicles.Add(ferrari);
            var porsche = new Car
            {
                Make = "Porsche",
                Model = "Cayman",
                Year = 2005,
                Color = Car.Colors.Yellow
            };
            context.Cars.Add(porsche);
            jack.Vehicles.Add(porsche);
            context.Employees.Add(jack);

            return context;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            app.UseMvc();
        }
    }
}
