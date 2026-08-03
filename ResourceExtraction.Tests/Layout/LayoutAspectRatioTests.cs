namespace ResourceExtraction.Tests.Layout;

using System;
using System.Text.Json;
using GameData.Resources.Layout;
using Xunit;

public class LayoutAspectRatioTests {
    [Fact]
    public void Parse_ReadsWidthAndHeight() {
        var ratio = LayoutAspectRatio.Parse("10:9");
        Assert.Equal(10f, ratio.Width);
        Assert.Equal(9f, ratio.Height);
    }

    [Fact]
    public void Ratio_IsWidthOverHeight() {
        // An inventory cell is 40x30 VGA = 200x180 canonical.
        Assert.Equal(200f / 180f, LayoutAspectRatio.Parse("200:180").Ratio, 5);
    }

    [Theory]
    [InlineData("10:9")]
    [InlineData("4:3")]
    [InlineData("2.5:1")]
    public void Parse_RoundTripsThroughToString(string text) {
        Assert.Equal(text, LayoutAspectRatio.Parse(text).ToString());
    }

    [Theory]
    [InlineData("10")]       // no separator
    [InlineData("10:")]      // no height
    [InlineData(":9")]       // no width
    [InlineData("10:0")]     // zero height would divide by zero
    [InlineData("0:9")]      // zero width is not a ratio
    [InlineData("-10:9")]    // negative is not a ratio
    [InlineData("a:b")]
    [InlineData("")]
    [InlineData("NaN:9")]    // NaN width is not valid
    [InlineData("10:Infinity")] // Infinity height is not valid
    public void Parse_RejectsMalformedOrDegenerateInput(string text) {
        Assert.Throws<FormatException>(() => LayoutAspectRatio.Parse(text));
    }

    [Fact]
    public void SerializesAsBareString() {
        Assert.Equal("\"10:9\"", JsonSerializer.Serialize(new LayoutAspectRatio(10f, 9f)));
        Assert.Equal(new LayoutAspectRatio(4f, 3f), JsonSerializer.Deserialize<LayoutAspectRatio>("\"4:3\""));
    }

    [Fact]
    public void Constructor_RejectsNonFiniteOrNonPositiveComponents() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutAspectRatio(0f, 9f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutAspectRatio(10f, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutAspectRatio(-10f, 9f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutAspectRatio(float.NaN, 9f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutAspectRatio(10f, float.PositiveInfinity));
    }

    [Fact]
    public void Ratio_OnDefaultValue_ThrowsRatherThanReturningNaN() {
        Assert.Throws<InvalidOperationException>(() => _ = default(LayoutAspectRatio).Ratio);
    }
}
