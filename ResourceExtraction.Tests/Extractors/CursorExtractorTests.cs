namespace ResourceExtraction.Tests.Extractors;

using System.IO;
using GameData.Resources.Cursor;
using ResourceExtraction.Extractors;
using Xunit;

public class CursorExtractorTests {
    /// <summary>Walk up from the test output dir to find OriginalGame/&lt;name&gt; (present on dev
    /// machines, absent on CI). Returns null when the shipped art isn't available.</summary>
    private static string? FindGameFile(string name) {
        string? dir = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    [Fact]
    public void Hotspot_Index0And1_AreTopLeft_Index2Plus_AreCentred() {
        Assert.Equal((0, 0), CursorExtractor.ComputeHotspot(0, 50, 60));
        Assert.Equal((0, 0), CursorExtractor.ComputeHotspot(1, 50, 60));
        Assert.Equal((25, 30), CursorExtractor.ComputeHotspot(2, 50, 60));
        Assert.Equal((37, 45), CursorExtractor.ComputeHotspot(7, 75, 90));
    }

    [SkippableFact]
    public void Extract_RealPointerBmx_Has3Images_WithIndex0TopLeft() {
        string? path = FindGameFile("POINTER.BMX");
        Skip.If(path == null, "OriginalGame/POINTER.BMX not found");
        using FileStream s = File.OpenRead(path!);
        CursorSet set = new CursorExtractor().Extract("POINTER.BMX", s);
        Assert.Equal(3, set.Images.Count);
        Assert.Equal(0, set.Images[0].HotspotX);
        Assert.Equal(0, set.Images[0].HotspotY);
    }

    [SkippableFact]
    public void Extract_RealPointergBmx_Has27Images() {
        string? path = FindGameFile("POINTERG.BMX");
        Skip.If(path == null, "OriginalGame/POINTERG.BMX not found");
        using FileStream s = File.OpenRead(path!);
        CursorSet set = new CursorExtractor().Extract("POINTERG.BMX", s);
        Assert.Equal(27, set.Images.Count);
        Assert.All(set.Images, img => Assert.True(img.Width > 0 && img.Height > 0));
    }
}
