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
    /// <summary>The REQ action id of the first school button.</summary>
    /// <remarks>
    /// <b>The six school buttons are action ids 2..7, in school order.</b> The dispatch is a jump
    /// table with one arm per id, each setting the symbol file to <c>id - 2</c> — so the mapping is
    /// positional and does not need a lookup table of its own.
    /// </remarks>
    public const int FirstSchoolActionId = 2;

    /// <summary>The REQ action id of the exit button.</summary>
    public const int ExitActionId = 1;

    /// <summary>The school a button selects, or -1 when the id is not a school button.</summary>
    public static int SchoolForAction(int actionId) {
        int school = actionId - FirstSchoolActionId;
        return school >= 0 && school < CastRingLayout.CategoryCount ? school : -1;
    }

    /// <summary>Help shown when a school button is right-clicked.</summary>
    public const int SchoolButtonHelpDialog = 311;

    /// <summary>Help shown when the exit button is right-clicked.</summary>
    public const int ExitButtonHelpDialog = 312;

    /// <summary>Shown when a party member who cannot cast is picked.</summary>
    /// <remarks>
    /// <b>Picking a non-caster is refused out loud, not ignored.</b> The screen says so rather than
    /// silently keeping the previous caster, which is the difference between a button that looks
    /// broken and one that explains itself.
    /// </remarks>
    public const int NotASpellcasterDialog = 216;

    /// <summary>The REQ action id of the first party-member click area.</summary>
    /// <remarks>
    /// <b>Clicking a party portrait switches who is casting.</b> Ids 128..130 are the three active
    /// slots; the dispatch indexes the active roster by <c>id - 128</c>. Re-picking the current
    /// caster is a no-op rather than a reload.
    /// </remarks>
    public const int FirstPartySlotActionId = 128;

    /// <summary>The active-roster slot a click area refers to, or -1.</summary>
    public static int PartySlotForAction(int actionId) =>
        Character.ActiveParty.SlotForAction(actionId, FirstPartySlotActionId);
    /// <summary>Nothing remembered yet — the value a new game starts at.</summary>
    public const int None = -1;

    /// <summary>
    /// The school shown when nothing is remembered — <c>SYMBOL6.DAT</c>, index 5.
    /// </summary>
    /// <remarks>
    /// <b>Read from the original, after a screenshot argued the other way and lost.</b>
    /// <c>cspell_cast_menu_loop</c> (CSPELL.C:2181) assigns <c>school = 5</c> when the sticky value
    /// is unset, and <c>cspell_symbol_resources_load</c> (CSPELL.C:1745) turns that into a filename
    /// with <c>szFile[6] = chapter + '1'</c> — so school 5 is <c>SYMBOL6.DAT</c>, matching this
    /// index and <c>CastScreen.LoadSchoolAsync</c>'s <c>SYMBOL{school + 1}.DAT</c>.
    ///
    /// <para><b>This was briefly changed to 4 and that was wrong.</b> The argument was a
    /// measurement: opening the screen with Owyn above ground draws an EMPTY ring on SYMBOL6,
    /// because the only spell he knows there is Candle Glow and Candle Glow is refused above
    /// ground. That looked like proof of an off-by-one. It is not — <b>the original's ring draws
    /// only castable spells</b> (<c>cspell_menu_animate_hilite</c> gates every glyph on
    /// <c>cspell_check_castable</c>, CSPELL.C:1840), so the original opens on the same empty ring
    /// from the same save. An empty ring is the correct rendering of that state, not a symptom.</para>
    ///
    /// <para>The "the original names Scent of Sarig on open" observation that supported the change
    /// is a SYMBOL5 reading, and the screen is sticky
    /// (<c>g_gameState.nSpellMenuPreselect</c>, SPELLFX.C:38) — a save that last browsed SYMBOL5
    /// reopens there. It says nothing about the value used when nothing is remembered.</para>
    ///
    /// <para><b>Confirmed live in both games, 2026-09-12, and this paragraph is why it did not become
    /// a second wrong change.</b> Opening the cast screen from <c>dir.G01/SAVE01</c> names Scent of
    /// Sarig in the original — the same observation, made again — and the sticky field explains it on
    /// both sides: the original's live <c>nSpellMenuPreselect</c> reads <b>4</b>, the save's own byte
    /// at body offset 1624 reads <b>4</b>, and our screen opens on <c>_school = 4</c>. The two agree,
    /// SYMBOL5 is school index 4, and nothing here is a default. The trap is that the spell NAME on
    /// screen invites you to match it against the spellbook's group order, which is INVSPELL.DAT's
    /// and not the ring's.</para>
    /// </remarks>
    public const int DefaultSchool = SymbolFileSix - 1;

    /// <summary>The 1-based SYMBOL file the original opens on, kept so the -1 above has a reason.</summary>
    private const int SymbolFileSix = 6;

    /// <summary>Party slots the screen offers faces for.</summary>
    /// <inheritdoc cref="Character.ActiveParty.Slots"/>
    public const int PartySlots = Character.ActiveParty.Slots;

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
