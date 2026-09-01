namespace GameData.Resources.Data;

/// <summary>
/// The StateData scalar fields the writer authors into the save body (the subset we currently model).
/// As more of the body is modeled, more fields/blocks move here and passthrough shrinks.
/// </summary>
public readonly record struct SaveGameFields(
    short Chapter,
    int PartyGold,
    int GameTime,
    int TimeSnapshot,
    short PaletteEventMask,
    /// <summary>Non-zero once the whole active party is down; ends the world loop.</summary>
    byte PartyDeathState,

    /// <summary>1 when the world loop should exit into the next chapter.</summary>
    byte ChapterTransitionPending,

    /// <summary>The zone the party came FROM, used to detect a zone CHANGE.</summary>
    byte PreviousZone,

    byte CurrentZone,
    byte WorldX,
    byte WorldY,
    int PositionX,
    int PositionY,
    int PositionZ,

    short Rotation,

    /// <summary>
    /// The overhead map's camera height — <b>the player's map zoom</b>, canassa
    /// <c>lInsetCameraPosZ</c> at body offset 55.
    /// </summary>
    /// <remarks>
    /// <b>Not the same field as <see cref="PositionZ"/>, which is offset 29.</b> Both are a camera
    /// Z and TASK-5 conflated them, which nearly closed this as already authored. This one is used
    /// only by the map view: <c>MAP.C:474</c> takes the world camera down to it with a pitch of
    /// -90 degrees, and <c>ZONE.C:105</c> reseeds it from the zone default ONLY when the zone
    /// changes — the same condition our <c>WorldRuntime</c> arrived at independently for
    /// <c>GameSession.MapCameraZ</c>.
    ///
    /// <para>It is state rather than scratch: <c>SPELLFX.C</c> saves and restores it around an
    /// effect. Without it in the save the player's map zoom resets to the zone default on every
    /// load.</para>
    ///
    /// <para><b>NULLABLE, and that is not tidiness.</b> The writer clones the backing body and
    /// overwrites only what we model, so a field patched unconditionally is written even by callers
    /// that know nothing about it — here that would stamp a zero over the player's saved zoom on
    /// every write. <c>SaveGameWriterTests.WritingBackUnchangedFields_ProducesAByteIdenticalBody</c>
    /// caught exactly that, at position 55. Null means "not supplied, leave the body's own", the
    /// same contract <c>lastSeenStepSpeed</c> uses.</para>
    ///
    /// <para><b>Placed before <see cref="ActiveParty"/> deliberately.</b> The four fully-positional
    /// callers pass fifteen arguments and bind to the parameters above; every caller that supplies
    /// <c>ActiveParty</c> does so BY NAME. So this slots in with the other camera scalars, where it
    /// belongs, without rebinding anything — the hazard <c>ActiveParty</c>'s own remark describes.
    /// </para>
    /// </remarks>
    int? MapCameraZ = null,

    /// <summary>
    /// The active party's character indices, or null to leave the save's own untouched.
    /// </summary>
    /// <remarks>
    /// <b>Written back because the SESSION owns it</b> — party composition changes through a dialog
    /// action, and a change that is not written is lost on save. Nothing mutates it at runtime yet,
    /// so today this writes what the backing body already holds; it becomes load-bearing the moment
    /// that lands, and adding it after the fact is the kind of gap nobody looks for.
    ///
    /// <para>Null rather than an empty array means "not supplied". An empty array is a real party of
    /// nobody and would be written as such.</para>
    ///
    /// <para><b>LAST on purpose.</b> This is a positional record and several callers construct it
    /// positionally; a parameter added in the middle rebinds every one of them, which is exactly how
    /// SaveGameWriter.Write's optional-argument break happened earlier the same day.</para>
    /// </remarks>
    byte[] ActiveParty = null);
