namespace GameData.Resources.Character;

/// <summary>
/// Disarming a trapped chest — the <c>trapped</c> arm of <c>handle_Container</c> @0x77284
/// (case 0 of its click switch, 0x77377).
/// </summary>
/// <remarks>
/// <b>You are only ever offered this if you can already see the trap.</b> The whole branch is
/// gated on Scent of Sarig being active (<c>IsSpellTimerActive</c> at 0x7737f) AND the chest
/// actually carrying trap damage. Without the spell the game never mentions a trap at all — it
/// asks the blunter question instead (see <see cref="OpenAnywayDialog"/>), so a player with no
/// detection has no way to tell a trapped chest from a safe one until they open it.
///
/// <para>The disarm itself is the <b>same shape as picking a lock</b> and deliberately shares its
/// reward: no roll, a strict comparison against the party's best LockPicking, and
/// <see cref="PicklockAttempt.SkillOnSuccess"/> on success. It is not a second mechanic, which is
/// why the constant is reused rather than restated.</para>
/// </remarks>
public static class ChestTrapDisarm {
    /// <summary>
    /// "It's trapped… Shall we try to deactivate it?" — asked only when the trap has been detected.
    /// </summary>
    public const int PromptDialog = 190;

    /// <summary>"The trap was deactivated."</summary>
    public const int SuccessDialog = 191;

    /// <summary>
    /// "Do you want to open the (ex)trapped chest?" — the question asked when there is no detection
    /// to go on, or nothing left to disarm.
    /// </summary>
    public const int OpenAnywayDialog = 317;

    /// <summary>
    /// Whether the game offers to disarm at all.
    /// </summary>
    /// <param name="scentOfSarigActive">Whether the detection spell's timer is running.</param>
    /// <param name="trapDamage">The chest's trap damage; 0 means nothing to disarm.</param>
    /// <remarks>
    /// Both conditions, not either: the branch falls through to
    /// <see cref="OpenAnywayDialog"/> if the spell is inactive OR the damage is already zero — the
    /// second being how a chest whose trap was disarmed earlier stops re-offering.
    /// </remarks>
    public static bool IsOffered(bool scentOfSarigActive, int trapDamage) =>
        scentOfSarigActive && trapDamage != 0;

    /// <summary>
    /// Whether the attempt succeeds.
    /// </summary>
    /// <param name="lockScore">The chest's lock difficulty — the same score a pick is judged by.</param>
    /// <param name="bestPartyLockPicking">
    /// The highest LockPicking in the party (<c>getHighestValueInParty</c>), not the leader's.
    /// </param>
    /// <remarks>
    /// <b>Strictly less, and no roll.</b> The original jumps to the failure arm on
    /// <c>difficulty &gt;= skill</c>, so a score exactly equal to the party's best still fails, and
    /// the same party either can or cannot disarm a given chest every time. A percentage reading
    /// would make it feel like a gamble when it is not.
    /// </remarks>
    public static bool Succeeds(int lockScore, int bestPartyLockPicking) =>
        lockScore < bestPartyLockPicking;

    /// <summary>
    /// Skill awarded to the character who disarmed it — the same award picking a lock gives.
    /// </summary>
    public const int SkillOnSuccess = PicklockAttempt.SkillOnSuccess;

    /// <summary>
    /// Nothing is said when it fails, and the trap stays armed.
    /// </summary>
    /// <remarks>
    /// The failure arm only sets a flag and falls through — there is no "you failed" line. The
    /// player finds out by opening the chest, which is the point: a message would give away for
    /// free what the spell is for.
    /// </remarks>
    public static bool AnnouncesFailure => false;
}
