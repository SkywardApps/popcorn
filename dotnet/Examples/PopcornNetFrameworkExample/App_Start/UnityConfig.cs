using ExampleModel.Models;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Mvc;
using Unity;
using Unity.WebApi;

namespace PopcornNetFrameworkExample
{
    public static class UnityConfig
    {
        public static UnityContainer Container { get; private set; }
        public static Unity.Mvc5.UnityDependencyResolver unityMvc5Resolver { get; private set; }
        public static Unity.WebApi.UnityDependencyResolver unityWebApiResolver { get; private set; }
        public static void RegisterComponents()
        {
			Container = new UnityContainer();

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            // e.g. container.RegisterType<ITestService, TestService>();
            Container.RegisterInstance(CreateExampleDatabase());

            unityMvc5Resolver = new Unity.Mvc5.UnityDependencyResolver(Container);
            DependencyResolver.SetResolver(unityMvc5Resolver);
            unityWebApiResolver = new UnityDependencyResolver(Container);
            GlobalConfiguration.Configuration.DependencyResolver = unityWebApiResolver;
        }



        private static ExampleContext CreateExampleDatabase()
        {
            var context = new ExampleContext();
            var liz = new Employee
            {
                FirstName = "Liz",
                LastName = "Lemon",
                SocialSecurityNumber = 5556667777,
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
                Color = Car.Colors.Blue,
                User = "Alice",
                Insured = true
            };
            context.Cars.Add(firebird);
            liz.Vehicles.Add(firebird);
            context.Employees.Add(liz);

            var jack = new Employee
            {
                FirstName = "Jack",
                LastName = "Donaghy",
                SocialSecurityNumber = 7776665555,
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
                Color = Car.Colors.Red,
                User = "Alice",
                Insured = false
            };
            context.Cars.Add(ferrari);
            jack.Vehicles.Add(ferrari);
            var porsche = new Car
            {
                Make = "Porsche",
                Model = "Cayman",
                Year = 2005,
                Color = Car.Colors.Yellow,
                User = "Alice",
                Insured = true
            };
            context.Cars.Add(porsche);
            jack.Vehicles.Add(porsche);
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
    }
}