using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopcornNet5Example.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IPopcornContextAccessor _popcornAccessor;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IPopcornContextAccessor popcornAccessor)
        {
            _logger = logger;
            _popcornAccessor = popcornAccessor ?? throw new ArgumentNullException(nameof(popcornAccessor));
        }

        [HttpGet, ExpandResult]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            var range = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)],
                Hourly = (_popcornAccessor.PropertyReferences == null || _popcornAccessor.PropertyReferences.Any(pr => pr.PropertyName == "Hourly")) 
                    ? Enumerable.Range(1,24).Select(hour => rng.Next(-20, 55))
                    : null
            })
            .ToArray();
            return range;
        }
    }
}
