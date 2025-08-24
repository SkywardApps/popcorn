

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

            PropertyReference root = new PropertyReference { };
            PropertyReference cursor = root;

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
                        cursor!.Children = ParseList();
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

                        cursor = new PropertyReference
                        {
                            Name = input.AsMemory().Slice(start, position - start),
                            Negated = isNegated,
                            Children = PropertyReference.Default,
                        };
                        builder.Add(cursor);
                    }
                }
                return builder;
            }

            ParseList();
            return root.Children ?? PropertyReference.Default;
        }
    }
}
