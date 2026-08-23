namespace GameData.Resources.Animation.FrameCommands;

using GameData.Resources.Animation;

/// <summary>
/// Draws an image rotated to a free angle and scaled (TTM opcode <c>0xA5A7</c>).
/// </summary>
/// <remarks>
/// <b>The angle is degrees here, not the original's encoding.</b> The file stores a 16-bit value
/// whose top 12 bits are the angle in 1/4096 of a turn, measured the opposite way round from ours:
/// <c>degrees = (4096 - (raw &gt;&gt; 4)) * 360 / 4096</c>. Decoding it at extraction is the same
/// treatment palette entries and VGA coordinates get — a consumer should not have to know the
/// original packed its angles backwards in order to draw a picture.
///
/// <para>The bottom four bits are unused: every rotation in the shipped tree is a multiple of 16,
/// so the conversion is lossless in both directions and <c>TtmAssembler</c> can rebuild the exact
/// original bytes from the degrees.</para>
/// </remarks>
public class DrawImageRotated : DrawImageBase, IArea {
    /// <summary>Angle units in a full turn, before the shift — the original's resolution.</summary>
    public const int UnitsPerTurn = 4096;

    /// <summary>Bits the raw value is shifted by; the low nibble carries nothing.</summary>
    public const int RawShift = 4;

    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Clockwise rotation in degrees, 0–360.</summary>
    public float AngleDegrees { get; set; }

    /// <summary>Decodes the file's packed value into <see cref="AngleDegrees"/>.</summary>
    public static float DegreesFromRaw(int raw) =>
        (short)(UnitsPerTurn - (raw >> RawShift)) * (360f / UnitsPerTurn);

    /// <summary>The inverse, for writing a TTM back out.</summary>
    public static ushort RawFromDegrees(float degrees) {
        var units = (int)System.Math.Round(degrees * (UnitsPerTurn / 360f));
        return (ushort)(((UnitsPerTurn - units) << RawShift) & 0xFFFF);
    }

    public override string ToString() {
        return $"{nameof(DrawImageRotated)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height}, {AngleDegrees});";
    }
}
