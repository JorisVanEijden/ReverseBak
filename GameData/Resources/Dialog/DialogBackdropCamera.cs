namespace GameData.Resources.Dialog;

using System.Collections.Generic;

/// <summary>
/// Where the camera looks while a backdrop dialog is up — <c>ExecuteDialog</c>'s
/// <see cref="DialogEntryFlags.IsolatePalette"/> arm (IDA 0x49a10-0x49a98).
/// </summary>
/// <remarks>
/// <b>Only the YAW is special.</b> The original writes the height as
/// <c>defaultCameraZ + &lt;the ground term&gt;</c> and the pitch as <c>defaultCameraPitch</c> — which
/// is the ordinary walking eye, not a pose of its own. Our <c>PartyMovement</c> already states its
/// altitude as "zone DefaultCameraZ plus the ground height under the party", the same expression
/// arrived at independently, so height and pitch need no override at all: leave the explore camera
/// where it is and turn it.
///
/// <para><b>That is why a reference shot of a travel dialog shows a flat green field</b> while the
/// party is standing beside a corpse on a road. The shot is of a different bearing, not a different
/// place, and not a painted fill.</para>
///
/// <para>The turn happens only when the speaker is a party member. A townsman, a sign or a narrator
/// leaves the camera alone, so their dialogs are framed on whatever the player was looking at.</para>
/// </remarks>
public static class DialogBackdropCamera {
    /// <summary>Turns the camera roughly about-face — <c>xor ax, 8400h</c>.</summary>
    /// <remarks>
    /// <b>XOR, not addition.</b> 0x8400 is half a turn (0x8000) plus a little, but the operation is
    /// a bitwise flip of the saved yaw, so it is not the same as adding 0x8400 for every input —
    /// only for those whose bit 0x0400 is clear. Applied to the SAVED yaw, not to an already
    /// modified one.
    /// </remarks>
    public const int AboutFaceMask = 0x8400;

    /// <summary>How far the second and third companions are turned off that bearing.</summary>
    /// <remarks>
    /// Slot 1 is turned back by this and slot 2 forward by it (<c>sub</c> then <c>add</c> at
    /// 0x49a83 / 0x49a93), so the three active members are each framed against a different
    /// direction. Slot 0 takes no offset.
    /// </remarks>
    public const int SlotYawOffset = 0x2800;

    /// <summary>Actor numbers above this are not party members and never turn the camera.</summary>
    /// <remarks>
    /// The original's gate is <c>actorNr &gt; 6</c> — and then a second test, that the character is
    /// actually in the ACTIVE party, because the six are the whole cast and only three walk at once.
    /// </remarks>
    public const int MaxPartyActorNumber = 6;

    /// <summary>
    /// The speaker's place in the marching order, or -1 when they are not walking with the party.
    /// </summary>
    /// <param name="activeParty">Character ids in marching order — <c>GameSession.ActivePartyIndices</c>.</param>
    /// <param name="actorNumber">The dialog entry's actor number. <b>One-based</b>: character id + 1.</param>
    /// <remarks>
    /// <b>Not found is NOT slot 0.</b> The original's loop leaves its counter at zero when nothing
    /// matches, which would silently frame a stranger as though they led the party; the guard that
    /// saves it there is the active-party test before the loop, and this returns -1 so a caller
    /// cannot skip it.
    /// </remarks>
    public static int SpeakerSlot(IReadOnlyList<byte> activeParty, int actorNumber) {
        if (activeParty == null || actorNumber <= 0 || actorNumber > MaxPartyActorNumber) {
            return -1;
        }
        int characterId = actorNumber - 1;
        for (int i = 0; i < activeParty.Count; i++) {
            if (activeParty[i] == characterId) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>Whether this speaker turns the camera at all.</summary>
    public static bool TurnsCamera(IReadOnlyList<byte> activeParty, int actorNumber) =>
        SpeakerSlot(activeParty, actorNumber) >= 0;

    /// <summary>
    /// The yaw to render the backdrop's world view with.
    /// </summary>
    /// <param name="savedYaw">The camera's yaw before the dialog — the one to restore afterwards.</param>
    /// <param name="speakerSlot"><see cref="SpeakerSlot"/>'s answer; negative leaves the yaw alone.</param>
    /// <remarks>
    /// Wraps in 16 bits, like every other angle in the original: the offsets are applied to a value
    /// that is about to overflow half the time, and widening the arithmetic puts the second and
    /// third companions' bearings somewhere the game never looks.
    /// </remarks>
    public static ushort YawFor(ushort savedYaw, int speakerSlot) {
        if (speakerSlot < 0) {
            return savedYaw;
        }
        var yaw = (ushort)(savedYaw ^ AboutFaceMask);
        return speakerSlot switch {
            1 => (ushort)(yaw - SlotYawOffset),
            2 => (ushort)(yaw + SlotYawOffset),
            _ => yaw,
        };
    }
}
