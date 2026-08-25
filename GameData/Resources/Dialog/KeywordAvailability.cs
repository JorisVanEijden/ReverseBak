namespace GameData.Resources.Dialog;

using System.Collections.Generic;

/// <summary>
/// Whether a conversation topic is currently on offer — IDA <c>EvaluateDialogCondition</c>
/// (ovr144 @0x4acc1).
///
/// <para><b>Availability is not purely data-driven.</b> The general rule is two flags, but fifteen
/// specific topics carry hand-written conditions compiled into the executable — party inventory, a
/// named chapter, spells known by a named character. A port that reads only the DDX data will offer
/// those topics at the wrong times and never know why.</para>
/// </summary>
public static class KeywordAvailability {
    /// <summary>
    /// Save-state key base of the per-topic <b>suppression</b> flag: <c>6700 + key</c>.
    /// </summary>
    public const int SuppressedFlagBase = 6700;

    /// <summary>The suppression key for a topic.</summary>
    public static int SuppressedFlag(int globalKey) => SuppressedFlagBase + globalKey;

    /// <summary>
    /// The general rule, which every topic passes through.
    /// </summary>
    /// <remarks>
    /// <b>The suppression flag has the last word</b> — it is applied after the hand-written cases,
    /// not instead of them, so a topic whose special condition says "yes" is still withdrawn when it
    /// is suppressed. Checking the two flags in the other order, or treating the special cases as
    /// overrides, lets retired topics come back.
    /// </remarks>
    public static bool IsAvailable(int ownFlagValue, int suppressedFlagValue) =>
        ownFlagValue != 0 && suppressedFlagValue == 0;

    /// <summary>What a hand-written condition asks about.</summary>
    public enum Requirement {
        /// <summary>The party must NOT be carrying the item.</summary>
        PartyLacksItem,

        /// <summary>A named save-state flag must be set.</summary>
        FlagSet,

        /// <summary>Either of two named flags must be set.</summary>
        EitherFlagSet,

        /// <summary>The story must be at a particular chapter.</summary>
        AtChapter,

        /// <summary>A named character must NOT yet know a particular spell.</summary>
        SpellNotKnown,

        /// <summary>Two flags set and an item carried — the most specific gate in the table.</summary>
        TwoFlagsAndItem,

        /// <summary>
        /// The topic's own flag is <b>replaced</b> by another before the general rule runs, rather
        /// than being tested alongside it.
        /// </summary>
        FlagRedirect,

        /// <summary>A gate whose backing routine is not modelled here.</summary>
        Unmodelled,
    }

    /// <summary>One topic's hand-written condition.</summary>
    public readonly struct SpecialCase {
        public SpecialCase(Requirement requirement, int first = 0, int second = 0, string note = "") {
            Requirement = requirement;
            First = first;
            Second = second;
            Note = note;
        }

        public Requirement Requirement { get; }

        /// <summary>Flag key, chapter, or spell word — read the <see cref="Requirement"/>.</summary>
        public int First { get; }

        /// <summary>Second flag key, or spell bit mask.</summary>
        public int Second { get; }

        /// <summary>What the parameters mean where a number alone does not say it.</summary>
        public string Note { get; }
    }

    /// <summary>
    /// The fifteen topics with conditions compiled into the executable, by keyword key.
    /// </summary>
    /// <remarks>
    /// <b>Several are "ask about what you still need": they are offered only while the party does
    /// NOT hold the item.</b> Get the sense backwards and the topic appears exactly when it has
    /// stopped being useful.
    ///
    /// <para>Two of them name a <i>specific party member's</i> spellbook, so the condition is not
    /// even about the party in general. Object ids are given by the names the disassembly uses;
    /// resolve them against the object table before use.</para>
    /// </remarks>
    public static readonly IReadOnlyDictionary<int, SpecialCase> SpecialCases =
        new Dictionary<int, SpecialCase> {
            [9] = new SpecialCase(Requirement.PartyLacksItem, note: "Waani"),
            [11] = new SpecialCase(Requirement.PartyLacksItem, note: "Bag of Grain"),
            [17] = new SpecialCase(Requirement.Unmodelled, 40004, note: "stub128 query"),
            [44] = new SpecialCase(Requirement.FlagSet, 8044),
            [71] = new SpecialCase(Requirement.SpellNotKnown, 1, 0x10, note: "Owyn"),
            [76] = new SpecialCase(Requirement.PartyLacksItem, note: "Rations"),
            [103] = new SpecialCase(Requirement.Unmodelled, 40004, note: "stub128 query"),
            [106] = new SpecialCase(Requirement.SpellNotKnown, 3, 0x200, note: "Owyn"),
            [117] = new SpecialCase(Requirement.AtChapter, 6),
            [130] = new SpecialCase(Requirement.FlagRedirect, 56222),
            [132] = new SpecialCase(Requirement.EitherFlagSet, 51021, 6521),
            [133] = new SpecialCase(Requirement.FlagRedirect, 56212),
            [148] = new SpecialCase(Requirement.PartyLacksItem, note: "Rations"),
            [163] = new SpecialCase(Requirement.TwoFlagsAndItem, 142, 170, note: "Abbot's Journal"),
            [164] = new SpecialCase(Requirement.FlagSet, 6514),
        };

    /// <summary>Whether this topic has a condition the data does not express.</summary>
    public static bool HasSpecialCase(int globalKey) => SpecialCases.ContainsKey(globalKey);

    /// <summary>What an availability decision came out as, and how much of it was actually applied.</summary>
    public readonly struct Decision {
        public Decision(bool available, Requirement? unevaluated) {
            Available = available;
            Unevaluated = unevaluated;
        }

        /// <summary>Whether the topic is offered.</summary>
        public bool Available { get; }

        /// <summary>
        /// The gate that could NOT be evaluated, or null when the answer is complete.
        /// </summary>
        /// <remarks>
        /// <b>Non-null means the answer used the general rule alone.</b> Because twelve of the
        /// fifteen gates NARROW, that errs toward offering a topic the original would hide — worth
        /// reporting rather than silently returning a bare bool.
        /// </remarks>
        public Requirement? Unevaluated { get; }
    }

    /// <summary>
    /// Decide whether a topic is offered, applying its hand-written gate where the inputs exist.
    /// </summary>
    /// <param name="globalKey">The topic's keyword key.</param>
    /// <param name="ownFlagValue">The topic's own flag.</param>
    /// <param name="suppressedFlagValue">Its <see cref="SuppressedFlag"/>.</param>
    /// <param name="flagValue">Reads any save-state flag; required for the flag-based gates.</param>
    /// <param name="chapter">The current chapter.</param>
    /// <param name="partyCarriesItem">
    /// Whether the party carries an item, by OBJECT ID. Null when unavailable — the item gates then
    /// report themselves unevaluated rather than guessing.
    /// </param>
    /// <param name="knowsSpell">
    /// Whether a character knows a spell, by (character, spell word). Null when unavailable.
    /// </param>
    /// <remarks>
    /// <b>THE SUPPRESSION FLAG HAS THE LAST WORD, on every path.</b> A special condition answering
    /// "yes" is still withdrawn when the topic is suppressed — the original falls through the same
    /// tail check whatever the gate said. Treating the special cases as overrides lets retired
    /// topics come back.
    ///
    /// <para><b>The gates NARROW; they do not grant.</b> Twelve of the fifteen bail out first when
    /// the topic's own flag is clear. The two <see cref="Requirement.FlagRedirect"/> cases are the
    /// exception and a different shape: they REPLACE the value the general rule then tests.</para>
    /// </remarks>
    public static Decision Evaluate(int globalKey, int ownFlagValue, int suppressedFlagValue,
        Func<int, int> flagValue, int chapter,
        Func<int, bool> partyCarriesItem = null, Func<int, int, bool> knowsSpell = null) {
        if (!SpecialCases.TryGetValue(globalKey, out SpecialCase special)) {
            return new Decision(IsAvailable(ownFlagValue, suppressedFlagValue), null);
        }

        // A redirect swaps the value the general rule tests, then the general rule runs as usual.
        if (special.Requirement == Requirement.FlagRedirect) {
            if (flagValue == null) {
                return new Decision(IsAvailable(ownFlagValue, suppressedFlagValue), special.Requirement);
            }
            return new Decision(
                IsAvailable(flagValue(special.First), suppressedFlagValue), null);
        }

        bool? extra = EvaluateExtra(special, flagValue, chapter, partyCarriesItem, knowsSpell);
        if (extra == null) {
            // Inputs missing: fall back to the general rule and SAY so.
            return new Decision(IsAvailable(ownFlagValue, suppressedFlagValue), special.Requirement);
        }

        // Narrowing: the extra condition and the general rule must BOTH hold.
        return new Decision(
            extra.Value && IsAvailable(ownFlagValue, suppressedFlagValue), null);
    }

    private static bool? EvaluateExtra(SpecialCase special, Func<int, int> flagValue, int chapter,
        Func<int, bool> partyCarriesItem, Func<int, int, bool> knowsSpell) {
        switch (special.Requirement) {
            case Requirement.FlagSet:
                return flagValue == null ? null : flagValue(special.First) != 0;
            case Requirement.EitherFlagSet:
                return flagValue == null
                    ? null
                    : flagValue(special.First) != 0 || flagValue(special.Second) != 0;
            case Requirement.AtChapter:
                return chapter == special.First;
            case Requirement.PartyLacksItem:
                // The object id is recorded by SYMBOL NAME in the table's note, not resolved against
                // the object table, so there is nothing to ask about yet.
                return null;
            case Requirement.SpellNotKnown:
                return knowsSpell == null ? null : !knowsSpell(special.First, special.Second);
            case Requirement.TwoFlagsAndItem:
                return null;   // needs the item half; see PartyLacksItem
            default:
                return null;   // Unmodelled, and anything added without an arm
        }
    }

    /// <summary>
    /// The condition for a topic, or <c>null</c> when the general rule is the whole story.
    /// </summary>
    public static SpecialCase? SpecialCaseFor(int globalKey) =>
        SpecialCases.TryGetValue(globalKey, out SpecialCase c) ? c : (SpecialCase?)null;

    /// <summary>
    /// Whether a special case runs its extra condition at all.
    /// </summary>
    /// <remarks>
    /// <b>The topic's own flag is still the gate.</b> Twelve of the fifteen open with "if the own
    /// flag is clear, unavailable" and only then test their extra condition — the hand-written part
    /// narrows availability, it does not grant it. The two redirects are the exception: they
    /// <i>replace</i> the value the general rule then tests.
    /// </remarks>
    public static bool ExtraConditionApplies(SpecialCase specialCase, int ownFlagValue) =>
        specialCase.Requirement == Requirement.FlagRedirect || ownFlagValue != 0;
}
