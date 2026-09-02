namespace GameData.Resources.Scene;

using System.Collections.Generic;
using GameData.Resources.Animation;

/// <summary>
/// The noise a solved cipher puzzle makes — <c>UI_RunCipherPuzzle</c> @0x78c60.
/// </summary>
/// <remarks>
/// <b>Two bolts, not one sound.</b> On a solve the routine waits, plays <c>sound_hit</c>, runs a
/// bolt animation, waits again, plays <c>sound_hit</c> a second time and runs a second bolt. The
/// pauses are the point: the lock is heard giving way in two stages, and collapsing it to a single
/// cue on the solving click throws away the whole sequence.
///
/// <para><b>The waits are unequal and that is deliberate</b> — 150 ticks before the first and 60
/// before the second, so the mechanism starts slowly and finishes quickly.</para>
///
/// <para><b>The outcome is delivered after the bolts, not on the click.</b> The original does not
/// leave the screen until the sequence has played, so whatever the puzzle guards opens when the
/// second bolt lands.</para>
/// </remarks>
public static class CipherPuzzleSound {
    /// <summary><c>sound_hit</c> (4) — one bolt retracting.</summary>
    public const int BoltCue = 4;

    /// <summary>Ticks before the first bolt, counted from the solving click.</summary>
    public const int FirstBoltDelayTicks = 0x96;

    /// <summary>Ticks between the first bolt and the second.</summary>
    public const int SecondBoltDelayTicks = 0x3c;

    /// <summary>How many bolts the sequence has.</summary>
    public const int Bolts = 2;

    /// <summary>Each bolt's delay in seconds, in order, on the game clock.</summary>
    public static IEnumerable<double> BoltDelaysSeconds {
        get {
            yield return FirstBoltDelayTicks / GameTick.TicksPerSecond;
            yield return SecondBoltDelayTicks / GameTick.TicksPerSecond;
        }
    }
}
