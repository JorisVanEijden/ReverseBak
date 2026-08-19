namespace GameData.Resources.World;

/// <summary>
/// The party's own marker on the overhead map — the tail of <c>drawMap</c> (IDA 0x21711).
/// </summary>
/// <remarks>
/// <b>The party is DRAWN, not implied by the camera.</b> After the world pass, <c>drawMap</c> puts an
/// icon from <c>mapicons.bmp</c> at the middle of the viewport. Which icon depends on the north-up
/// option, and so does what the render did with the camera's yaw a few instructions earlier — the
/// two halves are one decision:
/// <list type="bullet">
/// <item><b>Turning map</b> (the default): the world is rendered at the party's own yaw, so the
/// party always faces up the screen and the marker is a fixed arrow.</item>
/// <item><b>North-up</b> ('N', CD build only): the world is rendered at yaw 0, so north is up, and
/// the heading moves into the marker — the icon is picked from the party's direction.</item>
/// </list>
///
/// <para>The yaw is saved and put back around the render, so this changes what is drawn and never
/// where the party is standing. See <see cref="LocalMapScreen.MapRendersWithYaw"/>.</para>
/// </remarks>
public static class OverheadMapMarker {
    /// <summary>The icon sheet the marker comes from.</summary>
    public const string IconSheet = "MAPICONS.BMP";

    /// <summary>How many directions the north-up marker can point — a 16-point compass.</summary>
    public const int Directions = 16;

    /// <summary>
    /// The marker direction for a heading: the engine's angle rounded to one of
    /// <see cref="Directions"/>.
    /// </summary>
    /// <remarks>
    /// <c>(yaw + 0x800) &gt;&gt; 0xC</c> — a full turn is 0x10000, so the shift takes the top four
    /// bits and the 0x800 is half a step, which makes it round to the NEAREST direction rather than
    /// truncating towards the previous one. Truncating would leave the marker up to a whole step
    /// behind the party at every heading between two icons.
    /// </remarks>
    public static int DirectionFor(int yaw) =>
        ((yaw + (StepSize / 2)) >> 12) & (Directions - 1);

    /// <summary>The angle between two marker directions in engine units.</summary>
    public const int StepSize = 0x10000 / Directions;

    /// <summary>
    /// Whether the marker's icon carries the heading, or the map's rotation does.
    /// </summary>
    /// <remarks>
    /// Exactly the inverse of what the render does with the yaw: north-up puts the heading in the
    /// icon, a turning map puts it in the camera. A port that did both would turn the party twice.
    /// </remarks>
    public static bool IconCarriesTheHeading(bool northUp) => northUp;

    /// <summary>
    /// Where the marker sits: the middle of the world viewport, nudged by half the icon.
    /// </summary>
    /// <remarks>
    /// The original subtracts 4 from x and 3 from y of the viewport's centre for the north-up icon —
    /// half of an 8x6 sprite, i.e. it is centring the icon rather than placing its corner. Expressed
    /// as the icon's own half-size so a port centres whatever icon it actually has instead of
    /// carrying two magic numbers.
    /// </remarks>
    public static (int X, int Y) TopLeftFor(int viewportX, int viewportY, int viewportWidth,
        int viewportHeight, int iconWidth, int iconHeight) =>
        (viewportX + (viewportWidth / 2) - (iconWidth / 2),
            viewportY + (viewportHeight / 2) - (iconHeight / 2));

    /// <summary>The icon size the original's own offsets imply: 8 x 6.</summary>
    /// <remarks>
    /// Derived from the -4 / -3 nudge rather than read from the sheet, and recorded as a check on
    /// whatever <c>mapicons.bmp</c> turns out to hold: a port whose icon is a different size should
    /// centre it, not reproduce the numbers.
    /// </remarks>
    public static (int Width, int Height) ImpliedIconSize => (8, 6);
}
