namespace ResourceExtraction.Tests.Imaging;

using ResourceExtraction.Imaging;
using Xunit;

public class AspectCorrectionTests {
    [Theory]
    [InlineData(0, 0)]
    [InlineData(13, 65)]     // viewport origin X
    [InlineData(320, 1600)]  // full width
    public void ScaleVgaX_MultipliesByFive(int input, int expected) {
        Assert.Equal(expected, AspectCorrection.ScaleVgaX(input));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(11, 66)]     // viewport origin Y
    [InlineData(200, 1200)]  // full height
    public void ScaleVgaY_MultipliesBySix(int input, int expected) {
        Assert.Equal(expected, AspectCorrection.ScaleVgaY(input));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(40, 80)]
    [InlineData(640, 1280)]  // full width
    public void ScaleEgaX_MultipliesByTwo(int input, int expected) {
        Assert.Equal(expected, AspectCorrection.ScaleEgaX(input));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(80, 219)]    // round(80 * 96 / 35) = round(219.43)
    [InlineData(350, 960)]   // full height -> matches BOOK.SCX 1280x960
    public void ScaleEgaY_ResamplesBy96Over35(int input, int expected) {
        Assert.Equal(expected, AspectCorrection.ScaleEgaY(input));
    }
}
