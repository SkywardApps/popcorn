using System.Collections.Generic;
using ExampleModel.Models;
using System;
using System.Web.Http;
using System.Web;

namespace PopcornNetFrameworkExample.Controllers
{
    [RoutePrefix("api/example")]
    public class ExampleController : ApiController
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
            HttpContext.Current.Response.StatusCode = 301;
            throw new ArgumentException("This is a test exception");
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
