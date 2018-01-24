using CommonIntegrationTest._Utilities;
using ExampleModel.Projections;
using ExampleModel.Wire;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonIntegrationTest
{
    [TestClass]
    public class SortTests
    {
        // Sorting based on a simple property works properly
        [TestMethod]
        public async Task SortSimpleProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl +$"?sort={nameof(CarProjection.Make)}");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Do a sorting on the result object and make sure it sdefaults to Ascending and aligns properly
            var comparisonShell = result.OrderBy(i => i.Make).ToList();
            Enumerable.SequenceEqual(result, comparisonShell).ShouldBeTrue();
        }

        // Sorting based on a complex property throws an error
        [TestMethod]
        public async Task SortComplexProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?include=[{nameof(CarProjection.Owner)}]&sort={nameof(CarProjection.Owner)}");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

            // Assert the error throws appropriately and has the correct error code
            json.Success.ShouldBeFalse();
            json.ErrorCode.ShouldBe(typeof(ArgumentException).FullName);
            json.ErrorMessage.ShouldBe("At least one object must implement IComparable.");
        }

        // Sorting based on a non-existent property throws an error
        [TestMethod]
        public async Task SortNonExistentProperty()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?sort=Fishy");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

            // Assert the error throws appropriately and has the correct error code
            json.Success.ShouldBeFalse();
            json.ErrorCode.ShouldBe(typeof(InvalidCastException).FullName);
            json.ErrorMessage.ShouldBe("Fishy");
        }

        // Sorting with an Ascending sort direction sorts properly
        [TestMethod]
        public async Task SortAscendingSortDirection()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?sort={nameof(CarProjection.Model)}&sortDirection=Ascending");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Do a sorting on the result object and make sure it defaults to Ascending and aligns properly
            var comparisonShell = result.OrderBy(i => i.Model).ToList();
            Enumerable.SequenceEqual(result, comparisonShell).ShouldBeTrue();
        }

        // Sorting with a Descending sort direction sorts properly
        [TestMethod]
        public async Task SortDescendingSortDirection()
        {
            var response = await TestSetup.Client.GetAsync(Utilities.carsRelUrl + $"?sort={nameof(CarProjection.Model)}&sortDirection=Descending");

            // convert the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Response>(responseBody);
            var result = JsonConvert.DeserializeObject<List<CarProjection>>(json.Data.ToString());

            // Do a sorting on the result object and make sure it defaults to Ascending and aligns properly
            var comparisonShell = result.OrderByDescending(i => i.Model).ToList();
            Enumerable.SequenceEqual(result, comparisonShell).ShouldBeTrue();
        }
    }
}
