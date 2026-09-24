namespace BetrayalAtKrondor.Tests.Text;

using GameData.Resources.Book;

using Xunit;

/// <summary>
/// <c>booktext_int_to_roman</c> (BOOKTEXT.C:364), quirks included.
/// </summary>
public class BookPageNumeralTests {
    [Theory]
    [InlineData(1, "I")]
    [InlineData(4, "IV")]
    [InlineData(9, "IX")]
    [InlineData(14, "XIV")]
    [InlineData(40, "XL")]
    [InlineData(400, "CD")]
    public void TheUsualNumeralsComeOutUsual(int value, string expected) =>
        Assert.Equal(expected, BookPageNumeral.For(value));

    [Theory]
    // C92's first page (display number 456) reads LDVI on the original's own screen, and C94's first
    // (469) LDXIX: the subtractive arm takes the largest digit that fits, not the textbook pair.
    [InlineData(456, "LDVI")]
    [InlineData(469, "LDXIX")]
    [InlineData(49, "IL")]
    [InlineData(99, "IC")]
    public void TheOriginalsSubtractionIsKept(int value, string expected) =>
        Assert.Equal(expected, BookPageNumeral.For(value));
}
