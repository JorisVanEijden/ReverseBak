namespace GameData.Resources.Config;

/// <summary>
/// Engine-independent view of MOVEMENT.DAT — the table that turns the player's
/// movement-speed / turn-speed <see cref="Preferences"/> into the concrete
/// scalars the world-walk loop uses.
///
/// The file is 18 bytes = three arrays of three little-endian u16 (one entry per
/// speed preset, indexed Small/Medium/Large = 0/1/2):
///   bytes  0..5   StepDistances  — forward distance moved per step, in game units
///   bytes  6..11  TurnAngles     — heading rotation applied per discrete turn
///   bytes 12..17  raw "turn size 2" — game-time advanced per step (see below)
///
/// Reversed from <c>Load_movement.dat</c> (ovr184 @ 0x71790), which reads one
/// entry from each array and stows it in a global:
///   * StepDistances[Preferences.StepSize]  → forward step (MoveParty/AdvancePosition)
///   * TurnAngles[Preferences.TurnSize]      → heading delta (TurnParty_impl, added
///     to the camera entity heading at +0x12)
///   * "turn size 2"[Preferences.StepSize]   → NOT a turn at all. The loader does
///     <c>value * 30</c> and the main loop passes the result to the game-clock
///     advance (sub_ovr131_8E7: <c>GameTimeIn2Seconds += value</c>, with hourly
///     party-healing rollover). So it is the amount of game time that elapses per
///     forward step. Note it is indexed by the *step-size* preference, not the
///     turn preference — time-per-step scales with distance-per-step.
///
/// Domain-neutral conversion done by the extractor: "turn size 2" is exposed as
/// <see cref="SecondsPerStep"/> in real game-seconds (raw * 30 ticks * 2 s/tick =
/// raw * 60). Underground the main loop quarters this value at runtime; that is a
/// movement-system concern and is not baked into the table.
/// </summary>
public class MovementData : IResource {
    public const int PresetCount = 3;

    public MovementData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>Forward distance per step in game units (1600 = one sub-tile), indexed by <see cref="StepSize"/>.</summary>
    public int[] StepDistances { get; set; } = new int[PresetCount];

    /// <summary>
    /// Heading rotation applied per discrete turn, in the engine's 16-bit angle
    /// units (a full revolution is 0x10000; all three shipped values divide it
    /// evenly → 5.625° / 11.25° / 22.5°). Indexed by <see cref="TurnSize"/>.
    /// </summary>
    public int[] TurnAngles { get; set; } = new int[PresetCount];

    /// <summary>Game-time elapsed per forward step, in real seconds. Indexed by <see cref="StepSize"/>.</summary>
    public int[] SecondsPerStep { get; set; } = new int[PresetCount];

    public int StepDistanceFor(StepSize s) => StepDistances[(int)s];
    public int TurnAngleFor(TurnSize t) => TurnAngles[(int)t];
    public int SecondsPerStepFor(StepSize s) => SecondsPerStep[(int)s];
}
