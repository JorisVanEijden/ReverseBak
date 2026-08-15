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
    /// <summary>The REQ layout the cast screen loads in a tactical encounter.</summary>
    public const string CombatLayout = "spell.dat";

    /// <summary>The REQ layout it loads outside one.</summary>
    public const string FieldLayout = "req_cast.dat";

    /// <summary>
    /// Which of the two layouts the screen loads.
    /// </summary>
    /// <param name="casterHasCombatData">The caster is a combatant rather than a party record.</param>
    /// <remarks>
    /// <b>The discriminator is the caster, not a mode flag.</b> The screen tests whether the actor it
    /// was handed carries combat data and loads <c>spell.dat</c> if so, <c>req_cast.dat</c> if not —
    /// so the two REQ files are the same screen in its two call contexts, chosen by what is casting
    /// rather than by where.
    /// </remarks>
    public static string LayoutFor(bool casterHasCombatData) =>
        casterHasCombatData ? CombatLayout : FieldLayout;

    /// <summary>
    /// Where the opening school comes from, which is <b>not the same source in the two contexts</b>.
    /// </summary>
    /// <param name="casterHasCombatData">The caster is a combatant.</param>
    /// <param name="combatantSchool">The combatant's own stored school.</param>
    /// <param name="rememberedSchool">The overworld's sticky value, or <see cref="None"/>.</param>
    /// <remarks>
    /// In combat it is read straight off the combatant, so a combat cast neither reads nor writes the
    /// sticky pair. In the field it is the remembered value, falling back to
    /// <see cref="DefaultSchool"/>. Feeding the sticky value into a combat cast would leak the
    /// overworld's selection into battle.
    /// </remarks>
    public static int OpeningSchool(bool casterHasCombatData, int combatantSchool,
        int rememberedSchool) =>
        casterHasCombatData ? combatantSchool : ResolveSchool(rememberedSchool);

    /// <summary>
    /// <b>Only one school's symbols are in memory at a time.</b>
    /// </summary>
    /// <remarks>
    /// Switching schools disposes the loaded symbol and ring data and reads the next set — the six
    /// <c>SYMBOL*.DAT</c> files are streamed one at a time rather than all held at once. A port is
    /// free to preload all six, but should know that the original's school switch is a load, which
    /// is why it plays a sound and redraws rather than swapping instantly.
    /// </remarks>
    public static bool SchoolSwitchReloadsSymbolData => true;

    public static bool CanSelect(int partySlot, IReadOnlyList<bool> canCast) =>
        canCast != null && partySlot >= 0 && partySlot < canCast.Count && canCast[partySlot];
}
