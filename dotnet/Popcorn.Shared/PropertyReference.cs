using System.Collections.Immutable;

namespace Popcorn.Shared
{
#nullable enable
    public record PropertyReference
    {
        public ReadOnlyMemory<char> Name { get; init; }
        public bool Negated { get; set; }

        public ImmutableArray<PropertyReference> Children { get; set; }

        public static ImmutableArray<PropertyReference> ParseIncludeStatement(string input)
        {
            var builder = ImmutableArray.CreateBuilder<PropertyReference>();
            PropertyReference root = new PropertyReference { };
            PropertyReference cursor = root;

            int position = 0;
            ImmutableArray<PropertyReference> ParseList()
            {
                var builder = ImmutableArray.CreateBuilder<PropertyReference>();

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

                        var isNegated = false;

                        if (input[start] == '-')
                        {
                            isNegated = true;
                            start++;
                        }

                        cursor = new PropertyReference
                        {
                            Name = input.AsMemory().Slice(start, position - start),
                            Negated = isNegated,
                            Children = ImmutableArray<PropertyReference>.Empty,
                        };
                        builder.Add(cursor);
                    }
                }
                return builder.ToImmutable();
            }

            ParseList();
            return root.Children;
        }
    }
}
