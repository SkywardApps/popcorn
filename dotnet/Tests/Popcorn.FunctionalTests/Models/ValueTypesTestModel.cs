using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class ValueTypesTestModel
    {
        // Simple struct
        public SimpleStruct SimpleStructValue { get; set; }
        
        // Struct with properties
        public PointStruct PointStructValue { get; set; }
        
        // Struct with nested struct
        public ComplexStruct ComplexStructValue { get; set; }
        
        // Record struct (C# 10+)
        public PositionRecordStruct PositionRecordStructValue { get; set; }
        
        // Record (reference type)
        public PersonRecord PersonRecordValue { get; set; }
        
        // ValueTuple
        public (int X, string Y, bool Z) ValueTupleValue { get; set; }
        
        // Named ValueTuple
        public (int Id, string Name) NamedValueTupleValue { get; set; }
        
        // Tuple (reference type)
        public Tuple<int, string, bool> TupleValue { get; set; }
        
        // Nested ValueTuple
        public (int Id, (string First, string Last) Name) NestedValueTupleValue { get; set; }
        
        // Nullable struct
        public SimpleStruct? NullableStructValue { get; set; }
        
        // Nullable record struct
        public PositionRecordStruct? NullableRecordStructValue { get; set; }
        
        // Array of structs
        public SimpleStruct[] StructArrayValue { get; set; }
        
        // List of record structs
        public List<PositionRecordStruct> RecordStructListValue { get; set; }
    }

    // Simple struct with fields
    public struct SimpleStruct
    {
        public int Value;
        
        public SimpleStruct(int value)
        {
            Value = value;
        }
    }

    // Struct with properties
    public struct PointStruct
    {
        public int X { get; set; }
        public int Y { get; set; }
        
        public PointStruct(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    // Struct with nested struct
    public struct ComplexStruct
    {
        public string Name { get; set; }
        public PointStruct Point { get; set; }
        
        public ComplexStruct(string name, PointStruct point)
        {
            Name = name;
            Point = point;
        }
    }

    // Record struct (C# 10+)
    public record struct PositionRecordStruct(double Latitude, double Longitude);

    // Record (reference type)
    public record PersonRecord(string FirstName, string LastName, int Age);
}
