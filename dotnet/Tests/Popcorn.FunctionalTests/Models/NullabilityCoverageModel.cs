using System;
using System.Collections.Generic;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    // Exhaustive cross-product of:
    //   - type kind: primitive value type (int), reference type (string), custom struct, custom class
    //   - position: scalar property, list element, dictionary value
    //   - container nullability: plain vs. `?`
    //   - element nullability: plain vs. `?` (value types use `Nullable<T>`; reference types use C# NRT `?`)
    //
    // Naming convention: prefix Q = "question-mark applied at this level"
    //   Qint         = int?
    //   Qstring      = string?
    //   Qstruct      = CustomStruct?
    //   Qclass       = CustomClass?
    //   Container suffix Q = the container itself is nullable
    //   Element-slot Q = the element/value is nullable
    //
    // These property names are deliberately verbose so generator output can be grepped per permutation.
    public class NullabilityCoverageModel
    {
        // ---------- Scalar properties ----------
        public int Int { get; set; }
        public int? QInt { get; set; }

        public string String { get; set; } = string.Empty;
        public string? QString { get; set; }

        public NullStruct Struct { get; set; }
        public NullStruct? QStruct { get; set; }

        public NullClass Class { get; set; } = new NullClass();
        public NullClass? QClass { get; set; }

        // ---------- List<int> permutations ----------
        public List<int> ListInt { get; set; } = new();
        public List<int>? ListInt_QContainer { get; set; }
        public List<int?> ListQInt { get; set; } = new();
        public List<int?>? ListQInt_QContainer { get; set; }

        // ---------- List<string> permutations ----------
        public List<string> ListString { get; set; } = new();
        public List<string>? ListString_QContainer { get; set; }
        public List<string?> ListQString { get; set; } = new();
        public List<string?>? ListQString_QContainer { get; set; }

        // ---------- List<CustomStruct> permutations ----------
        public List<NullStruct> ListStruct { get; set; } = new();
        public List<NullStruct>? ListStruct_QContainer { get; set; }
        public List<NullStruct?> ListQStruct { get; set; } = new();
        public List<NullStruct?>? ListQStruct_QContainer { get; set; }

        // ---------- List<CustomClass> permutations ----------
        public List<NullClass> ListClass { get; set; } = new();
        public List<NullClass>? ListClass_QContainer { get; set; }
        public List<NullClass?> ListQClass { get; set; } = new();
        public List<NullClass?>? ListQClass_QContainer { get; set; }

        // ---------- Array permutations ----------
        public int[] ArrInt { get; set; } = Array.Empty<int>();
        public int[]? ArrInt_QContainer { get; set; }
        public string[] ArrString { get; set; } = Array.Empty<string>();
        public string[]? ArrString_QContainer { get; set; }
        public string?[] ArrQString { get; set; } = Array.Empty<string?>();
        public NullClass[] ArrClass { get; set; } = Array.Empty<NullClass>();
        public NullClass?[] ArrQClass { get; set; } = Array.Empty<NullClass?>();
        public NullClass[]? ArrClass_QContainer { get; set; }

        // ---------- Dictionary<string,int> permutations ----------
        public Dictionary<string, int> DictInt { get; set; } = new();
        public Dictionary<string, int>? DictInt_QContainer { get; set; }
        public Dictionary<string, int?> DictQInt { get; set; } = new();
        public Dictionary<string, int?>? DictQInt_QContainer { get; set; }

        // ---------- Dictionary<string,string> permutations ----------
        public Dictionary<string, string> DictString { get; set; } = new();
        public Dictionary<string, string>? DictString_QContainer { get; set; }
        public Dictionary<string, string?> DictQString { get; set; } = new();
        public Dictionary<string, string?>? DictQString_QContainer { get; set; }

        // ---------- Dictionary<string,CustomStruct> permutations ----------
        public Dictionary<string, NullStruct> DictStruct { get; set; } = new();
        public Dictionary<string, NullStruct>? DictStruct_QContainer { get; set; }
        public Dictionary<string, NullStruct?> DictQStruct { get; set; } = new();
        public Dictionary<string, NullStruct?>? DictQStruct_QContainer { get; set; }

        // ---------- Dictionary<string,CustomClass> permutations ----------
        public Dictionary<string, NullClass> DictClass { get; set; } = new();
        public Dictionary<string, NullClass>? DictClass_QContainer { get; set; }
        public Dictionary<string, NullClass?> DictQClass { get; set; } = new();
        public Dictionary<string, NullClass?>? DictQClass_QContainer { get; set; }

        // ---------- Nested combos (container-of-container) ----------
        public List<Dictionary<string, NullClass>> ListOfDictOfClass { get; set; } = new();
        public List<Dictionary<string, NullClass>?> ListOfQDictOfClass { get; set; } = new();
        public Dictionary<string, List<NullClass>> DictOfListOfClass { get; set; } = new();
        public Dictionary<string, List<NullClass>?> DictOfQListOfClass { get; set; } = new();
    }

    // Custom struct — used as a value type throughout the matrix.
    public struct NullStruct
    {
        public int X { get; set; }
        public string Label { get; set; }
    }

    // Custom class — used as a reference type throughout the matrix. Has both a default-included
    // property and a non-default property so we can verify [Default] attribute semantics flow
    // through every nullability combination.
    public class NullClass
    {
        [Default]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
