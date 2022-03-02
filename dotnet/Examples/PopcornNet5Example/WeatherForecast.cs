using Skyward.Popcorn;
using System;
using System.Collections.Generic;

namespace PopcornNet5Example
{
#nullable enable
    public class WeatherForecastBase
    {
        [IncludeByDefault]
        public int TemperatureC { get; set; }
    }

    public class WeatherForecast : WeatherForecastBase
    {
        [IncludeByDefault]
        public DateTime Date { get; set; }

        [IncludeByDefault]
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }

        public IEnumerable<int>? Hourly { get; set; }

        public IEnumerable<SubElement?>? Elements { get; set; }
    }

    public class SubElement
    {
        public string? Name { get; set; }
    }
}
