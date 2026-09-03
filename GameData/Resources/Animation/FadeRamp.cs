namespace GameData.Resources.Animation;

using System;

/// <summary>
/// How a cutscene fade ramps — the data behind TTM opcodes <c>0x4114</c> (out) and <c>0x4124</c> (in).
/// </summary>
/// <remarks>
/// <b>The original stores a STEP, not a duration.</b> Its table is indexed by the command's speed
/// argument and gives the amount a counter moves each time round the loop; the loop walks a fixed
/// ramp and writes the palette at every position. So "how long does a fade take" is not in the data
/// at all — only how many palette writes it is made of.
///
/// <para>Both fade commands in the remake had this table inlined as a speed-to-frame-count switch,
/// twice over and identically. That is the drift risk this class exists to remove; it is also why
/// the numbers are re-derived here from the original's step table rather than copied across from the
/// C#.</para>
/// </remarks>
public static class FadeRamp {
    /// <summary>
    /// The step per iteration for each speed, straight out of the original's table.
    /// </summary>
    /// <remarks>
    /// <c>{0, 0x50, 0x14, 0x0A, 0x05, 0x02, 0x01}</c>. Note it runs the "wrong" way round — a bigger
    /// speed argument means a SMALLER step and therefore a longer, smoother fade.
    /// </remarks>
    public static readonly int[] StepTable = { 0, 80, 20, 10, 5, 2, 1 };

    /// <summary>The highest speed the table defines.</summary>
    public const int MaxSpeed = 6;

    /// <summary>The counter's full range: 0 to 640.</summary>
    /// <remarks>
    /// Fading out walks it down to zero, fading in walks it up. The intensity written is the counter
    /// divided by ten and clamped, so the ramp is ten counter units per intensity level.
    /// </remarks>
    public const int RampTop = 0x280;

    /// <summary>The counter units per intensity level — ten.</summary>
    public const int CounterPerIntensity = 10;

    /// <summary>VGA's full palette intensity, 63.</summary>
    public const int MaxIntensity = 0x3F;

    /// <summary>
    /// Palette writes in the WORLD's screen fade — <c>palette_fade_out</c> (PALETTE.C:83).
    /// </summary>
    /// <remarks>
    /// <b>A different mechanism from the speed table above, and worth keeping apart.</b> The
    /// cutscene ramp counts in <see cref="CounterPerIntensity" /> units and picks its step from
    /// <see cref="StepTable" />; the world fade steps the INTENSITY itself:
    /// <code>
    /// if (step == -1) step = 2;                                  // every shipped caller passes -1
    /// for (intensity = 0x3f; 0 &lt; intensity; intensity -= step)   // 63, 61, ... 1  -> 32 writes
    ///     palette_set_scaled(off, seg, 0, intensity);
    /// palette_set_scaled(off, seg, 0, 0);                        // + the final black -> 33
    /// </code>
    ///
    /// <para><b>The count is recoverable; the DURATION is not.</b> The loop's
    /// <c>wait_each_step</c> is 0 at every shipped call site, so no frame is presented per step and
    /// the pace is whatever the DAC writes cost on the hardware — and
    /// <c>palette_set_scaled</c> is assembly, so it is not in the reconstructed C to read. Feed this
    /// to <c>CutsceneTiming.FadeDurationSeconds</c>, whose factor is the project's one tuned knob for
    /// exactly this gap, rather than inventing a second timing.</para>
    /// </remarks>
    public const int WorldFadePaletteWrites = (MaxIntensity / WorldFadeStep) + 1;

    /// <summary>The world fade's intensity step — the default <c>palette_fade_out</c> substitutes
    /// for the -1 every shipped caller passes.</summary>
    public const int WorldFadeStep = 2;

    /// <summary>The step for a speed.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The speed is outside the table.</exception>
    public static int StepFor(int speed) =>
        speed >= 0 && speed <= MaxSpeed
            ? StepTable[speed]
            : throw new ArgumentOutOfRangeException(nameof(speed));

    /// <summary>
    /// <b>Speed 0 is instant, and it is a different code path — not a very fast ramp.</b>
    /// </summary>
    /// <remarks>
    /// A step of zero would never terminate the loop, so the original branches before it and writes
    /// the destination intensity once: 0 for a fade out, full for a fade in. A port that treats
    /// speed 0 as "ramp with step 0" hangs, and one that treats it as "ramp very fast" shows a
    /// flicker where the original shows a cut.
    /// </remarks>
    public static bool IsInstant(int speed) => StepFor(speed) == 0;

    /// <summary>How many palette writes the ramp is made of, or 0 when instant.</summary>
    /// <remarks>
    /// <see cref="RampTop"/> divided by the step: 8, 32, 64, 128, 320 and 640 for speeds 1 to 6.
    /// <b>This is a count of writes, not of frames.</b> The loop has no wait in it — it does not
    /// present a frame per iteration the way <c>palette_fade_out</c> does elsewhere — so on the
    /// original the wall-clock duration fell out of how fast the machine could write the DAC. Any
    /// mapping onto a fixed frame clock is therefore a CALIBRATION, not a fact recoverable from the
    /// data, and belongs with the other tuned UI constants rather than here.
    /// </remarks>
    public static int PaletteWrites(int speed) {
        int step = StepFor(speed);

        return step == 0 ? 0 : RampTop / step;
    }

    /// <summary>The intensity written at a counter position: <c>counter / 10</c>, clamped to 0..63.</summary>
    /// <remarks>
    /// The clamp is load-bearing at the top of the ramp: 640 / 10 is 64, one past VGA's maximum, so
    /// the first write of a fade-in — and the last of a fade-out — would be out of range without it.
    /// </remarks>
    public static int IntensityAt(int counter) {
        int intensity = counter / CounterPerIntensity;
        if (intensity < 0) {
            return 0;
        }

        return intensity > MaxIntensity ? MaxIntensity : intensity;
    }
}
