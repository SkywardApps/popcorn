using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PopcornNetCoreExampleIntegrationTest._Utilities;
using PopcornNetFrameworkExample.Models;
using PopcornNetFrameworkExample.Projections;
using PopcornNetFrameworkExample.Wire;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonIntegrationTest
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

        // A factory generated property comes through despite not being included
        [TestMethod]
        public async Task FactoryGeneratedPropertyNotRequested()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.employeesRelUrl + $"?include=[{nameof(EmployeeProjection.FullName)}, {nameof(EmployeeProjection.InsuredVehicles)}]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<EmployeeProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.FirstName != null).ShouldBeFalse();
            result.Any(c => c.FullName == null).ShouldBeFalse();

            result.Any(c => c.Employment != EmploymentType.Employed).ShouldBeFalse();
        }

        // A factory generated property does not overwrite the actual database value
        [TestMethod]
        public async Task FactoryGeneratedPropertyRequested()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.employeesRelUrl + $"?include=[{nameof(EmployeeProjection.FullName)}, {nameof(EmployeeProjection.FirstName)}, {nameof(EmployeeProjection.Employment)}]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<EmployeeProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.LastName != null).ShouldBeFalse();
            result.Any(c => c.FullName == null).ShouldBeFalse();

            result.FirstOrDefault(n => n.FirstName == "Liz").Employment.ShouldBe(EmploymentType.FullTime);
            result.FirstOrDefault(n => n.FirstName == "Jack").Employment.ShouldBe(EmploymentType.PartTime);
        }

        // A factory generated property comes through despite not being included
        [TestMethod]
        public async Task TranslatedProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.employeesRelUrl + $"?include=[{nameof(EmployeeProjection.Birthday)}]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<EmployeeProjection>>(json.Data.ToString());

            // Assert that the mapping applied accurately and the desired results came through
            result.Any(c => c.FirstName != null).ShouldBeFalse();
            result.Any(c => DateTimeOffset.Parse(c.Birthday).ToString("MM/dd/yyyy") != c.Birthday).ShouldBeFalse();
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
            json.ErrorCode.ShouldBe("Skyward.Popcorn.SelfReferencingLoopException");
        }

        // An include including an invalid property returns a proper error
        [TestMethod]
        public async Task IncludeNonExistentProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?include=[{nameof(CarProjection.Insured)}, {nameof(CarProjection.Year)}, {nameof(CarProjection.Make)}, Fishy]");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

            json.Success.ShouldBeFalse();
            json.ErrorCode.ShouldBe(typeof(ArgumentOutOfRangeException).FullName);
            json.ErrorMessage.ShouldContain("Specified argument was out of the range of valid values.");
            json.ErrorMessage.ShouldContain("Parameter name: Fishy"); // Split these into separate lines to handle for linux/windows line break differences
        }

        // A complete null response object returned
        [TestMethod]
        public async Task ResponseObjectNull()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.nullRelUrl);

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);

            // Assert failure and appropriate error
            json.Success.ShouldBeTrue();
            json.Data.ShouldBeNull();
        }

        // A complete error response object returned from a server side error (i.e. not Popcorn itself)
        [TestMethod]
        public async Task ResponseObjectServerError()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.errorRelUrl);

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.Moved);

            var json = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

            // Assert failure and appropriate error
            json.Success.ShouldBeFalse();
            json.ErrorCode.ShouldBe(typeof(ArgumentException).FullName);
            json.ErrorMessage.ShouldBe("This is a test exception");
        }
    }
}
