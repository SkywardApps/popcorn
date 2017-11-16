using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PopcornCoreExample.Models;
using System;

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

        [HttpGet, Route("null")]
        public List<Employee> Null()
        {
            return null;
        }

        [HttpGet, Route("status")]
        public string Status()
        {
            return "OK";
        }

        [HttpGet, Route("error")]
        public string Error()
        {
            HttpContext.Response.StatusCode = 301;
            throw new ArgumentException("This is a test exception");

            return "Error thrown";
        }

        [HttpGet, Route("employees")]
        public List<Employee> Employees()
        {
            return _context.Employees;
        }

        [HttpGet, Route("cars")]
        public List<Car> Cars()
        {
            return _context.Cars;
        }

        [HttpGet, Route("businesses")]
        public List<Business> Businesses()
        {
            return _context.Businesses;
        }
    }
}
