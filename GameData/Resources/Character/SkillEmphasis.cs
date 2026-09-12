namespace GameData.Resources.Character;

using System;

/// <summary>
/// Marking a rating for study — the character sheet's per-skill emphasis, and what the mark on its
/// bar means. Read from <c>charscreen_info_loop</c> @0x58378 (ovr160).
/// </summary>
/// <remarks>
/// <b>The mark at the end of a bar is not decoration.</b> The end marker
/// <see cref="CharacterSheetRow.BarEndMarkerIcon"/> is drawn exactly when this flag is set
/// (<c>UI_show_attribute</c> reads the same global at 0x57e24), so the sheet is showing which
/// ratings the character is concentrating on. A renderer that drew the marker for its own reasons
/// — or never — would be telling the player the wrong thing about their own choices.
///
/// <para><b>Clicking a rating is what sets it.</b> The row's own click area toggles this; the help
/// line says so in as many words ("Left clicking on a skill can emphasize or de-emphasize how much
/// the character focuses on learning that skill"). It is the input side of
/// <c>StatEngine.Modify</c>'s <c>studyBonusPer52</c>, which is otherwise a parameter with nobody to
/// supply it.</para>
/// </remarks>
public static class SkillEmphasis {
    /// <summary>
    /// Base of the per-attribute emphasis flags.
    /// </summary>
    /// <remarks>
    /// A second array beside the "changed since you last looked" flags
    /// (<see cref="CharacterSheetRow.ChangedFlagBase"/>), with the same per-actor stride and the
    /// same indexing. Two arrays, two meanings, 120 apart — reading one for the other would make
    /// every improvement look like a study choice.
    /// </remarks>
    public const int FlagBase = 6230;

    /// <inheritdoc cref="CharacterSheetRow.AttributesPerActor"/>
    public const int AttributesPerActor = CharacterSheetRow.AttributesPerActor;

    /// <summary>The flag key for one actor's attribute.</summary>
    public static int FlagFor(int actorNumber, int attributeNumber) =>
        FlagBase + (actorNumber * AttributesPerActor) + attributeNumber;

    /// <summary>Whether a rating is marked for study.</summary>
    public static bool IsEmphasised(int flagValue) => flagValue != 0;

    /// <summary>
    /// What a click writes back.
    /// </summary>
    /// <remarks>
    /// <b>A plain boolean toggle</b> — <c>value = (old == 0) ? 1 : 0</c> at 0x5859c, so a flag left
    /// holding some other number by a save comes back as 1 rather than being incremented. There is
    /// no limit on how many ratings a character may emphasise at once; the original counts nothing.
    /// </remarks>
    public static int Toggled(int flagValue) => flagValue == 0 ? 1 : 0;

    /// <summary>
    /// Whether this rating can be marked at all.
    /// </summary>
    /// <param name="maximum">The actor's maximum for it.</param>
    /// <remarks>
    /// <b>Tested on the MAXIMUM, and the click is dropped when it is zero</b> (0x5857d) — the same
    /// "never had it" case that prints N/A instead of a percentage. So a rating the character does
    /// not possess cannot be studied, and the click does nothing rather than saying why.
    /// </remarks>
    public static bool CanEmphasise(int maximum) => maximum != 0;

    /// <summary>The line shown when a rating row is asked about rather than clicked.</summary>
    public const int HelpDialog = 323;

    /// <summary>Action id of the first rating row on the sheet.</summary>
    public const int FirstRowActionId = 128;

    /// <summary>The attribute a rating row stands for.</summary>
    /// <remarks>
    /// The rows cover the lower half only, so row 0 is attribute
    /// <see cref="CharacterSheetLayout.LowerHalfFirstAttribute"/> — which is why the original adds
    /// -124 to the action id where the help arm adds -128 (0x5856e against 0x58551): the toggle
    /// wants the ATTRIBUTE and the help wants the ROW.
    /// </remarks>
    public static int AttributeForRow(int rowIndex) =>
        rowIndex + CharacterSheetLayout.LowerHalfFirstAttribute;

    /// <summary>The row a rating-row action id stands for, or -1 for any other id.</summary>
    public static int RowForAction(int actionId) {
        int row = actionId - FirstRowActionId;

        return row >= 0 && row < CharacterSheetLayout.LowerHalfAttributeCount ? row : -1;
    }

    /// <summary>How many of the actor's first sixteen ratings are marked for study.</summary>
    /// <param name="flagValue">Reads a global flag by key — the session's own lookup.</param>
    /// <remarks>
    /// <b>Sixteen, against a stride of seventeen.</b> <c>charscreen_recalc_train_rates</c>
    /// (CHARSCRN.C:373) counts <c>i &lt; 0x10</c> while indexing <c>memberIdx * 0x11 + i</c>, so the
    /// seventeenth slot — the Health/Stamina combo pseudo-attribute — is addressable but never
    /// counted. Counting it would dilute every rate by one whenever it happened to be set.
    /// </remarks>
    public static int EmphasisedCount(Func<int, int> flagValue, int actorNumber) {
        if (flagValue == null) {
            return 0;
        }
        int count = 0;
        for (int attribute = 0; attribute < CountedAttributes; attribute++) {
            if (IsEmphasised(flagValue(FlagFor(actorNumber, attribute)))) {
                count++;
            }
        }
        return count;
    }

    /// <summary>Attributes the count above covers — the first sixteen of seventeen.</summary>
    public const int CountedAttributes = 16;

    /// <summary>
    /// The actor's study rate: <c>26 / count</c>, or zero when nothing is marked.
    /// </summary>
    /// <remarks>
    /// <c>g_gameState.aSkillTrainRate[memberIdx] = count != 0 ? 0x1a / count : 0</c>
    /// (CHARSCRN.C:379), integer division. It is the numerator of
    /// <see cref="StatEngine.Modify"/>'s <c>studyBonusPer52</c>, whose denominator is 52 — so one
    /// marked rating is <b>+50%</b>, two are +25%, three +15%, and thirteen or more round down to
    /// +4% or +2%. The walkthrough's "if it is the ONLY skill selected it will rise 50% faster" is
    /// this line.
    ///
    /// <para>Computed on demand rather than cached. The original keeps an array and recalculates it
    /// for every member whenever the sheet changes; the flags are the source of truth either way,
    /// and recomputing sixteen flag reads at the point of use cannot go stale the way a cache that
    /// misses a toggle can.</para>
    /// </remarks>
    public static int TrainRate(int emphasisedCount) =>
        emphasisedCount != 0 ? RateNumerator / emphasisedCount : 0;

    /// <summary>The 0x1a the rate divides.</summary>
    public const int RateNumerator = 0x1a;

    /// <summary>
    /// The study bonus to hand <see cref="StatEngine.Modify"/> for one change to one rating.
    /// </summary>
    /// <param name="flagValue">Reads a global flag by key.</param>
    /// <param name="actorNumber">The actor, for both the count and the per-rating test.</param>
    /// <param name="attribute">Which rating is changing.</param>
    /// <param name="isPartyMember">
    /// False for anyone who is not in the party. <c>STAT.C:271</c> gates the whole bonus on
    /// <c>actor-&gt;charSlot != 0</c>, so a monster gets none however its flags read.
    /// </param>
    /// <remarks>
    /// <b>The rate is per ACTOR but the test is per RATING.</b> Emphasising Lockpicking does not
    /// speed up Barding: STAT.C:271 asks for this attribute's own flag before applying the actor's
    /// rate. Getting that wrong would turn one mark into a blanket bonus on everything.
    ///
    /// <para><b>And it applies to every change MODE, not only skill use.</b> The bonus sits after
    /// the mode switch and before the <c>frac</c> banking (STAT.C:264-273), so an absolute award to
    /// a marked rating is boosted too.</para>
    /// </remarks>
    public static int BonusFor(Func<int, int> flagValue, int actorNumber, int attribute,
        bool isPartyMember) {
        if (!isPartyMember || flagValue == null
            || !IsEmphasised(flagValue(FlagFor(actorNumber, attribute)))) {
            return 0;
        }
        return TrainRate(EmphasisedCount(flagValue, actorNumber));
    }
}
