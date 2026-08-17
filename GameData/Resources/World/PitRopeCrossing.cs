namespace GameData.Resources.World;

/// <summary>
/// Swinging across a pit on a rope — <c>handle_Pit</c> @0x79c63 (ovr195).
/// </summary>
/// <remarks>
/// <b>This is the crossing, not the falling.</b> A <c>m_pit</c> POLYGON is walkable and dropping
/// into it is a separate consequence delivered by the movement loop (collision spec §3.4). This is
/// the world OBJECT you click on: a chasm with a hook above it that the party swings over.
/// </remarks>
public static class PitRopeCrossing {
    /// <summary>The rope the party must be carrying — object 82.</summary>
    public const int RopeObjectId = 82;

    /// <summary>"Shall we try to swing across?" — the offer, and a confirm.</summary>
    public const int OfferDialog = 197;

    /// <summary>
    /// How far along the pit's own axis the party may stand and still be lined up with it.
    /// </summary>
    /// <remarks>
    /// <b>Exclusive on both sides</b> — the original uses <c>jg</c>/<c>jl</c>, so a party exactly
    /// 300 units off centre is refused. The band is what stops you swinging from the far END of a
    /// long pit, where there is no hook.
    /// </remarks>
    public const int LateralBand = 300;

    /// <summary>How far across the pit the swing lands the party.</summary>
    public const int CrossingSpan = 900;

    /// <summary>
    /// The four axis-aligned facings a crossable pit can have, as raw rotation values.
    /// </summary>
    /// <remarks>
    /// <b>A pit at any other angle cannot be crossed at all</b>, and says nothing — the handler
    /// falls straight through to the no-offer path. 0 and 0x8000 are the same axis pointing
    /// opposite ways, as are 0x4000 and 0xC000, which is why the test is four equalities rather
    /// than two.
    /// </remarks>
    public const int RotationEast = 0x0000;

    /// <inheritdoc cref="RotationEast"/>
    public const int RotationWest = 0x8000;

    /// <inheritdoc cref="RotationEast"/>
    public const int RotationNorth = 0x4000;

    /// <inheritdoc cref="RotationEast"/>
    public const int RotationSouth = 0xC000;

    /// <summary>Which way a pit lies, or that it lies at no usable angle.</summary>
    public enum PitAxis {
        /// <summary>Not axis-aligned — uncrossable.</summary>
        None,

        /// <summary>Lies along X: the party lines up in X and swings across Y.</summary>
        AlongX,

        /// <summary>Lies along Y: the party lines up in Y and swings across X.</summary>
        AlongY,
    }

    /// <summary>The axis a pit's rotation puts it on.</summary>
    public static PitAxis AxisOf(int rotationZ) {
        int rotation = rotationZ & 0xFFFF;
        if (rotation == RotationEast || rotation == RotationWest) {
            return PitAxis.AlongX;
        }

        return rotation == RotationNorth || rotation == RotationSouth
            ? PitAxis.AlongY
            : PitAxis.None;
    }

    /// <summary>
    /// Whether the party is lined up well enough to be offered the crossing.
    /// </summary>
    /// <param name="alongPit">
    /// The party's coordinate on the pit's OWN axis — x for <see cref="PitAxis.AlongX"/>, y for
    /// <see cref="PitAxis.AlongY"/>.
    /// </param>
    /// <param name="pitAlongPit">The pit's coordinate on that same axis.</param>
    /// <remarks>
    /// Note which axis this tests: the one the pit RUNS ALONG, not the one the swing crosses. The
    /// party has to be beside the hook, and may be any distance back from the edge.
    /// </remarks>
    public static bool IsLinedUp(int alongPit, int pitAlongPit) =>
        alongPit > pitAlongPit - LateralBand && alongPit < pitAlongPit + LateralBand;

    /// <summary>
    /// Where the party lands, on the axis the swing crosses.
    /// </summary>
    /// <param name="acrossPit">The party's coordinate on the crossing axis.</param>
    /// <param name="pitAcrossPit">The pit's coordinate on that axis.</param>
    /// <remarks>
    /// <b>The far side, chosen by which side you are on</b> — the original compares the two and
    /// picks <c>pit + 900</c> or <c>pit - 900</c> accordingly, so the swing always crosses rather
    /// than depositing you back where you started.
    /// </remarks>
    public static int LandingPosition(int acrossPit, int pitAcrossPit) =>
        acrossPit > pitAcrossPit ? pitAcrossPit - CrossingSpan : pitAcrossPit + CrossingSpan;

    /// <summary>
    /// Whether the party can be offered the swing at all.
    /// </summary>
    /// <param name="ropeCount">Ropes held across the whole party, not one member's pack.</param>
    /// <remarks>
    /// <b>No rope means no offer and no explanation</b> — the original checks the count before it
    /// looks at the pit at all, and takes a path that says nothing about ropes. A port that
    /// explained the missing rope would be more helpful than the original and less faithful; that
    /// is a deliberate call for whoever wires this, not a detail to smooth over silently.
    /// </remarks>
    public static bool CanOffer(int ropeCount, int rotationZ, int alongPit, int pitAlongPit) =>
        ropeCount > 0
        && AxisOf(rotationZ) != PitAxis.None
        && IsLinedUp(alongPit, pitAlongPit);
}
