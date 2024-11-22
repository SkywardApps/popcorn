using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

internal class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<HashSetVsArray>();

        Console.WriteLine("Press To Continue");
        Console.ReadKey();
    }
}

public class PropertyReferenceClass {
    public ReadOnlyMemory<char> Name { get; set; }
    public bool Optional { get; set; }
    public bool Negated {get; set; }
    public ImmutableArray<PropertyReferenceClass> Children { get; set; }
}

public record PropertyReferenceRecord
{
    public ReadOnlyMemory<char> Name { get; set; }
    public bool Optional { get; set; }
    public bool Negated { get; set; }
    public ImmutableArray<PropertyReferenceRecord> Children { get; set; }
}

public record PropertyReferenceRecordDictionary
{
    public ReadOnlyMemory<char> Name { get; set; }
    public bool Optional { get; set; }
    public bool Negated { get; set; }
    public ImmutableDictionary<ReadOnlyMemory<char>, PropertyReferenceRecordDictionary> Children { get; set; }
}

[MemoryDiagnoser]
public class Parsers
{
    Regex SplitterRegex = new Regex("[a-zA-Z_?$!-][a-zA-Z_0-9]+(\\[.+\\])?");

    private const string InputString = "[!default,Id,Title[Id,Name],Decide[Task,Result],-Second,First[!default,Id,Title[Id,Name[!default,Id,Title[Id,Name],Decide[Task,Result],-Second]],Decide[Task,Result],-Second]]";

    // Recursive Descent Parser
    private List<object> RecursiveDescentParserObject(string input)
    {
        int position = 0;
        List<object> ParseList()
        {
            var tokens = new List<object>();
            while (position < input.Length)
            {
                char c = input[position];
                if (c == '[')
                {
                    position++;
                    tokens.Add(ParseList());
                }
                else if (c == ']')
                {
                    position++;
                    return tokens;
                }
                else if (c == ',')
                {
                    position++;
                }
                else
                {
                    int start = position;
                    while (position < input.Length && input[position] != ',' && input[position] != ']')
                    {
                        position++;
                    }
                    tokens.Add(input[start..position].Trim());
                }
            }
            return tokens;
        }

        position = 0;
        return ParseList();
    }

    private ImmutableArray<PropertyReferenceClass> RecursiveParserClass(string input)
    {
        var builder = ImmutableArray.CreateBuilder<PropertyReferenceClass>();
        PropertyReferenceClass root = new PropertyReferenceClass { };
        PropertyReferenceClass cursor = root;

        int position = 0;
        ImmutableArray<PropertyReferenceClass> ParseList()
        {
            var builder = ImmutableArray.CreateBuilder<PropertyReferenceClass>();

            while (position < input.Length)
            {
                char c = input[position];
                if (c == '[')
                {
                    position++;
                    cursor!.Children = ParseList();
                }
                else if (c == ']')
                {
                    position++;
                    return builder.ToImmutable();
                }
                else if (c == ',')
                {
                    position++;
                }
                else
                {
                    int start = position;
                    while (position < input.Length && input[position] != ',' && input[position] != ']' && input[position] != '[')
                    {
                        position++;
                    }

                    var isOptional = false;
                    var isNegated = false;
                    if (input[start] == '?')
                    {
                        isOptional = true;
                        start++;
                    }
                    if (input[start] == '-')
                    {
                        isNegated = true;
                        start++;
                    }

                    cursor = new PropertyReferenceClass { 
                        Name = input.AsMemory().Slice(start, position - start),
                        Optional= isOptional,
                        Negated = isNegated
                    };
                    builder.Add(cursor);
                }
            }
            return builder.ToImmutable();
        }

        ParseList();
        return root.Children;
    }

    private ImmutableArray<PropertyReferenceRecord> RecursiveParserRecord(string input)
    {
        var builder = ImmutableArray.CreateBuilder<PropertyReferenceRecord>();
        PropertyReferenceRecord root = new PropertyReferenceRecord { };
        PropertyReferenceRecord cursor = root;

        int position = 0;
        ImmutableArray<PropertyReferenceRecord> ParseList()
        {
            var builder = ImmutableArray.CreateBuilder<PropertyReferenceRecord>();

            while (position < input.Length)
            {
                char c = input[position];
                if (c == '[')
                {
                    position++;
                    cursor!.Children = ParseList();
                }
                else if (c == ']')
                {
                    position++;
                    return builder.ToImmutable();
                }
                else if (c == ',')
                {
                    position++;
                }
                else
                {
                    int start = position;
                    while (position < input.Length && input[position] != ',' && input[position] != ']' && input[position] != '[')
                    {
                        position++;
                    }

                    var isOptional = false;
                    var isNegated = false;
                    if (input[start] == '?')
                    {
                        isOptional = true;
                        start++;
                    }
                    if (input[start] == '-')
                    {
                        isNegated = true;
                        start++;
                    }

                    cursor = new PropertyReferenceRecord
                    {
                        Name = input.AsMemory().Slice(start, position - start),
                        Optional = isOptional,
                        Negated = isNegated
                    };
                    builder.Add(cursor);
                }
            }
            return builder.ToImmutable();
        }

        ParseList();
        return root.Children;
    }

    private ImmutableDictionary<ReadOnlyMemory<char>, PropertyReferenceRecordDictionary> RecursiveParserRecordDictionary(string input)
    {
        var builder = ImmutableDictionary.CreateBuilder<ReadOnlyMemory<char>, PropertyReferenceRecordDictionary>();
        PropertyReferenceRecordDictionary root = new PropertyReferenceRecordDictionary { };
        PropertyReferenceRecordDictionary cursor = root;

        int position = 0;
        ImmutableDictionary<ReadOnlyMemory<char>, PropertyReferenceRecordDictionary> ParseList()
        {
            var builder = ImmutableDictionary.CreateBuilder<ReadOnlyMemory<char>, PropertyReferenceRecordDictionary>();

            while (position < input.Length)
            {
                char c = input[position];
                if (c == '[')
                {
                    position++;
                    cursor!.Children = ParseList();
                }
                else if (c == ']')
                {
                    position++;
                    return builder.ToImmutable();
                }
                else if (c == ',')
                {
                    position++;
                }
                else
                {
                    int start = position;
                    while (position < input.Length && input[position] != ',' && input[position] != ']' && input[position] != '[')
                    {
                        position++;
                    }

                    var isOptional = false;
                    var isNegated = false;
                    if (input[start] == '?')
                    {
                        isOptional = true;
                        start++;
                    }
                    if (input[start] == '-')
                    {
                        isNegated = true;
                        start++;
                    }

                    cursor = new PropertyReferenceRecordDictionary
                    {
                        Name = input.AsMemory().Slice(start, position - start),
                        Optional = isOptional,
                        Negated = isNegated,
                        Children = ImmutableDictionary<ReadOnlyMemory<char>, PropertyReferenceRecordDictionary>.Empty
                    };
                    builder.Add(cursor.Name, cursor);
                }
            }
            return builder.ToImmutable();
        }

        ParseList();
        return root.Children;
    }


    [Benchmark]
    public void RecursiveDescent()
    {
        var result = RecursiveDescentParserObject(InputString);
    }

    [Benchmark]
    public void RecursivePropertyReferenceClass()
    {
        var result = RecursiveParserClass(InputString);
        if (result.Length != 6)
        {
            throw new Exception($"Misparsed output: {result.Length}");
        }
    }

    [Benchmark]
    public void RecursivePropertyReferenceRecord()
    {
        var result = RecursiveParserRecord(InputString);
        if (result.Length != 6)
        {
            throw new Exception($"Misparsed output: {result.Length}");
        }
    }
    [Benchmark]

    public void RecursivePropertyReferenceRecordDictionary()
    {
        var result = RecursiveParserRecordDictionary(InputString);
        if (result.Keys.Count() != 6)
        {
            throw new Exception($"Misparsed output: {result.Keys.Count()}");
        }
    }
}

[MemoryDiagnoser]
public class HashSetVsArray
{
    private ImmutableArray<string> array;
    private HashSet<string> hashSet;
    private string lookupItem;

    [Params(5, 10)] // Vary the number of elements
    public int ElementCount;

    [Params(3, 8, 13)] // Vary the number of lookups
    public int LookupCount;

    [GlobalSetup]
    public void Setup()
    {
        var r = new Random();
        array = Enumerable.Range(0, ElementCount).Select(i => $"Item{i}").ToImmutableArray();
        lookupItem = $"Item{r.Next(LookupCount*2)}"; // Middle item for consistent lookup
    }

    [Benchmark]
    public bool ArrayLookup()
    {
        bool found = false;
        for (int i = 0; i < LookupCount; i++)
        {
            found |= array.Contains(lookupItem);
        }
        return found;
    }

    [Benchmark]
    public bool HashSetLookup()
    {
        hashSet = new HashSet<string>(array);
        bool found = false;
        for (int i = 0; i < LookupCount; i++)
        {
            found |= hashSet.Contains(lookupItem);
        }
        return found;
    }

    bool Inner<T>(T set) where T : ICollection<string>
    {
        bool found = false;
        for (int i = 0; i < LookupCount; i++)
        {
            found |= set.Contains(lookupItem);
        }
        return found;
    }

    [Benchmark]
    public bool DynamicLookup()
    {
        if (LookupCount < 5)
        {
            return Inner(new HashSet<string>(array));
        }
        else
        {
            return Inner(array);
        }
    }       
}