namespace GameData.Resources.Image;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;
#endif

public abstract class ImageResource(string id) : IResource {
    /// <summary>Width in CANONICAL pixels — the mode-13h width already scaled by 5.</summary>
    public int Width { get; set; }

    /// <summary>Height in CANONICAL pixels — the mode-13h height already scaled by 6.</summary>
    public int Height { get; set; }

    /// <summary>
    /// The image's width as a FRACTION OF THE SCREEN, not a scale factor.
    /// </summary>
    /// <remarks>
    /// <b>The name reads as a multiplier and it is the opposite.</b> A full-width image is 1.0 and a
    /// quarter-width one is 0.25, so passing 1 to a consumer that expects this makes the image fill
    /// the screen rather than leaving it alone. That is not hypothetical: it cost a Unity test a
    /// failing run on 2026-09-02, where a 16x16 synthetic image given <c>ScaleX = 1</c> painted the
    /// entire cutscene buffer.
    ///
    /// <para><b>Computed by the extractor, not read from the file.</b>
    /// <c>BitmapExtractor</c> sets it to <c>rawWidth / 320.0</c> the moment the width is read, i.e.
    /// against the mode-13h screen and BEFORE <see cref="Width"/> is scaled into canonical space.
    /// The fraction is scale-invariant, so in the extracted data it equals <see cref="Width"/> / 1600
    /// as well — checked across 4000 shipped BMX images with zero mismatches.</para>
    ///
    /// <para>So it is derivable and kept for convenience. A consumer that wants a size in canonical
    /// pixels should use <see cref="Width"/>; one that wants a proportion of the screen should use
    /// this.</para>
    /// </remarks>
    public virtual double ScaleX { get; set; }

    /// <inheritdoc cref="ScaleX"/>
    /// <remarks>
    /// The vertical twin — <c>rawHeight / 200.0</c>, equal to <see cref="Height"/> / 1200 in the
    /// extracted data. See <see cref="ScaleX"/> for why the name misleads.
    /// </remarks>
    public virtual double ScaleY { get; set; }
    public abstract ResourceType Type { get; }

    public string Id { get; } = id;

    public string? Filename { get; set; }

#if JSON_SERIALIZE
[JsonIgnore]
#endif
    public byte[]? BitMapData { get; set; }
}