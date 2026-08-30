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
    /// <b>CORRECTED 2026-08-30 against the disassembly: NO ROPE DOES EXPLAIN ITSELF.</b> This said
    /// the original "takes a path that says nothing about ropes" and warned that explaining it
    /// would be less faithful. The opposite is true — the rope count is tested first (@0x79cb1) and
    /// failing it jumps to a branch that shows <see cref="NoRopeDialog"/>, whose text is "If we only
    /// had a rope we could probably swing across this pit." A port that stayed silent would be the
    /// unfaithful one.
    ///
    /// <para><b>The GEOMETRY failures are the silent ones</b>, and that is the distinction the old
    /// remark blurred: a pit at an unusable angle, or a party outside the band, both fall to a flag
    /// test that returns without a word. So the three outcomes are not two — an explained refusal,
    /// a silent one, and the offer.</para>
    /// </remarks>
    public static bool CanOffer(int ropeCount, int rotationZ, int alongPit, int pitAlongPit) =>
        ropeCount > 0
        && AxisOf(rotationZ) != PitAxis.None
        && IsLinedUp(alongPit, pitAlongPit);

    // ------------------------------------------------------------------ the swing itself

    /// <summary>How far the party moves per frame of the traverse.</summary>
    /// <remarks>
    /// <b>The walk to the start point uses the same increment as the crossing.</b> It is one
    /// stepped move, not a teleport to the near edge followed by an animation.
    /// </remarks>
    public const int StepUnits = 100;

    /// <summary>Half-width of the band in which the rope sags, measured from the pit's centre.</summary>
    public const int SagRadius = 600;

    /// <summary>The height the rope hangs from, before the sag is subtracted.</summary>
    public const int SagAnchorHeight = 0x1C2;

    /// <summary>The constant inside the sag's square root — the anchor circle's radius squared.</summary>
    public const int SagRadiusSquared = 0x4DEF9;

    /// <summary>The swing sound, played at the exact centre of the crossing.</summary>
    /// <remarks>
    /// <b>At the centre, not on entering the sag band.</b> One cue per crossing; firing it on the
    /// band boundary would play it twice, once on each side.
    /// </remarks>
    public const int SwingSoundId = 0x32;

    /// <summary>
    /// Shown on a SECONDARY click — "The cavernous pit stretched across the narrow corridor…"
    /// </summary>
    /// <remarks>
    /// The dispatch tests the button first (@0x79c9c) and sends anything but a primary click here,
    /// before the rope count is even read. So examining a pit works with no rope and from any
    /// position, which the crossing does not.
    /// </remarks>
    public const int ExamineDialog = 177;

    /// <summary>
    /// Shown when the party carries no rope — "If we only had a rope…"
    /// </summary>
    /// <remarks>
    /// <b>Not to be confused with <see cref="OutOfRopeDialog"/>.</b> This one refuses a crossing
    /// that never started; that one reports the last rope breaking after one that did. Both mention
    /// rope and they are different moments.
    /// </remarks>
    public const int NoRopeDialog = 198;

    /// <summary>
    /// <b>A completed crossing SPENDS a rope.</b>
    /// </summary>
    /// <remarks>
    /// Established 2026-08-30 from the routine's tail (@0x7a11e): once the party lands, it calls
    /// <c>useItem(Rope)</c> — <c>party_consumeOneOfKindFromAnyMember</c> — and only then re-counts.
    /// So crossings are limited by the ropes carried, and a port that omits this gives unlimited
    /// crossings from one rope.
    /// </remarks>
    public static bool CrossingConsumesARope => true;

    /// <summary>Shown when that consumption took the LAST rope — dialog 0x114.</summary>
    /// <remarks>
    /// <b>This is the RAN-OUT message, not the no-rope refusal.</b> That distinction was right and
    /// is now sharper: the refusal is <see cref="NoRopeDialog"/>, and this one fires only when the
    /// post-crossing re-count comes back zero (@0x7a12e). Carrying two ropes and crossing once
    /// spends one and says nothing.
    /// </remarks>
    public const int OutOfRopeDialog = 0x114;

    /// <summary>
    /// The eye height partway across — a circular sag on the rope.
    /// </summary>
    /// <param name="distanceFromCentre">Distance along the travel axis from the pit's centre.</param>
    /// <remarks>
    /// <c>z = 0x1C2 - isqrt(0x4DEF9 - d^2)</c> inside <see cref="SagRadius"/>, and flat outside it.
    /// The integer square root is the original's, so the curve is stepped rather than smooth —
    /// reproducing it with a float gives a subtly different dip at every frame of the crossing.
    /// </remarks>
    public static int SagHeightAt(int distanceFromCentre) {
        int d = distanceFromCentre < 0 ? -distanceFromCentre : distanceFromCentre;
        if (d >= SagRadius) {
            return 0;
        }
        return SagAnchorHeight - IntegerSquareRoot(SagRadiusSquared - (d * d));
    }

    /// <summary>Whether the swing cue fires at this point of the crossing.</summary>
    public static bool PlaysSwingSound(int distanceFromCentre) => distanceFromCentre == 0;

    /// <summary>
    /// Whether the rope sags at this point — i.e. whether <see cref="SagHeightAt"/>'s answer is a
    /// height at all.
    /// </summary>
    /// <remarks>
    /// <b>Outside the band <see cref="SagHeightAt"/> returns 0, and 0 is a SENTINEL rather than a
    /// height.</b> The original writes <c>z = saved_z</c> there — the walking eye — not zero, and a
    /// caller that took the 0 literally would drop the party to the floor for the approach and then
    /// snap them up onto the rope. The formula can also legitimately produce a height near 0 around
    /// <c>d = 341</c>, so testing the return value cannot separate the two cases. Test the distance.
    /// </remarks>
    public static bool IsSagging(int distanceFromCentre) =>
        (distanceFromCentre < 0 ? -distanceFromCentre : distanceFromCentre) < SagRadius;

    /// <summary>
    /// The heading the party faces to cross — from the start point toward the landing.
    /// </summary>
    /// <param name="crossingAxisIsY">
    /// True when the swing travels along Y (the pit lies along X, <see cref="PitAxis.AlongX"/>).
    /// </param>
    /// <remarks>
    /// <b>The party is turned to face the crossing before it starts</b>, and the original animates
    /// that turn (<c>worldmove_animate_hdg_tgt</c>) rather than snapping it. The heading itself is
    /// not a choice: it is the direction from <paramref name="startAcross"/> to
    /// <paramref name="landingAcross"/>, which <see cref="LandingPosition"/> has already put on the
    /// far side.
    ///
    /// <para>The angles follow <c>RoadTravel.AxisOffset</c>'s convention, where 0 is +Y and the
    /// circle runs anticlockwise — so +X is 0xC000, not 0x4000. Getting that backwards sends the
    /// swing away from the pit.</para>
    /// </remarks>
    public static ushort CrossingHeading(bool crossingAxisIsY, int startAcross, int landingAcross) {
        bool positive = landingAcross > startAcross;
        if (crossingAxisIsY) {
            return positive ? (ushort)0x0000 : (ushort)0x8000;
        }
        return positive ? (ushort)0xC000 : (ushort)0x4000;
    }

    /// <summary>The original's integer square root — truncating, never rounding.</summary>
    private static int IntegerSquareRoot(int value) {
        if (value <= 0) {
            return 0;
        }
        var root = 0;
        var bit = 1 << 30;
        while (bit > value) {
            bit >>= 2;
        }
        int remainder = value;
        while (bit != 0) {
            if (remainder >= root + bit) {
                remainder -= root + bit;
                root = (root >> 1) + bit;
            } else {
                root >>= 1;
            }
            bit >>= 2;
        }
        return root;
    }
}
