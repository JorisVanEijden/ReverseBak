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
/// <b>CONSUMED since 2026-08-31.</b> <c>RiftMachineInteractionHandler</c> runs these rules on a real
/// click, and <c>InteractionProfileTable</c> carries the <c>("rift", empty profile)</c> row that
/// names the behaviour.
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
    /// The handler raises a global render flag (<c>g_bRiftMachineRunning</c>), redraws ten times,
    /// clears it, and redraws twice more. So the effect is a property of the WORLD for its duration
    /// rather than of the object — the opposite of the catapult, which steps its own item's frame
    /// and touches nothing global. Two objects, two mechanisms; a shared "play the effect" helper
    /// fits neither.
    ///
    /// <para><b>What the flag DOES, established 2026-08-31:</b>
    /// <c>worlditem_render_scrambled_while_rift_runs</c> @0x79a70 wraps every world item's draw.
    /// While the flag is set it points the item's render record at two RANDOM bytes —
    /// <see cref="ScrambleSlot0Range"/> and <see cref="ScrambleSlot1Range"/> — draws it, and puts
    /// the original back. Each mesh part reads the byte its <c>RuntimeFlagsIndex</c> names and
    /// reduces it modulo its own frame count, so the whole scene flickers through nearby frames.
    /// The machine itself never animates, which is why <c>gate2</c> carries no polygon region.</para>
    /// </remarks>
    public const int EffectFrames = 10;

    /// <summary>The scramble value written to render slot 0 — <c>rand() &amp; 3</c>, so 0..3.</summary>
    /// <remarks>
    /// Exclusive upper bound. The byte is reduced modulo each part's own frame count at draw time,
    /// so this caps which frames a scrambled object can reach: an eight-frame door flickers only
    /// through its first four.
    /// </remarks>
    public const int ScrambleSlot0Range = 4;

    /// <summary>The scramble value written to render slot 1 — <c>rand() % 5</c>, so 0..4.</summary>
    public const int ScrambleSlot1Range = 5;

    /// <summary>The sound the machine makes — <c>push 0x2e</c> at <c>handle_RiftMachine</c> +0x102.
    /// </summary>
    public const int RunSoundId = 46;

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
