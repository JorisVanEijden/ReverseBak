namespace ResourceExtraction.Extractors;

using System.IO;
using GameData.Resources.Cursor;
using GameData.Resources.Image;

/// <summary>Translates POINTER.BMX / POINTERG.BMX into an engine-independent <see cref="CursorSet"/>.
/// Reuses <see cref="BitmapExtractor"/> for the (already 5x:6x upscaled) image dimensions and attaches the
/// RE-derived hotspot per image. Pixel data is NOT re-emitted; Unity loads cursor sprites through the
/// normal BMX archive path ("POINTER.BMX#N").</summary>
public class CursorExtractor : ExtractorBase<CursorSet> {
    private readonly BitmapExtractor _bitmap = new();

    public override CursorSet Extract(string id, Stream resourceStream) {
        ImageSet images = _bitmap.Extract(id, resourceStream);
        var set = new CursorSet(Path.GetFileNameWithoutExtension(id)) { SourceFile = id };
        for (int i = 0; i < images.Images.Count; i++) {
            BmImage img = images.Images[i];
            (int hx, int hy) = ComputeHotspot(i, img.Width, img.Height);
            set.Images.Add(new CursorImage {
                Index = i, Width = img.Width, Height = img.Height, HotspotX = hx, HotspotY = hy
            });
        }
        return set;
    }

    /// <summary>SetPointerImage (0x2abb7) rule: index 0/1 -> top-left (0,0); index &gt;= 2 -> centred.
    /// Computed on the upscaled dimensions, which yields the same canonical hotspot as the DOS native w/2.</summary>
    public static (int, int) ComputeHotspot(int index, int width, int height) =>
        index >= 2 ? (width / 2, height / 2) : (0, 0);
}
