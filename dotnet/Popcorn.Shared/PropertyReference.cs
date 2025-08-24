

namespace Popcorn.Shared
{
#nullable enable
    public record PropertyReference
    {
        public static IReadOnlyList<PropertyReference> Default = new List<PropertyReference> { new PropertyReference { Name = "!default".AsMemory() } };

        public ReadOnlyMemory<char> Name { get; init; }
        public bool Negated { get; set; }

        public IReadOnlyList<PropertyReference>? Children { get; set; }

        public static IReadOnlyList<PropertyReference> ParseIncludeStatement(string? input)
        {
            if(input == null || input.Length < 3)
            {
                return Default;
            }

            int position = 0;
            IReadOnlyList<PropertyReference> ParseList()
            {
                var builder = new List<PropertyReference>();

                while (position < input.Length)
                {
                    char c = input[position];
                    if (c == '[')
                    {
                        position++;
                        // The last property added to builder should get the children
                        if (builder.Count > 0)
                        {
                            var parentProperty = builder[builder.Count - 1];
                            var children = ParseList();
                            // Create new PropertyReference with children to maintain immutability
                            var updatedParent = parentProperty with { Children = children };
                            builder[builder.Count - 1] = updatedParent;
                        }
                        // If builder.Count == 0, this is just the opening bracket of the top-level list
                        // Continue parsing the contents (don't call ParseList recursively)
                    }
                    else if (c == ']')
                    {
                        position++;
                        return builder;
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

                        var isNegated = false;

                        if (input[start] == '-')
                        {
                            isNegated = true;
                            start++;
                        }

                        var newProperty = new PropertyReference
                        {
                            Name = input.AsMemory().Slice(start, position - start),
                            Negated = isNegated,
                            Children = null, // Will be set later if brackets follow
                        };
                        builder.Add(newProperty);
                    }
                }
                return builder;
            }

            return ParseList();
        }
    }
}
