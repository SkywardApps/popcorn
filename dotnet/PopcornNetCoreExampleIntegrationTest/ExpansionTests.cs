using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PopcornNetCoreExample.Models;
using PopcornNetCoreExample.Projections;
using PopcornNetCoreExample.Wire;
using PopcornNetCoreExampleIntegrationTest._Utilities;
using Shouldly;
using Skyward.Popcorn;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornNetCoreExampleIntegrationTest
{
    [TestClass]
    public class ExpansionTests
    {
        // A simple mapped object returns properly
        [TestMethod]
        public async Task MappedObjectInConfig()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl);

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            // Can't isolate default includes here so a little bit of overlap
            result.Any(c => c.Color != null).ShouldBeFalse();
            result.Any(c => c.Owner != null).ShouldBeFalse();
            result.Any(c => c.Insured != null).ShouldBeFalse();
            result.Any(c => c.User != null).ShouldBeFalse();

            result.Any(c => c.Model == null).ShouldBeFalse();
            result.Any(c => c.Make == null).ShouldBeFalse();
            result.Any(c => c.Year == null).ShouldBeFalse();
        }

        // A blind mapped object returns properly
        [TestMethod]
        public async Task MappedObjectBlind()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.businessesRelUrl);

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<Business>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.Name != null).ShouldBeTrue();
            result.Any(c => c.StreetAddress != null).ShouldBeTrue();
            result.Any(c => c.ZipCode != 0).ShouldBeTrue();
            result.Any(c => c.Employees != new List<Employee>()).ShouldBeTrue();
        }

        // An include on simple properties returns properly
        [TestMethod]
        public async Task IncludeSimpleProperties()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?include=[{nameof(CarProjection.Insured)}, {nameof(CarProjection.Year)}, {nameof(CarProjection.Make)}]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.Color != null).ShouldBeFalse();
            result.Any(c => c.Owner != null).ShouldBeFalse();

            result.Any(c => c.Make == null).ShouldBeFalse();
            result.Any(c => c.Insured == null).ShouldBeFalse();
            result.Any(c => c.Year == null).ShouldBeFalse();
        }

        // An include on a complex property returns properly
        [TestMethod]
        public async Task IncludeComplexProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?include=[{nameof(CarProjection.Owner)}]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.Color != null).ShouldBeFalse();
            result.Any(c => c.Model != null).ShouldBeFalse();

            result.Any(c => c.Owner == null).ShouldBeFalse();
        }

        // A nested include on a sub property returns properly
        [TestMethod]
        public async Task IncludeNestedOnComplexProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?include=[{nameof(CarProjection.Owner)}[{nameof(EmployeeProjection.FirstName)}, {nameof(EmployeeProjection.Birthday)}]]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.Color != null).ShouldBeFalse();
            result.Any(c => c.Model != null).ShouldBeFalse();

            result.Any(c => c.Owner == null).ShouldBeFalse();
            result.Any(c => c.Owner.FirstName == null).ShouldBeFalse();
            result.Any(c => c.Owner.Birthday == null).ShouldBeFalse();
            result.Any(c => c.Owner.LastName != null).ShouldBeFalse();
        }

        // A self referencing include loop passes the appropriate error to the client
        [TestMethod]
        public async Task IncludeSelfReferencingLoop()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?include=[{nameof(CarProjection.Owner)}[{nameof(EmployeeProjection.Vehicles)}]]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

            // Assert failure and appropriate error
            json.Success.ShouldBeFalse();
            json.ErrorCode.ShouldBe(typeof(SelfReferencingLoopException).FullName);
        }
    }
}
