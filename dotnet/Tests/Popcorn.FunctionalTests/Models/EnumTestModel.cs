using System.Collections.Generic;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public enum Color
    {
        Red = 0,
        Green = 1,
        Blue = 2,
    }

    [System.Flags]
    public enum Permissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        ReadWrite = Read | Write,
    }

    public class EnumTestModel
    {
        [Default]
        public Color FavoriteColor { get; set; }

        public Color? NullableColor { get; set; }

        public Permissions UserPermissions { get; set; }

        public List<Color> ColorList { get; set; } = new();

        [Always]
        public Color StatusColor { get; set; }
    }
}
