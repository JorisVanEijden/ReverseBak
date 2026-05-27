namespace BetrayalAtKrondor.Tests.Imaging;

using ResourceExtraction.Imaging;
using Xunit;

/// <summary>
/// Verifies the DOS non-square-pixel → square-pixel correction applied at the extraction
/// boundary. VGA (320x200-space) pixels replicate x5 / x6 to exact 4:3; the lone 640x350
/// EGA screen resamples to 1280x960. See memory: project-aspect-correction.
/// </summary>
public class AspectCorrectionTests {

    [Fact]
    public void CorrectVga_ScalesDimensionsBy5And6() {
        var src = new byte[320 * 200];

        byte[] dst = AspectCorrection.CorrectVga(src, 320, 200, columnMajor: false, out int w, out int h);

        Assert.Equal(1600, w);
        Assert.Equal(1200, h);
        Assert.Equal(1600 * 1200, dst.Length);
    }

    [Fact]
    public void CorrectVga_RowMajor_ReplicatesEachPixelIntoA5x6Block() {
        // 2x2 row-major: index = y*width + x
        var src = new byte[] {
            1, 2, // (0,0)=1 (1,0)=2
            3, 4  // (0,1)=3 (1,1)=4
        };

        byte[] dst = AspectCorrection.CorrectVga(src, 2, 2, columnMajor: false, out int w, out int h);

        Assert.Equal(10, w);
        Assert.Equal(12, h);
        // Each source pixel owns a 5-wide x 6-tall block. Sample inside and on the boundaries.
        Assert.Equal(1, dst[0 * w + 0]);   // top-left block
        Assert.Equal(1, dst[5 * w + 4]);   // last cell of (0,0)'s block (x<5, y<6)
        Assert.Equal(2, dst[0 * w + 5]);   // first column of (1,0)'s block
        Assert.Equal(3, dst[6 * w + 0]);   // first row of (0,1)'s block
        Assert.Equal(4, dst[11 * w + 9]);  // bottom-right block
    }

    [Fact]
    public void CorrectVga_ColumnMajor_ReplicatesAlongStoredAxisOrder() {
        // 2x2 column-major: index = x*height + y, stored as P(0,0) P(0,1) P(1,0) P(1,1)
        var src = new byte[] { 10, 11, 20, 21 };

        byte[] dst = AspectCorrection.CorrectVga(src, 2, 2, columnMajor: true, out int w, out int h);

        Assert.Equal(10, w);
        Assert.Equal(12, h);
        // Column-major read-back: index = dx*height + dy maps to logical P(dx/5, dy/6).
        Assert.Equal(10, dst[0 * h + 0]);   // P(0,0)
        Assert.Equal(11, dst[0 * h + 6]);   // P(0,1)
        Assert.Equal(20, dst[5 * h + 0]);   // P(1,0)
        Assert.Equal(21, dst[5 * h + 6]);   // P(1,1)
    }

    [Fact]
    public void ResampleNearest_HiResBook_Produces1280x960() {
        var src = new byte[640 * 350];
        System.Array.Fill(src, (byte)7);

        byte[] dst = AspectCorrection.ResampleNearest(src, 640, 350, 1280, 960);

        Assert.Equal(1280 * 960, dst.Length);
        Assert.All(dst, b => Assert.Equal(7, b)); // a uniform source stays uniform through resampling
    }
}
