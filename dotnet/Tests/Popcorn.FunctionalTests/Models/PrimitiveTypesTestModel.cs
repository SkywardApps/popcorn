using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class PrimitiveTypesTestModel
    {
        // Integer types
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public short ShortValue { get; set; }
        public byte ByteValue { get; set; }
        public sbyte SByteValue { get; set; }
        public uint UIntValue { get; set; }
        public ulong ULongValue { get; set; }
        public ushort UShortValue { get; set; }
        
        // Floating point types
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        
        // Boolean type
        public bool BoolValue { get; set; }
        
        // Character type
        public char CharValue { get; set; }
        
        // String type
        public string StringValue { get; set; } = string.Empty;
        
        // Date and time types
        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        
        // Identifier type
        public Guid GuidValue { get; set; }
        
        // Nullable primitive types
        public int? NullableIntValue { get; set; }
        public double? NullableDoubleValue { get; set; }
        public bool? NullableBoolValue { get; set; }
        public DateTime? NullableDateTimeValue { get; set; }
        public Guid? NullableGuidValue { get; set; }
    }
}
