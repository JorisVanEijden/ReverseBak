namespace GameData.Resources.Data;

/// <summary>
/// A trapped chest — <c>handle_Container</c>'s case 0 (ovr190 @0x77377).
/// </summary>
/// <remarks>
/// <b>The trap is not sprung by opening blind.</b> Every path ends at a yes/no prompt, so the player
/// always chooses to open a chest they have been told about — or one they were never told about,
/// which is the same prompt with a different line. Nothing here opens a chest on the player's behalf.
/// </remarks>
public static class ChestTrap {
    /// <summary>
    /// The spell that reveals a trap — <b>Scent of Sarig</b>.
    /// </summary>
    /// <remarks>
    /// <b>Without it there is no warning and no chance to disarm.</b> The branch is gated on that one
    /// spell's timer being active; an unprotected party is simply asked whether to open the chest,
    /// with no hint that anything is wrong. So the spell is not a convenience — it is the entire
    /// detection mechanic, and a port that always offers a disarm gives away information the game
    /// charges a spell for.
    /// </remarks>
    /// <remarks>Its number lives on <c>FieldSpells</c>, not <c>SpellIds</c> — the two classes both
    /// name spells and neither is a superset.</remarks>
    public const int DetectionSpell = Spells.FieldSpells.ScentOfSarig;

    /// <summary>The attribute a disarm is rolled against.</summary>
    /// <remarks>Lockpicking, attribute 13 — the party's best, not the clicker's.</remarks>
    public const int DisarmAttribute = 13;

    /// <summary>Whether the party is warned and offered a disarm.</summary>
    public static bool Detected(bool detectionSpellActive, int trapDamage) =>
        detectionSpellActive && trapDamage != 0;

    /// <summary>
    /// Whether a disarm attempt succeeds.
    /// </summary>
    /// <remarks>
    /// <b>Strictly greater, and there is no roll.</b> The comparison is
    /// <c>difficulty &gt;= best</c> to FAIL, so a party whose best lockpicking exactly equals the
    /// difficulty fails — and the outcome is deterministic, so re-trying the same chest with the same
    /// party can never succeed. A port that adds a die here makes a fixed obstacle into a
    /// save-scummable one.
    /// </remarks>
    public static bool DisarmSucceeds(int bestLockpicking, int difficulty) =>
        bestLockpicking > difficulty;

    /// <summary>Lockpicking awarded to the actor who disarms it.</summary>
    /// <remarks>
    /// Two points, in the same use-based mode every other skill award uses — and only on SUCCESS.
    /// Failing teaches nothing.
    ///
    /// <para><b>Pointed at the picklock constant rather than restating 2</b>, because the disarm is
    /// deliberately the same shape as picking a lock and shares its reward. Two literal 2s could
    /// drift apart, and nothing would catch it.</para>
    /// </remarks>
    public const int DisarmSkillAward = Character.PicklockAttempt.SkillOnSuccess;

    /// <summary>
    /// <b>Nothing is said when a disarm fails, and the trap stays armed.</b>
    /// </summary>
    /// <remarks>
    /// The failure arm sets a flag and falls through — there is no "you failed" line. The player
    /// finds out by opening the chest, which is the point: a message would give away for free what
    /// the detection spell is for.
    ///
    /// <para>A failed attempt does not spring the trap either. It costs the attempt and nothing
    /// else, landing on exactly the prompt an undetected trap shows.</para>
    /// </remarks>
    public static bool AnnouncesFailure => false;

    /// <summary>
    /// <b>A disarmed trap is gone for good.</b>
    /// </summary>
    /// <remarks>
    /// Success zeroes the record's trap damage, so the chest is permanently safe — the state lives in
    /// the container, not in a session flag. Leaving it set would re-arm the trap on the next visit.
    /// </remarks>
    public static bool DisarmIsPermanent => true;

    // ---- the four lines, and which is shown when --------------------------------------------------

    /// <summary>Shown when the spell reveals the trap; asks whether to attempt a disarm.</summary>
    public const int DetectedPromptDialog = 190;

    /// <summary>Shown when the disarm succeeds.</summary>
    public const int DisarmedDialog = 191;

    /// <summary>
    /// Asks whether to open a chest that is <b>still trapped</b> — undetected, or a failed disarm.
    /// </summary>
    public const int OpenStillTrappedDialog = 79;

    /// <summary>Asks whether to open a chest whose trap has been dealt with.</summary>
    /// <remarks>
    /// <b>A defused chest still asks.</b> Different line from the armed one, and easy to drop on the
    /// grounds that there is nothing left to fear — but the prompt is what the player expects after
    /// the effort of disarming it.
    /// </remarks>
    public const int OpenExTrappedDialog = 317;

    /// <summary>Which prompt an open attempt shows.</summary>
    /// <remarks>
    /// <b>A FAILED DISARM DOES NOT SPRING THE TRAP.</b> It falls through to exactly the same prompt
    /// an undetected trap shows — so failing costs the attempt and nothing else, and the player is
    /// still asked before anything happens.
    /// </remarks>
    public static int OpenPromptFor(int trapDamage) =>
        trapDamage != 0 ? OpenStillTrappedDialog : OpenExTrappedDialog;

    /// <summary>
    /// The detonation text, shown BEFORE the damage — ddx <b>192</b>.
    /// </summary>
    /// <remarks>
    /// <i>"Something clicked… and suddenly the box detonated into flame and hurtling splinters."</i>
    /// </remarks>
    public const int DetonationDialog = 192;

    /// <summary><c>sound_trapexpl</c> (57), played as the explosion starts.</summary>
    /// <remarks>
    /// <b>Its return is kept and tested before the unload</b>, so only a play that actually started
    /// is released — the same rule <c>RiftMachineUse.UnloadsOnlyWhatItLoaded</c> records.
    /// </remarks>
    public const int ExplosionCue = 57;

    /// <summary>
    /// The pool change one sprung trap deals — <b>negative, and SHIFTED</b>.
    /// </summary>
    /// <param name="trapDamage">The lock record's <c>TrapDamage</c> byte.</param>
    /// <remarks>
    /// <b>*** THE SHIFT IS THE WHOLE TRAP. ***</b> The original is
    /// <c>-(trapDamage &lt;&lt; 8)</c>: the byte is a whole-point value and the amount is
    /// fixed-point with the low byte as its fraction. **Passing the byte raw deals 1/256th of the
    /// damage** — a trapped chest that tickles, and nothing on screen says so. The same scale is
    /// why a full heal elsewhere is <c>0x7fff</c> rather than 127.
    /// </remarks>
    public static long DamageDelta(int trapDamage) => -((long)trapDamage << 8);

    /// <summary>
    /// The third argument, and it is <b>not a <see cref="Character.StatChangeMode"/></b>.
    /// </summary>
    /// <remarks>
    /// The call passes <c>100</c>, which that enum does not define — for the combined pool it is the
    /// heal-target percent <c>ModifyHealthPool</c> takes. Worth stating because the same routine
    /// passes a real <c>StatChangeMode</c> (3, skill use) for its disarm award a few lines earlier,
    /// so the nearer call is the wrong one to copy.
    /// </remarks>
    public const int DamageHealTargetPercent = 100;

    /// <summary>
    /// <b>The damage hits the WHOLE PARTY</b>, not whoever opened the box.
    /// </summary>
    /// <remarks>
    /// <c>ChangeAttributeValueForWholeParty</c>, and against
    /// <see cref="ActorAttribute.HealthStaminaCombo"/> — the combined pool, not Health alone.
    /// </remarks>
    public static bool DamageHitsTheWholeParty => true;

    /// <summary>
    /// <b>A sprung trap is SPENT</b>: the record's <c>TrapDamage</c> is zeroed and written back.
    /// </summary>
    /// <remarks>
    /// Which is what finally gives <see cref="OpenExTrappedDialog"/> something to describe. Until a
    /// trap could spring, ddx 317 was reachable only for a chest authored with no trap at all — the
    /// prompt split was ported ahead of the mechanic that makes it mean anything.
    /// </remarks>
    public static bool SpringingSpendsTheTrap => true;
}
