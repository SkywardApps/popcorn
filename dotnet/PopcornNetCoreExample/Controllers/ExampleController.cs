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

        [HttpGet, Route("internalc")]
        public InternalClass InternalClass()
        {
            return _context.InternalClass;
        }

        [HttpGet, Route("internalf")]
        public InternalFieldsClass InternalFields()
        {
            return _context.InternalFieldsClass;
        }
        [HttpGet, Route("internalferror")]
        public InternalFieldClassException InternalFieldsException()
        {
            return _context.InternalFieldClassException;
        }
    }
}
