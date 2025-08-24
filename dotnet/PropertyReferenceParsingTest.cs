using System;
using System.Linq;
using Popcorn.Shared;

// Simple test to confirm the PropertyReference parsing bug
class Program
{
    static void Main()
    {
        Console.WriteLine("Testing PropertyReference parsing...");
        
        var input = "[StringComplexItemDictionary[Id,Name,Description,CreatedDate]]";
        var result = PropertyReference.ParseIncludeStatement(input);
        
        Console.WriteLine($"Input: {input}");
        Console.WriteLine($"Parsed {result.Count} top-level properties");
        
        foreach (var prop in result)
        {
            Console.WriteLine($"Property: {prop.Name} (Negated: {prop.Negated})");
            if (prop.Children != null && prop.Children.Count > 0)
            {
                Console.WriteLine($"  Children: {prop.Children.Count}");
                foreach (var child in prop.Children)
                {
                    Console.WriteLine($"    - {child.Name} (Negated: {child.Negated})");
                }
            }
        }
        
        // Expected: 
        // Property: StringComplexItemDictionary (Negated: False)
        //   Children: 4
        //     - Id (Negated: False)
        //     - Name (Negated: False) 
        //     - Description (Negated: False)
        //     - CreatedDate (Negated: False)
    }
}
