namespace GameData.Resources.World;

/// <summary>
/// Firing the catapult — <c>handle_Catapult</c> (ovr190 @0x770aa).
/// </summary>
/// <remarks>
/// <b>IT DOES NOT SUMMON AN ACTOR.</b> TASK-136 describes it as "summons a scripted actor"; there is
/// no actor, no encounter spawn and no roster touch anywhere in the routine. It animates the world
/// item's own sprite frame, plays a sound and disposes the container. The task's description was
/// written from the neighbouring handlers.
///
/// <para>Same container shape as <see cref="GraveDigging"/>: a <c>fixedWorldItem</c> looked up by the
/// item's world position, carrying dialog AND encounter data. Missing any of those does nothing.</para>
/// </remarks>
/// <remarks>
/// <b>AWAITING ITS FEATURE (TASK-136).</b> Nothing dispatches a catapult hotspot yet, and the
/// container-at-a-location lookup it shares with the grave has no caller.
///
/// <para>Read by <c>scripts/audit-unconsumed-models.py</c>: it separates a rule ported ahead
/// of its feature from an orphan nobody owns, so the audit stays a signal instead of a list
/// to re-triage every run.</para>
/// </remarks>
public static class CatapultUse {
    /// <summary>Shown for a SECONDARY click — the engineer's appraisal.</summary>
    /// <remarks>
    /// "The design was standard. @4 examined the catapult with great interest, noting it was
    /// remarkably similar to the models used in the Kingdom."
    /// </remarks>
    public const int ExamineDialog = 167;

    /// <summary>Shown when the encounter data carries no global key.</summary>
    /// <remarks>
    /// "@0 shrugged. \"This must not be very important,\" he said as he turned to leave." — the same
    /// generic line the grave uses (<see cref="GraveDigging.NothingHereDialog"/>).
    /// </remarks>
    public const int NothingHereDialog = 154;

    /// <summary>
    /// <b>The catapult fires only when its GLOBAL FLAG is set</b> — not on an item, not on a skill.
    /// </summary>
    /// <param name="globalKey">The encounter data's <c>globalDataKey1</c>.</param>
    /// <param name="globalValue">What <c>GetGlobalValue</c> answers for it.</param>
    /// <remarks>
    /// Two separate gates that read alike and are not: a key of <b>zero</b> means the object has no
    /// story attached and shows <see cref="NothingHereDialog"/>; a real key whose VALUE is zero
    /// means the story has not reached this point, and shows the container's own dialog and then
    /// stops silently. Collapsing them loses the second line entirely.
    /// </remarks>
    public static bool Fires(int globalKey, int globalValue) => globalKey != 0 && globalValue != 0;

    /// <summary>Whether the object has any story attached at all.</summary>
    public static bool HasAnything(int globalKey) => globalKey != 0;

    /// <summary>
    /// The sprite frames the world item is stepped through, in order.
    /// </summary>
    /// <remarks>
    /// Two loops, and together they read as a wind-back and a release: the first counts <b>down</b>
    /// from 1 to 0, the second counts <b>up</b> from 0 to 1, each frame redrawn. Four writes, not
    /// two — a single 0..1 sweep drops half the motion.
    /// </remarks>
    public static readonly int[] FrameSequence = { 1, 0, 0, 1 };

    /// <summary>
    /// <b>Only the LOW BYTE of the item's frame word is written.</b>
    /// </summary>
    /// <remarks>
    /// <c>ax = (current &amp; 0xFF00) | frame</c>. The high byte carries something else and is
    /// preserved across every step; writing the whole word would clear it.
    /// </remarks>
    public static int FrameWord(int currentWord, int frame) => (currentWord & 0xFF00) | (frame & 0xFF);

    /// <summary>
    /// <b>The sound plays AFTER the animation, not during it.</b>
    /// </summary>
    /// <remarks>
    /// All four frames are drawn and presented first, and only then does
    /// <c>audio_sound_play(sound_catapult)</c> run — non-blocking, no repeat. Playing it on the
    /// first frame would put the crack of the arm before the arm moves.
    ///
    /// <para>It is also <b>loaded and unloaded around the sequence</b>
    /// (<c>audio_load_catapult_sound</c> / <c>audio_unload_catapult_sound</c>) rather than living in
    /// a resident bank — one of the per-context bank loads TASK-144 records as IDA-only.</para>
    /// </remarks>
    public static bool SoundFollowsTheAnimation => true;

    /// <summary>
    /// <b>Any primary click on a valid catapult DISPOSES its container — fired or not.</b>
    /// </summary>
    /// <remarks>
    /// The dispose sits on the shared tail that all three primary outcomes reach: no global key, key
    /// present but unset, and the full firing. Only the secondary-click examine returns without it.
    /// So the object is one-shot from the first primary click, whatever that click achieved.
    /// </remarks>
    public static bool PrimaryClickAlwaysDisposesTheContainer => true;
}
