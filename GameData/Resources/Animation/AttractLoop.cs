namespace GameData.Resources.Animation;

/// <summary>
/// The title-screen attract sequence — <c>gmain_play_intro_animation</c>, reached from the boot path
/// before the main menu.
/// </summary>
/// <remarks>
/// <b>It is a loop, not an intro.</b> Left alone the game cycles INTRO.ADS, then the credits, then a
/// short pause, and starts again; only input ends it. Treating it as "play the intro once and show
/// the menu" gets the common case right and the idle case wrong.
/// </remarks>
public static class AttractLoop {
    /// <summary>
    /// <b>Any key, or EITHER mouse button, ends the attract sequence.</b>
    /// </summary>
    /// <remarks>
    /// The animation loop polls the keyboard and then both buttons — left and right are checked
    /// separately and treated identically. There is no "press the correct key"; a right-click during
    /// the intro is as good as Enter. Every interruptible point in the sequence uses the same test.
    /// </remarks>
    public static bool EndsOnAnyKeyOrEitherMouseButton => true;

    /// <summary>The stages of one pass, in order.</summary>
    public enum Stage {
        /// <summary>INTRO.ADS.</summary>
        IntroAnimation,

        /// <summary>The credits roll, which can itself be interrupted.</summary>
        Credits,

        /// <summary>The pause before the next pass — interruptible like everything else.</summary>
        PauseBeforeRepeat,
    }

    /// <summary>What runs after a stage that was NOT interrupted, or null to leave the loop.</summary>
    /// <remarks>
    /// Interruption at ANY stage leaves for the menu; this only describes the idle path. The credits
    /// returning "interrupted" ends the sequence exactly as a keypress during the animation does,
    /// which is why the credits' return value has to be honoured rather than discarded.
    /// </remarks>
    public static Stage? Next(Stage completed) =>
        completed switch {
            Stage.IntroAnimation => Stage.Credits,
            Stage.Credits => Stage.PauseBeforeRepeat,
            _ => Stage.IntroAnimation,
        };

    /// <summary>
    /// The wait between the credits and the next pass: <b>140 timer ticks</b>.
    /// </summary>
    /// <remarks>
    /// <c>deadline = g_timer_ticks + 0x8C</c>, spun on while polling for input. Its absence does not
    /// break anything — it makes the attract loop restart immediately, so the title screen cycles
    /// faster than the original and never rests on the fade-out.
    ///
    /// <para>In wall-clock terms that is <b>about 2.37 seconds</b>: the timer rate is fixed and
    /// recovered (<see cref="GameTick"/>), not configuration-dependent as recorded here before.</para>
    /// </remarks>
    public const int PauseTicksBeforeRepeat = 0x8C;

    /// <summary>Each pass ends by fading to black before the next begins.</summary>
    /// <remarks>
    /// The fade is outside the interruptible section — it runs on the way round the loop whether the
    /// pass ended by itself or not, so leaving for the menu still goes through black rather than
    /// cutting from a half-lit credits roll.
    /// </remarks>
    public static bool EachPassEndsWithAFadeToBlack => true;
}
