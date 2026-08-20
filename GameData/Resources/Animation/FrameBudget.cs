namespace GameData.Resources.Animation;

/// <summary>
/// How long a cutscene frame occupies the screen — the scheduling shape behind the TTM delay
/// opcode (<c>0x1021</c>) and the animation scheduler that consumes it.
/// </summary>
/// <remarks>
/// <b>A frame's duration is a DEADLINE, not a sleep.</b> The delay opcode sets the script node's
/// interval, and the scheduler writes <c>expiry = now + interval</c> at the moment the frame's
/// commands have run. The frame is then due when the tick count reaches that expiry, so whatever the
/// frame spent drawing has already come out of its own budget. A port that runs the commands and
/// *then* sleeps for the full interval makes every frame longer than the script asked, and makes
/// heavy frames drift further than light ones.
/// </remarks>
public static class FrameBudget {
    /// <summary>How long is left to wait once the frame's own work is done.</summary>
    /// <param name="budget">The frame's allotted time.</param>
    /// <param name="alreadySpent">Time the frame's commands took.</param>
    /// <remarks>
    /// Never negative: a frame that overran its budget is already late, and the scheduler's answer
    /// is to move on rather than to try to claw the time back from the next one. Overrun is not
    /// carried forward.
    /// </remarks>
    public static double RemainingWait(double budget, double alreadySpent) {
        double remaining = budget - alreadySpent;

        return remaining > 0 ? remaining : 0;
    }

    /// <summary>Whether the frame overran and should simply yield.</summary>
    public static bool Overran(double budget, double alreadySpent) =>
        RemainingWait(budget, alreadySpent) <= 0;

    /// <summary>
    /// <b>Frame duration and palette cycling run off ONE clock in the original.</b>
    /// </summary>
    /// <remarks>
    /// The scheduler compares a frame's expiry against <c>g_dwSysTickCount</c>, and the palette
    /// cycling that shimmers during a hold advances on that same counter — there is one timer and
    /// everything animated hangs off it. So whatever rate a port picks, the two must be the SAME
    /// rate: if frames run on one clock and colour cycling on another, a shimmer that should take
    /// exactly one frame drifts against the frame it belongs to, and the error grows with the length
    /// of the hold.
    ///
    /// <para><b>This is currently violated in the remake</b> — the frame processor holds frames
    /// against a 60Hz assumption and steps palette cycles at 18.2Hz. Both numbers are ours: the
    /// original's tick is whatever <c>timer_install</c> programmed, which the sound init sets and
    /// which therefore is not even fixed across configurations. Recorded as a rule here so the
    /// mismatch is visible; picking the rate is a calibration and belongs with the tuned constants.</para>
    /// </remarks>
    public static bool FramesAndPaletteCyclesShareOneClock => true;
}
