using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PopcornCoreExample.Models;

namespace PopcornCoreExample.Controllers
{
    [Route("api/example/")]
    public class ExampleController : Controller
    {
        ExampleContext _context;
        public ExampleController(ExampleContext context)
        {
            _context = context;
        }

        [HttpGet, Route("status")]
        public string Status()
        {
            return "OK";
        }

        [HttpGet, Route("employees")]
        public List<Employee> Employees()
        {
            return _context.Employees;
        }

        [HttpGet, Route("null")]
        public List<Employee> Null()
        {
            return null;
        }

        [HttpGet, Route("cars")]
        public List<Car> Cars()
        {
            return _context.Cars;
        }
    }
}
