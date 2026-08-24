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
