using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PopcornNetCoreExampleIntegrationTest._Utilities;
using PopcornNetFrameworkExample.Projections;
using PopcornNetFrameworkExample.Wire;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonIntegrationTest
{
    [TestClass]
    public class DefaultIncludesTests
    {
        // Default includes declared in the PopcornConfig are respected
        [TestMethod]
        public async Task DefaultIncludesInConfig()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl);

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.Color != null).ShouldBeFalse();
            result.Any(c => c.Owner != null).ShouldBeFalse();
            result.Any(c => c.Insured != null).ShouldBeFalse();
            result.Any(c => c.User != null).ShouldBeFalse();

            result.Any(c => c.Model == null).ShouldBeFalse();
            result.Any(c => c.Make == null).ShouldBeFalse();
            result.Any(c => c.Year == null).ShouldBeFalse();
        }

        // Default includes declared as attributes on the projection are respected
        [TestMethod]
        public async Task DefaultIncludesInAttributes()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.employeesRelUrl);

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<EmployeeProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.FirstName == null).ShouldBeFalse();
            result.Any(c => c.LastName == null).ShouldBeFalse();

            result.Any(c => c.FullName != null).ShouldBeFalse();
        }

        // SubProperty Default includes declared as attributes on the projection are respected
        [TestMethod]
        public async Task DefaultIncludesSubProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.employeesRelUrl + $"?include=[{nameof(EmployeeProjection.Vehicles)}]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<EmployeeProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            // Can't isolate default includes here so a little bit of overlap
            result.Any(c => c.Vehicles.Any(v => v.Color == null)).ShouldBeFalse();
            result.Any(c => c.Vehicles.Any(v => v.Make == null)).ShouldBeFalse();
            result.Any(c => c.Vehicles.Any(v => v.Model == null)).ShouldBeFalse();

            result.Any(c => c.Vehicles.Any(v => v.Year != null)).ShouldBeFalse();
        }
    }
}
