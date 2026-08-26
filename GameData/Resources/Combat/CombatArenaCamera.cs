namespace GameData.Resources.Combat;

using GameData.Resources.Config;

/// <summary>
/// The pose the arena is viewed from — a SEPARATE camera from the one the party walks behind.
/// </summary>
/// <remarks>
/// <b>Two cameras, two rules, and the difference is the height.</b> The explore view's eye is
/// GROUND-RELATIVE: the step routines write the scanned ground z into the camera and the zone
/// default is added on top. The arena's is <b>absolute</b> — <see cref="HeightIsAbsolute"/> — set on
/// the scratch camera only the combat renderer draws through. Reusing the walking rule here would
/// lift the arena view by the terrain height under the party and tilt the whole fight off the
/// ground on any slope.
///
/// <para><b>Only two of the six components change.</b> The arena keeps the explore camera's X, Y and
/// YAW, so the fight faces wherever the party was looking, and overwrites just the height and the
/// pitch. That is what makes <see cref="CombatArenaPlacement"/>'s offsets — which are stated
/// relative to the party's heading — land in front of the camera.</para>
///
/// <para><b>The discriminator is the zone KIND, not the chapter.</b> The engine branches on the
/// zone's location field being 2, which is what the three dungeon zones ship; canassa reads the same
/// branch as chapter-dependent. See <see cref="StartData.CombatCameraHeightUnderground"/>.</para>
/// </remarks>
public static class CombatArenaCamera {
    /// <summary>
    /// Whether <see cref="HeightFor"/> is a world z or an offset above the ground under the party.
    /// </summary>
    /// <remarks>
    /// A world z. Stated as a constant rather than left to a comment because the explore camera's
    /// neighbouring value is the opposite and the two have been confused before.
    /// </remarks>
    public const bool HeightIsAbsolute = true;

    /// <summary>The arena camera's height, in game units.</summary>
    public static int HeightFor(StartData start, bool underground) =>
        underground ? start.CombatCameraHeightUnderground : start.CombatCameraHeightAboveGround;

    /// <summary>The arena camera's downward tilt, in the engine's 16-bit angle units.</summary>
    /// <remarks>
    /// <b>Not "the underground pitch" in every situation.</b> The look-straight-down targeting view
    /// hard-codes -90° instead of reading this, so a caller that wants that view must not come here
    /// for it.
    /// </remarks>
    public static int PitchFor(StartData start, bool underground) =>
        underground ? start.CombatCameraPitchUnderground : start.CombatCameraPitchAboveGround;

    /// <summary>Whether a start record carries a usable pose at all.</summary>
    /// <remarks>
    /// A zero height is the tell that START.DAT never loaded; the shipped values are 1024 and 800.
    /// A caller that gets false should leave the camera where it is rather than drop it to the
    /// floor.
    /// </remarks>
    public static bool IsUsable(StartData start, bool underground) =>
        start != null && HeightFor(start, underground) > 0;
}
