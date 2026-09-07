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
///
/// <para><b>Each bolt is a SPRITE, not just a noise</b> — see
/// <see cref="CipherPuzzleLayout.LatchOriginsVga"/>. The cue and the blit are one event, so a port
/// that plays the sound without swapping the latch art leaves the chest looking shut while it is
/// heard opening.</para>
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

    /// <summary>
    /// Ticks the screen holds after the second bolt, before the closing line.
    /// </summary>
    /// <remarks>
    /// <b>CIPHER.C:200-205 waits 200 ticks between the last blit and
    /// <see cref="CipherPuzzleLayout.SolvedDialog"/>.</b> It is the longest of the three, and it is
    /// what lets the player see both latches open before anyone speaks — running the line straight
    /// off the second bolt puts narration over a lock that is still visibly moving.
    /// </remarks>
    public const int AfterBoltsDelayTicks = 0xc8;

    /// <summary>That hold, in seconds.</summary>
    public static double AfterBoltsSeconds => AfterBoltsDelayTicks / GameTick.TicksPerSecond;

    /// <summary>Each bolt's delay in seconds, in order, on the game clock.</summary>
    public static IEnumerable<double> BoltDelaysSeconds {
        get {
            yield return FirstBoltDelayTicks / GameTick.TicksPerSecond;
            yield return SecondBoltDelayTicks / GameTick.TicksPerSecond;
        }
    }
}
