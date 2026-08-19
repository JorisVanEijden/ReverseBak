namespace GameData.Resources.Scene;

using GameData.Resources.Character;

/// <summary>
/// Playing the lute at a tavern for coin — <c>container_PerformBarding</c> @0x4dd1e (ovr149),
/// reached as location action code 9.
/// </summary>
/// <remarks>
/// <b>Barding is busking, not haggling.</b> The skill of that name buys nothing in a shop; it is
/// what a character earns with when they play. The tavern's own record carries how hard its crowd
/// is to please and what its entertainment fund holds, and the fund is spent ONCE.
/// </remarks>
public static class Barding {
    /// <summary>The tavern has nothing left to pay with.</summary>
    public const int FundTappedOutDialog = 71;

    /// <summary>A performance good enough to be worth the full purse.</summary>
    public const int ExcellentDialog = 90;

    /// <summary>Competent: a few mangled notes, and half the purse.</summary>
    public const int DecentDialog = 89;

    /// <summary>Saved by a drunk crowd — paid despite the playing.</summary>
    public const int DrunkPatronsDialog = 88;

    /// <summary>Thrown out with the lute taken off you.</summary>
    public const int FailedDialog = 73;

    /// <summary>What the party learns from a performance they were good enough for.</summary>
    public const int ExperienceWhenCapable = 2;

    /// <summary>What they learn from one they were not.</summary>
    /// <remarks>
    /// <b>Awarded even when the performance fails outright.</b> The experience is handed out on the
    /// way in, before the outcome is decided, so being thrown out of a tavern still teaches
    /// something — and it goes to the WHOLE PARTY, not to whoever played.
    /// </remarks>
    public const int ExperienceWhenOutmatched = 1;

    /// <summary>Whether the party is up to this crowd — their best barder against its difficulty.</summary>
    /// <remarks>
    /// <b>The party's best, not the leader's.</b> The original asks
    /// <c>getHighestValueInParty(Barding)</c>, so the character who plays is whoever is best at it.
    /// </remarks>
    public static bool IsCapable(int difficulty, int partyBestBarding) =>
        difficulty < partyBestBarding;

    /// <summary>How much a performance earns, as a multiple of the tavern's fund.</summary>
    /// <returns>Zero when the crowd turns on them, which is also what ends the visit.</returns>
    /// <remarks>
    /// Four outcomes, in the order the original tests them:
    /// <list type="bullet">
    /// <item>capable and comfortably so — <c>(difficulty + 100) / 2 &lt;= skill</c> — the whole fund;</item>
    /// <item>capable but only just — half of it;</item>
    /// <item>outmatched, but not by much — <c>difficulty * 3 / 4 &lt;= skill</c> — a quarter of the
    /// tenfold purse, which the drunk crowd pays anyway;</item>
    /// <item>outmatched badly — nothing, and thrown out.</item>
    /// </list>
    /// <para>The arithmetic is the original's: the fund is multiplied by ten FIRST and the division
    /// truncates, so the tiers are not exactly a half and a quarter of each other.</para>
    /// </remarks>
    public static int Reward(int fund, int difficulty, int partyBestBarding) {
        if (fund == 0) {
            return 0;
        }

        if (IsCapable(difficulty, partyBestBarding)) {
            return (difficulty + 100) / 2 <= partyBestBarding
                ? fund * 10
                : fund * 10 / 2;
        }

        return difficulty * 3 / 4 > partyBestBarding ? 0 : fund * 10 / 4;
    }

    /// <summary>Which line the tavern keeper speaks.</summary>
    public static int DialogFor(int fund, int difficulty, int partyBestBarding) {
        if (fund == 0) {
            return FundTappedOutDialog;
        }

        if (IsCapable(difficulty, partyBestBarding)) {
            return (difficulty + 100) / 2 <= partyBestBarding ? ExcellentDialog : DecentDialog;
        }

        return difficulty * 3 / 4 > partyBestBarding ? FailedDialog : DrunkPatronsDialog;
    }

    /// <summary>
    /// How the experience is applied: as skill USE, not as a flat addition.
    /// </summary>
    /// <remarks>
    /// <b>A trap worth stating.</b> The call is
    /// <c>ChangeAttributeValueForWholeParty(Barding, amount, CurrentBase)</c>, and IDA types that
    /// third parameter as a <c>WhichValue</c> whose member 3 is named <c>CurrentBase</c> — but the
    /// routine it forwards to is <c>stat_combatant_modify</c>, which reads the same argument as its
    /// change MODE, and mode 3 there is skill-use advancement.
    ///
    /// <para>So the amount is a number of uses put through the per-skill rate, not points added to
    /// the stored value. Going by the parameter's name would hand a flat +2 to every member, which
    /// for a skill already near 100 is far more than the original grants — advancement there is
    /// deliberately slow.</para>
    /// </remarks>
    public const StatChangeMode ExperienceMode = StatChangeMode.SkillUse;

    /// <summary>The Barding experience a performance grants the party.</summary>
    public static int ExperienceFor(int fund, int difficulty, int partyBestBarding) =>
        fund == 0
            ? 0
            : IsCapable(difficulty, partyBestBarding)
                ? ExperienceWhenCapable
                : ExperienceWhenOutmatched;

    /// <summary>
    /// Whether the party is thrown out — which the location turns into an exit.
    /// </summary>
    /// <remarks>
    /// <b>Only an outright failure ends the visit.</b> A tavern with nothing left to pay says so and
    /// the party stays: the routine returns its success flag untouched on that path, so "no money"
    /// and "no talent" are not the same answer. See <c>GdsActionDispatch.ActionAfterBarding</c> —
    /// a failure becomes a sub-scene transition and walks them out through the scene's own exit.
    /// </remarks>
    public static bool ThrownOut(int fund, int difficulty, int partyBestBarding) =>
        fund != 0 && !IsCapable(difficulty, partyBestBarding)
        && difficulty * 3 / 4 > partyBestBarding;

    /// <summary>
    /// Whether the tavern's fund is spent by this performance.
    /// </summary>
    /// <remarks>
    /// <b>One paid performance per tavern, ever.</b> A reward zeroes the fund on the container, so
    /// coming back finds it tapped out — and a performance that earned nothing leaves it intact to
    /// try again.
    /// </remarks>
    public static bool SpendsTheFund(int reward) => reward > 0;

    /// <summary>The song that plays, by how good the player is.</summary>
    /// <remarks>Four bands, and the best one is a DIFFERENT tune rather than the same one played
    /// better — the skill picks the piece, not the performance.</remarks>
    public static int SongFor(int partyBestBarding) =>
        partyBestBarding < 45 ? 1008
        : partyBestBarding < 65 ? 1040
        : partyBestBarding < 85 ? 1039
        : 1007;
}
