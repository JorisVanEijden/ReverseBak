namespace BetrayalAtKrondor.Tests.Content;

using GameData.Resources.Content;
using Xunit;

public class ContentKeyTests {
    [Fact] public void ForBase_ComposesCanonicalKey() =>
        Assert.Equal("base:objinfo:17", ContentKey.ForBase("objinfo", 17));

    [Fact] public void ForMod_ComposesNamespacedKey() =>
        Assert.Equal("mymod:antidote", ContentKey.ForMod("mymod", "antidote"));

    [Theory]
    [InlineData("base:objinfo:17", "base")]
    [InlineData("mymod:antidote", "mymod")]
    [InlineData("nocolon", "")]
    [InlineData(":leadingcolon", "")]
    public void NamespaceOf_ReturnsPrefixBeforeFirstColon(string key, string expected) =>
        Assert.Equal(expected, ContentKey.NamespaceOf(key));

    [Theory]
    [InlineData("base:objinfo:17", true)]
    [InlineData("mymod:antidote", true)]
    [InlineData("nocolon", false)]
    [InlineData("", false)]
    [InlineData(":x", false)]
    [InlineData("x:", false)]
    public void IsValid_RequiresNonEmptyNamespaceAndRemainder(string? key, bool expected) =>
        Assert.Equal(expected, ContentKey.IsValid(key));

    [Fact] public void TryParseBase_ExtractsIndexForMatchingCatalog() {
        Assert.True(ContentKey.TryParseBase("base:objinfo:17", "objinfo", out int index));
        Assert.Equal(17, index);
    }

    [Theory]
    [InlineData("base:spells:3", "objinfo")]   // wrong catalog
    [InlineData("mymod:antidote", "objinfo")]  // not a base key
    [InlineData("base:objinfo:x", "objinfo")]  // non-numeric index
    public void TryParseBase_RejectsNonMatching(string key, string catalog) =>
        Assert.False(ContentKey.TryParseBase(key, catalog, out _));
}
