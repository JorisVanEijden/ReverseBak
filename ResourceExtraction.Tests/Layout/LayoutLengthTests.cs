namespace ResourceExtraction.Tests.Layout;

using System;
using System.Text.Json;
using GameData.Resources.Layout;
using Xunit;

public class LayoutLengthTests {
    [Theory]
    [InlineData("200px", 200f, LayoutLengthUnit.Px)]
    [InlineData("12.5%", 12.5f, LayoutLengthUnit.Percent)]
    [InlineData("auto", 0f, LayoutLengthUnit.Auto)]
    [InlineData("AUTO", 0f, LayoutLengthUnit.Auto)]
    [InlineData("0px", 0f, LayoutLengthUnit.Px)]
    [InlineData("-5px", -5f, LayoutLengthUnit.Px)]
    public void Parse_ReadsValueAndUnit(string text, float value, LayoutLengthUnit unit) {
        var length = LayoutLength.Parse(text);
        Assert.Equal(value, length.Value);
        Assert.Equal(unit, length.Unit);
    }

    [Theory]
    [InlineData("200px")]
    [InlineData("12.5%")]
    [InlineData("auto")]
    [InlineData("2.5px")]
    public void Parse_RoundTripsThroughToString(string text) {
        Assert.Equal(text, LayoutLength.Parse(text).ToString());
    }

    [Theory]
    [InlineData("200")]      // no unit
    [InlineData("px")]       // no value
    [InlineData("")]
    [InlineData("200em")]    // unsupported unit
    [InlineData("abcpx")]
    [InlineData("NaNpx")]
    [InlineData("Infinitypx")]
    [InlineData("-Infinity%")]
    public void Parse_RejectsMalformedInput(string text) {
        Assert.Throws<FormatException>(() => LayoutLength.Parse(text));
    }

    [Fact]
    public void Parse_UsesInvariantCulture_NotTheAmbientOne() {
        // A comma-decimal culture must not make "12.5%" unparseable or "12,5%" valid.
        Assert.Equal(12.5f, LayoutLength.Parse("12.5%").Value);
        Assert.Throws<FormatException>(() => LayoutLength.Parse("12,5%"));
    }

    [Fact]
    public void TryParse_ReturnsFalseInsteadOfThrowing() {
        Assert.False(LayoutLength.TryParse("nonsense", out _));
        Assert.True(LayoutLength.TryParse("48px", out LayoutLength ok));
        Assert.Equal(LayoutLength.Px(48f), ok);
    }

    [Fact]
    public void SerializesAsBareString() {
        Assert.Equal("\"200px\"", JsonSerializer.Serialize(LayoutLength.Px(200f)));
        Assert.Equal(LayoutLength.Percent(50f), JsonSerializer.Deserialize<LayoutLength>("\"50%\""));
    }
}
