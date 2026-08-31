namespace GameData.Resources.World;

/// <summary>
/// Switching on the rift machine — <c>handle_RiftMachine</c> (ovr190 @0x782c0).
/// </summary>
/// <remarks>
/// <b>IT IS NOT A SHOVEL/DIG OBJECT.</b> TASK-136 calls it "another shovel/dig object"; there is no
/// shovel, no digging and no item test anywhere in it. Like <see cref="CatapultUse"/> it is gated on
/// a global flag and plays an effect — but by a different mechanism, so the two are not one handler
/// with a parameter.
/// </remarks>
/// <remarks>
/// <b>AWAITING ITS FEATURE (TASK-136).</b> Nothing dispatches a rift-machine hotspot yet, and the
/// container-at-a-location lookup it shares with the grave and the catapult has no caller.
///
/// <para>Read by <c>scripts/audit-unconsumed-models.py</c>: it separates a rule ported ahead
/// of its feature from an orphan nobody owns, so the audit stays a signal instead of a list
/// to re-triage every run.</para>
/// </remarks>
public static class RiftMachineUse {
    /// <summary>Shown for a SECONDARY click.</summary>
    /// <remarks>
    /// "The rift machine hummed. Formed by a pair of staves driven into the ground, and topped by
    /// two metallic, mushroom shaped objects, it sometimes formed the shadows of the moredhel
    /// waiting at the other side of the gate…"
    /// </remarks>
    public const int ExamineDialog = 180;

    /// <summary>The generic "not important" line, shared with the grave and the catapult.</summary>
    public const int NothingHereDialog = 154;

    /// <summary>
    /// <b>It accepts TWO container types</b>, unlike its two siblings.
    /// </summary>
    /// <remarks>
    /// <c>fixedWorldItem</c> <b>or</b> <c>containerType_9</c>. The grave and the catapult both
    /// require <c>fixedWorldItem</c> alone and bail on anything else, so a shared "is this a world
    /// container" helper written from either of them refuses half the rift machines.
    /// </remarks>
    public static bool AcceptsContainerType(int containerType) =>
        containerType == FixedWorldItemType || containerType == SecondAcceptedType;

    /// <summary>The type all three handlers accept.</summary>
    public const int FixedWorldItemType = 1;

    /// <summary>The extra type only this handler accepts.</summary>
    public const int SecondAcceptedType = 9;

    /// <summary>
    /// Whether the machine runs: a real global key whose value is set.
    /// </summary>
    /// <remarks>
    /// Same two-gate shape as <see cref="CatapultUse.Fires"/> — key zero is "no story attached" and
    /// shows <see cref="NothingHereDialog"/>; a real key reading zero shows the container's own
    /// dialog and stops.
    /// </remarks>
    public static bool Runs(int globalKey, int globalValue) => globalKey != 0 && globalValue != 0;

    /// <summary>
    /// Frames drawn with the effect flag <b>set</b>.
    /// </summary>
    /// <remarks>
    /// The handler raises a global render flag (<c>bool_dseg_2094</c>), redraws ten times, clears it,
    /// and redraws twice more. So the effect is a property of the WORLD for its duration rather than
    /// of the object — the opposite of the catapult, which steps its own item's sprite frame and
    /// touches nothing global. Two objects, two mechanisms; a shared "play the effect" helper fits
    /// neither.
    /// </remarks>
    public const int EffectFrames = 10;

    /// <summary>Frames drawn after the flag is cleared, to put the world back.</summary>
    /// <remarks>
    /// Without them the last frame the player sees is the one still carrying the effect.
    /// </remarks>
    public const int SettleFrames = 2;

    /// <summary>
    /// <b>The sound is unloaded only if it actually started.</b>
    /// </summary>
    /// <remarks>
    /// <c>audio_sound_play</c>'s return is kept and tested before <c>audio_unload_soundEffect</c>.
    /// Unloading unconditionally would release a bank the play never took.
    /// </remarks>
    public static bool UnloadsOnlyWhatItLoaded => true;

    /// <summary>
    /// <b>Any primary click disposes the container</b> — ran or not, exactly as the catapult does.
    /// </summary>
    /// <remarks>
    /// The dispose is on the tail every primary outcome reaches. Only the secondary-click examine
    /// returns without it.
    /// </remarks>
    public static bool PrimaryClickAlwaysDisposesTheContainer => true;
}
