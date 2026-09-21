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
    ///
    /// <para><b>Deliberately callerless.</b> A pinned fact; the remark above says which neighbouring value is the opposite.</para>
    /// </remarks>
    public const bool HeightIsAbsolute = true;

    /// <summary>
    /// The extra height the arena camera takes UNDERGROUND, on top of START.DAT's field.
    /// </summary>
    /// <remarks>
    /// <b>The shipped 800 is not the height the fight is viewed from.</b> The last nine
    /// instructions of <c>combat_captureArenaBackdrop</c> (@0x222a2, underground arm only) raise the
    /// camera's world z by this much and rebuild the view, <i>after</i> the backdrop still has been
    /// captured — so the backdrop is drawn from 800 and the fight itself is projected from 1310. The
    /// surface arm returns before any of it, which is why only the underground arena is affected.
    ///
    /// <para><b>It is the sign of the vertical placement, not a tweak.</b> With the arena 3350 ahead
    /// and the camera pitched 16.64° down, 800 puts the near row 3.2° ABOVE the camera axis and 1310
    /// puts it 4.7° BELOW — the near row crosses frame centre. No field of view can do that, which is
    /// what sent TASK-604 looking for a pose difference in the first place.</para>
    /// </remarks>
    public const int UndergroundHeightRaise = 510;

    /// <summary>The arena camera's height, in game units.</summary>
    /// <remarks>
    /// Underground this is START.DAT's field <b>plus</b> <see cref="UndergroundHeightRaise"/>; see
    /// there for why the shipped 800 is not what the fight is viewed from.
    /// </remarks>
    public static int HeightFor(StartData start, bool underground) =>
        underground
            ? start.CombatCameraHeightUnderground + UndergroundHeightRaise
            : start.CombatCameraHeightAboveGround;

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
