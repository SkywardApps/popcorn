using PopcornSpecTests.Models;
using Skyward.Popcorn.Abstractions;
using System;
using System.Collections.Generic;
using Xunit;

namespace PopcornSpecTests
{
    /// <summary>
    /// Our basic specification tests for non-customized implementations
    /// </summary>
    public class TestPopcornDefaultBehavior
    {
        PopcornFactory factory = new PopcornFactory();
        public TestPopcornDefaultBehavior()
        {
            factory.UseDefaultConfiguration();
        }

        [Fact]
        public void CanExpandClasses()
        {
            var popcorn = factory.CreatePopcorn();
            var referenceData = new SampleBasicClass();
            var result = popcorn.Expand(typeof(SampleBasicClass), new SampleBasicClass(), null);

            // validate the results
            Assert.IsType<Dictionary<string, object>>(result);
            var dict = (Dictionary<string, object>)result!;
            Assert.StrictEqual(referenceData.Int, dict[nameof(SampleBasicClass.Int)]);
            Assert.StrictEqual(referenceData.Byte, dict[nameof(SampleBasicClass.Byte)]);
            Assert.StrictEqual(referenceData.String, dict[nameof(SampleBasicClass.String)]);
            Assert.StrictEqual(referenceData.DateTime, dict[nameof(SampleBasicClass.DateTime)]);
            Assert.StrictEqual(referenceData.Guid, dict[nameof(SampleBasicClass.Guid)]);
        }

        [Fact]
        public void CanExpandStructs()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void CanExpandNull()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void CanExpandString()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void CanExpandList()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void CanExpandDictionary()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void CanExpandHashSet()
        {
            throw new NotImplementedException();
        }


        [Fact]
        public void CanExpandArray()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void CanExpandNonGenericList()
        {
            throw new NotImplementedException();
        }

    }
}