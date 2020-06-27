using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PopcornNetCoreEntityFrameworkCoreTests
{
    public class Car
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid EmployeeId { get; set; }
    }

    public class Employee
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Car> Cars { get; set; }
    }


    public class TestContext : DbContext
    {
        public TestContext(string name) : base(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name)
            .UseLazyLoadingProxies()
            .Options) {
        }

        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public virtual DbSet<Car> Cars { get; set; }
        public virtual DbSet<Employee> Employees { get; set; }

    }


    [TestClass]
    public class TestEntityFrameworkCore
    {
        string dbname = Guid.NewGuid().ToString();

        [TestInitialize]
        public void Setup()
        {
            using (var context = new TestContext(dbname))
            {
                var employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    Name = "Bob McMuffin"
                };

                context.Employees.Add(employee);
                context.Cars.Add(new Car
                {
                    Id = Guid.NewGuid(),
                    Name = "Blue Angel",
                    EmployeeId = employee.Id
                });

                context.SaveChanges();
            }
        }

        [TestMethod]
        public async Task SimpleExpandLazyLoading()
        {
            using (var context = new TestContext(dbname))
            {
                // Get the employee that has cars lazily loaded
                var employee = await context.Employees.FirstOrDefaultAsync();
                employee.ShouldNotBeNull();
                employee.Cars.ShouldNotBeNull();

                // Set up our expander
                var expander = new Expander();
                var config = new PopcornConfiguration(expander);
                config.EnableBlindExpansion(true);

                // Expand it and make sure we only have our three expected properties.
                var expanded = (Dictionary<string, object>)expander.Expand(employee);
                expanded.ShouldNotBeNull();
                expanded.Count.ShouldBe(3);
            }

        }
    }
}
