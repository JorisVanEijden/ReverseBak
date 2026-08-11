namespace GameData.Resources.Config;

/// <summary>
/// How long a dialog panel stays up before dismissing itself, from the player's
/// <see cref="TextSpeed"/> preference — <c>dialog_input_wait_timed</c> (DIALOG.C:224-240).
/// </summary>
public static class DialogTextSpeed {
    /// <summary>
    /// <c>g_abTextSpeedScale</c>: the per-setting percentage applied to the base reading time.
    /// </summary>
    private static readonly int[] ScalePercent = { 100, 75, 50 };

    /// <summary>
    /// The engine's timer runs at <b>~236.7 Hz</b>, not the 18.2 Hz BIOS default:
    /// <c>InitializeGameHardware</c> calls <c>InitializeTimers(13)</c> (KRONDOR.EXE 0x41a20), which
    /// programs the PIT with a count of <c>0xFFFF / 13</c> = 5041, i.e. 1193182 / 5041. Every
    /// duration below is in those ticks, so this is what turns them into seconds.
    /// </summary>
    public const double TicksPerSecond = 1193182.0 / (0xFFFF / 13);

    /// <summary>
    /// Reading time for <paramref name="characters"/> of text, in engine ticks:
    /// <c>(chars * 12 + 150) * scale / 100</c>. The 150 is a floor so a two-word line still
    /// registers.
    /// </summary>
    public static long AutoDismissTicks(int characters, TextSpeed speed) {
        if (characters < 0) {
            characters = 0;
        }
        int scale = ScalePercent[(int)speed >= 0 && (int)speed < ScalePercent.Length ? (int)speed : 0];
        return ((long)characters * 12 + 150) * scale / 100;
    }

    /// <summary>
    /// How long the panel should wait before dismissing itself, or <c>null</c> at
    /// <see cref="TextSpeed.Slow"/> — which is not "slow" so much as "wait for the player".
    /// The engine sets an infinite deadline there and only ever leaves on input
    /// (DIALOG.C:227-231).
    ///
    /// <para>At the other two settings a 100-character line holds for about 4.3 s (Medium) or
    /// 2.9 s (Fast); input always cuts it short.</para>
    ///
    /// <para>Not modelled: the engine's flag-8 dialogs, which use the timeout even at Slow and
    /// divide it by <c>chapter / 5 + 2</c> so the same text gets briefer as the story runs on.</para>
    /// </summary>
    public static double? AutoDismissSeconds(int characters, TextSpeed speed) {
        if (speed == TextSpeed.Slow) {
            return null;
        }
        return AutoDismissTicks(characters, speed) / TicksPerSecond;
    }
}
