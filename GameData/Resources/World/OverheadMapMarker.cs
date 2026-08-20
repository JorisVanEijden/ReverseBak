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
///
/// <para><b>Both branches centre the icon on the viewport; only the icon differs.</b> They reach it
/// by different routes, which is what makes it look like two rules. North-up blits the direction's
/// icon itself and does its own centring (the -4 / -3 below). The turning map hands icon 0 to the
/// scaling blit at 0x0400 &gt;&gt; 10 — exactly 1:1 — and that routine subtracts half the scaled size
/// before drawing, so the centring is done for it and no offset appears at the call site. A port
/// reading only the call sites would conclude the default marker is drawn corner-first at the
/// centre, and place it half an icon down and to the right.</para>
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
    /// The icon a heading draws: the direction under north-up, the fixed arrow otherwise.
    /// </summary>
    /// <remarks>
    /// The whole choice, in the form a renderer wants it — the two branches of <c>drawMap</c>
    /// differ in nothing else that a caller can see.
    /// </remarks>
    public static int IconIndexFor(int yaw, bool northUp) =>
        northUp ? DirectionFor(yaw) : FixedArrowIndex;

    /// <summary>The icon the turning map always draws: 0, the north arrow.</summary>
    /// <remarks>
    /// Not a separate "you are here" graphic — the default branch pushes the sheet's base pointer
    /// with no index added, so it draws the same icon north-up uses for due north. It reads as a
    /// fixed arrow because the world beneath it has already been turned.
    /// </remarks>
    public const int FixedArrowIndex = 0;

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

    /// <summary>The icon size: 8 x 7 VGA pixels, as the sheet actually holds them.</summary>
    /// <remarks>
    /// Measured from MAPICONS.BMX (40 x 42 extracted, i.e. 8 x 7 before the x5 / x6 aspect scale),
    /// not inferred. The original's -4 / -3 agrees with it: C truncates 7 / 2 to 3, so a half-height
    /// of 3 is what an 8 x 7 sprite gives. An earlier reading took the -3 at face value and called
    /// the sheet 8 x 6, which is one row short of what is there.
    ///
    /// <para>Kept as a check on the sheet rather than as coordinates to reproduce: a port centres
    /// the icon it actually loaded (<see cref="TopLeftFor"/>), which is why being one off here never
    /// moved anything.</para>
    /// </remarks>
    public static (int Width, int Height) ImpliedIconSize => (8, 7);

    /// <summary>
    /// <b>The sheet runs anticlockwise from north</b> — 0 = N, 4 = W, 8 = S, 12 = E.
    /// </summary>
    /// <remarks>
    /// Which is the engine's own yaw direction, so indexing the sheet with
    /// <see cref="DirectionFor"/> needs no correction. It matters to a port that draws the marker by
    /// ROTATING one arrow sprite instead of picking from the sheet: screen rotation is clockwise, so
    /// that port needs the negated angle. Feeding it +yaw mirrors the marker about the north-south
    /// axis, which is correct at exactly N and S and wrong at the fourteen headings in between.
    /// </remarks>
    public static bool SheetRunsAnticlockwise => true;
}
