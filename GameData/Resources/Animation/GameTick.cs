namespace GameData.Resources.Animation;

/// <summary>
/// The engine's one animation clock — the counter that every timed thing in the game polls.
/// </summary>
/// <remarks>
/// <b>The rate is RECOVERED, not chosen.</b> It had been recorded in several places that the tick
/// rate was unknowable ("whatever <c>timer_install</c> programmed, which the sound init sets and
/// which is therefore not even fixed across configurations"), which made every timing question a
/// calibration. That is wrong on both halves, and the whole chain is in the sources:
///
/// <list type="number">
/// <item><description>
/// <b>It is BOOT that installs the timer, not sound.</b> The hardware init calls
/// <c>timer_install(0xd)</c> unconditionally. The sound init's identical call is guarded on
/// "not already installed", so by the time audio starts it is a no-op — and it passes the same
/// <c>0xd</c> anyway. There is no configuration in which the rate differs.
/// </description></item>
/// <item><description>
/// <b>The argument is not a frequency in Hz.</b> Despite the parameter's name it is a MULTIPLE of
/// the 18.2 Hz BIOS tick: the routine computes <c>divisor = 0xffff / argument</c> and programs PIT
/// channel 0 with it, so <c>0xd</c> means "13 times the BIOS rate". The same argument is stored as
/// the BIOS chain reload, and the ISR passes every 13th tick through to the original handler so the
/// DOS time-of-day keeps running — which is the tell that 13 is a ratio and not a frequency.
/// </description></item>
/// <item><description>
/// <b>The animation counter is a DIVIDED tick, not the IRQ.</b> IRQ0 does not advance the counter
/// directly; boot registers the counter's increment as a timer callback with an interval of 4, so
/// it advances once every fourth interrupt.
/// </description></item>
/// </list>
///
/// <para>That lands at ~59.17 Hz, which is presumably the point: dividing by 4 turns the 13x BIOS
/// rate into something within one and a half percent of 60 Hz. So the remake's long-standing 60 Hz
/// assumption was a good guess — but only for frames. Anything expressed in ticks (a hold, a pause,
/// an interval) should come through <see cref="Seconds"/> so there is exactly one clock.</para>
/// </remarks>
public static class GameTick {
    /// <summary>
    /// The 8253's input clock. Not a game constant — it is the PC's, at 14.31818 MHz over 12.
    /// </summary>
    public const double PitInputHz = 1193181.6666666667;

    /// <summary>
    /// <c>timer_install</c>'s argument: how many times faster than the BIOS tick to run.
    /// </summary>
    public const int BiosTickMultiple = 0xd;

    /// <summary>
    /// What actually reaches the chip — <c>0xffff / multiple</c>, truncating, exactly as the 16-bit
    /// divide does.
    /// </summary>
    /// <remarks>
    /// The truncation matters at this size: the exact ratio would be 5041.15, so reproducing this in
    /// floating point and rounding gives a different chip rate than the game ever ran at.
    /// </remarks>
    public const int PitDivisor = 0xffff / BiosTickMultiple;

    /// <summary>How many interrupts pass per counter increment — the registered callback interval.</summary>
    public const int Irq0sPerTick = 4;

    /// <summary>Interrupts per second, before the divide.</summary>
    public static double Irq0Hz => PitInputHz / PitDivisor;

    /// <summary>The animation clock's rate — about 59.17 Hz.</summary>
    public static double TicksPerSecond => Irq0Hz / Irq0sPerTick;

    /// <summary>One tick, in seconds — about 16.9 ms.</summary>
    public static double SecondsPerTick => 1.0 / TicksPerSecond;

    /// <summary>How long a count of ticks lasts.</summary>
    /// <remarks>
    /// The unit of every interval the scripts carry: a frame's hold, the attract loop's pause
    /// between passes, a scheduler node's interval. Going through here keeps them commensurable —
    /// two durations measured off one clock cannot drift against each other, which is the whole
    /// reason the original has only one.
    /// </remarks>
    public static double Seconds(double ticks) => ticks * SecondsPerTick;
}
