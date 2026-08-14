namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// What the cast screen opens on — <c>cspell_cast_menu_loop</c> (<c>SRC/COMBAT/SPELL/CSPELL.C</c>).
///
/// <para><b>The screen is sticky.</b> Two values live in the save — which party slot is casting and
/// which of the six schools is showing — and the screen reopens where you left it. Both start
/// <see cref="None"/> on a new game (<c>SAVEGAME.C</c> resets them), and both are written back
/// <b>when the screen closes, whether or not a spell was cast</b>: browsing the schools and backing
/// out still changes what you see next time.</para>
///
/// <para><b>Combat does not share it.</b> The combat caller passes no pointers at all, so a combat
/// cast neither reads nor updates the pair — a combatant's school lives on the combatant. Wiring
/// combat into the same state would leak the overworld's selection into battle and back.</para>
/// </summary>
public static class CastMenuSelection {
    /// <summary>Nothing remembered yet — the value a new game starts at.</summary>
    public const int None = -1;

    /// <summary>
    /// The school shown when nothing is remembered.
    ///
    /// <para><b>The last one, not the first.</b> The original opens on 5; defaulting to 0 would put
    /// a different set of symbols on the ring the first time the screen is ever opened.</para>
    /// </summary>
    public const int DefaultSchool = CastRingLayout.CategoryCount - 1;

    /// <summary>Party slots the screen offers faces for.</summary>
    public const int PartySlots = 3;

    /// <summary>Dialog played when a face that cannot cast is clicked.</summary>
    public const int CannotCastDialogId = 0xd8;

    /// <summary>
    /// Which party slot the screen opens as the caster.
    /// </summary>
    /// <param name="rememberedSlot">The saved slot, or <see cref="None"/>.</param>
    /// <param name="canCast">Per active-party slot, whether that character is a caster.</param>
    /// <remarks>
    /// <b>The remembered slot is honoured only if that character can still cast.</b> The party is
    /// reordered and swapped between chapters, so the slot that was casting last may now hold a
    /// non-caster — the original re-checks and falls back to the <i>first</i> caster in the party
    /// rather than opening the screen on someone with no spells.
    /// </remarks>
    /// <returns>The slot, or <see cref="None"/> when the party has no caster at all.</returns>
    public static int ResolveCasterSlot(int rememberedSlot, IReadOnlyList<bool> canCast) {
        if (canCast == null) {
            return None;
        }
        if (rememberedSlot >= 0 && rememberedSlot < canCast.Count && canCast[rememberedSlot]) {
            return rememberedSlot;
        }
        for (var slot = 0; slot < canCast.Count; slot++) {
            if (canCast[slot]) {
                return slot;
            }
        }
        return None;
    }

    /// <summary>Which school the ring opens on.</summary>
    public static int ResolveSchool(int rememberedSchool) =>
        rememberedSchool >= 0 && rememberedSchool < CastRingLayout.CategoryCount
            ? rememberedSchool
            : DefaultSchool;

    /// <summary>
    /// Whether clicking a party face switches the caster to it.
    /// </summary>
    /// <remarks>
    /// A face that cannot cast is <b>not</b> disabled — it is clickable and answers with
    /// <see cref="CannotCastDialogId"/>, leaving the current caster in place. Greying it out would
    /// lose the explanation the original gives.
    /// </remarks>
    public static bool CanSelect(int partySlot, IReadOnlyList<bool> canCast) =>
        canCast != null && partySlot >= 0 && partySlot < canCast.Count && canCast[partySlot];
}
