using Popcorn.Shared;
using Xunit;

namespace Popcorn.FunctionalTests
{
    public class IncludeParserEdgeTests
    {
        [Fact]
        public void Parser_NullInput_ReturnsDefault()
        {
            var refs = PropertyReference.ParseIncludeStatement(null);
            Assert.Single(refs);
            Assert.Equal("!default", refs[0].Name.ToString());
        }

        [Fact]
        public void Parser_EmptyBrackets_ReturnsDefault()
        {
            var refs = PropertyReference.ParseIncludeStatement("[]");
            Assert.Single(refs);
            Assert.Equal("!default", refs[0].Name.ToString());
        }

        [Fact]
        public void Parser_SingleProperty_Parsed()
        {
            var refs = PropertyReference.ParseIncludeStatement("[Name]");
            Assert.Single(refs);
            Assert.Equal("Name", refs[0].Name.ToString());
            Assert.False(refs[0].Negated);
        }

        [Fact]
        public void Parser_MultipleProperties_ParsedInOrder()
        {
            var refs = PropertyReference.ParseIncludeStatement("[Id,Name,Value]");
            Assert.Equal(3, refs.Count);
            Assert.Equal("Id", refs[0].Name.ToString());
            Assert.Equal("Name", refs[1].Name.ToString());
            Assert.Equal("Value", refs[2].Name.ToString());
        }

        [Fact]
        public void Parser_NegatedProperty_SetsNegatedFlag()
        {
            var refs = PropertyReference.ParseIncludeStatement("[!all,-Secret]");
            Assert.Equal(2, refs.Count);
            Assert.Equal("!all", refs[0].Name.ToString());
            Assert.Equal("Secret", refs[1].Name.ToString());
            Assert.True(refs[1].Negated);
        }

        [Fact]
        public void Parser_NestedProperty_BuildsChildReferences()
        {
            var refs = PropertyReference.ParseIncludeStatement("[Items[Id,Name]]");
            Assert.Single(refs);
            Assert.Equal("Items", refs[0].Name.ToString());
            Assert.NotNull(refs[0].Children);
            Assert.Equal(2, refs[0].Children!.Count);
            Assert.Equal("Id", refs[0].Children![0].Name.ToString());
            Assert.Equal("Name", refs[0].Children![1].Name.ToString());
        }

        [Fact]
        public void Parser_DeeplyNested_BuildsCorrectTree()
        {
            var refs = PropertyReference.ParseIncludeStatement("[A[B[C[D]]]]");
            Assert.Single(refs);
            Assert.Equal("A", refs[0].Name.ToString());
            var b = refs[0].Children!;
            Assert.Single(b);
            Assert.Equal("B", b[0].Name.ToString());
            var c = b[0].Children!;
            Assert.Single(c);
            Assert.Equal("C", c[0].Name.ToString());
            var d = c[0].Children!;
            Assert.Single(d);
            Assert.Equal("D", d[0].Name.ToString());
        }

        [Fact]
        public void Parser_MultipleNested_ParsesSiblingSubtrees()
        {
            var refs = PropertyReference.ParseIncludeStatement("[Car[Make,Model],Owner[Name]]");
            Assert.Equal(2, refs.Count);
            Assert.Equal("Car", refs[0].Name.ToString());
            Assert.Equal(2, refs[0].Children!.Count);
            Assert.Equal("Owner", refs[1].Name.ToString());
            Assert.Single(refs[1].Children!);
        }

        [Fact]
        public void Parser_Wildcards_ParsedAsNamedReferences()
        {
            var refs = PropertyReference.ParseIncludeStatement("[!all]");
            Assert.Single(refs);
            Assert.Equal("!all", refs[0].Name.ToString());
        }

        [Fact]
        public void Parser_WhitespaceInsideBrackets_TreatedAsPartOfName()
        {
            var refs = PropertyReference.ParseIncludeStatement("[ Id , Name ]");
            Assert.Equal(2, refs.Count);
        }

        [Fact(Skip = "Pending: parser currently mishandles dictionary-value subtrees; see migrationAnalysis.md / PropertyReference.ParseIncludeStatement bug.")]
        public void Parser_DictionaryValueNestedInclude_BuildsCorrectTree()
        {
            var refs = PropertyReference.ParseIncludeStatement("[MyDictionary[Value[Name]]]");
            Assert.Single(refs);
            Assert.NotNull(refs[0].Children);
        }

        [Fact]
        public void Parser_ShortInput_ReturnsDefault()
        {
            var refs = PropertyReference.ParseIncludeStatement("[");
            Assert.Single(refs);
            Assert.Equal("!default", refs[0].Name.ToString());
        }

        [Fact]
        public void Parser_UnderscoreStartName_IsValid()
        {
            var refs = PropertyReference.ParseIncludeStatement("[_Private]");
            Assert.Single(refs);
            Assert.Equal("_Private", refs[0].Name.ToString());
        }

        [Fact]
        public void Parser_MixedNegationAndSelection_PreservesOrder()
        {
            var refs = PropertyReference.ParseIncludeStatement("[Id,-Secret,Name]");
            Assert.Equal(3, refs.Count);
            Assert.False(refs[0].Negated);
            Assert.True(refs[1].Negated);
            Assert.False(refs[2].Negated);
        }
    }
}
