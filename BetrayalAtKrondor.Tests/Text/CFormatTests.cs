namespace BetrayalAtKrondor.Tests.Text;

using GameData.Resources.Text;
using Xunit;

public class CFormatTests {
    // Successive conversions consume successive arguments, in order.
    [Fact]
    public void SubstitutesInOrder() =>
        Assert.Equal("12 gold 5 silver", CFormat.Apply("%ld gold %ld silver", 12, 5));

    [Fact]
    public void HandlesTheShortConversion() =>
        Assert.Equal("3 sovereigns", CFormat.Apply("%d sovereigns", 3));

    [Fact]
    public void HandlesStringConversion() =>
        Assert.Equal("a Goblin", CFormat.Apply("a %s", "Goblin"));

    // A doubled %% is a literal percent, not a conversion.
    [Fact]
    public void DoubledPercentIsALiteral() =>
        Assert.Equal("50%", CFormat.Apply("%d%%", 50));

    // Missing arguments must not throw — a malformed override should degrade, not crash the UI.
    [Fact]
    public void MissingArgumentsBecomeEmpty() =>
        Assert.Equal(" gold  silver", CFormat.Apply("%ld gold %ld silver"));

    // Width is NOT supported, and the catalog does contain it (base:uistring:item.percent_value).
    // Pinned rather than left implicit: the specifier survives verbatim AND the argument is left
    // unconsumed, so in a multi-argument format every later value would shift by one. If someone
    // adds width support, this test is the one that should change — and it will say why.
    [Fact]
    public void WidthIsUnsupportedAndDropsTheArgument() {
        Assert.Equal("%3d%", CFormat.Apply("%3d%%", 50));
        // The shift, made visible: the unconsumed 50 is picked up by the NEXT conversion.
        Assert.Equal("%3d 50", CFormat.Apply("%3d %d", 50, 7));
    }
}
