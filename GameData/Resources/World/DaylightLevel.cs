namespace GameData.Resources.World;

using GameData.Resources.GameState;

/// <summary>
/// How bright the world is at a given moment — IDA <c>GetTimeOfDayLightIntensity</c>
/// (seg031 @0x2d0bb). The daylight term the palette blend starts from; the special light sources
/// layer on top of it.
///
/// <para>Two flat plateaus with a ramp between them, and the ramps are <b>not the same length</b>:
/// dawn takes four hours and dusk three.</para>
/// </summary>
public static class DaylightLevel {
    /// <summary>Level through the night — the floor, never fully dark.</summary>
    public const int Night = 15;

    /// <summary>Level through the middle of the day.</summary>
    public const int Day = 64;

    /// <summary>The whole swing between them, which both ramps traverse.</summary>
    public const int Swing = Day - Night;

    /// <summary>Dawn begins; the level starts climbing from <see cref="Night"/>.</summary>
    public const int DawnStartHour = 4;

    /// <summary>Full daylight from here.</summary>
    public const int FullDayHour = 8;

    /// <summary>Dusk begins; the level starts falling from <see cref="Day"/>.</summary>
    public const int DuskStartHour = 17;

    /// <summary>Full night from here.</summary>
    public const int FullNightHour = 20;

    /// <summary>Length of the dawn ramp, in game-time units.</summary>
    public const int DawnUnits = (FullDayHour - DawnStartHour) * GameTime.UnitsPerHour;

    /// <summary>Length of the dusk ramp, in game-time units.</summary>
    public const int DuskUnits = (FullNightHour - DuskStartHour) * GameTime.UnitsPerHour;

    /// <summary>
    /// The daylight level for a moment.
    /// </summary>
    /// <param name="gameTimeIn2Seconds">The game clock, in its two-second unit.</param>
    /// <remarks>
    /// <b>Both ramps land exactly on the plateaus they join</b>, so the curve is continuous: dawn
    /// reads <see cref="Night"/> at 04:00 and <see cref="Day"/> at 08:00, dusk the reverse. That is
    /// worth knowing because of <i>how</i> the original gets there — see the remark below — and a
    /// port that lands a step off would show a visible jump twice a day.
    ///
    /// <para><b>The original does the rebasing by 16-bit overflow.</b> It adds a magic constant to
    /// the time-of-day remainder in a 16-bit register, and that constant is simply
    /// <c>65536 − rampStart</c> — so the add wraps and the result is the offset into the ramp. In
    /// wider arithmetic the same constants produce numbers roughly nine times too large; this port
    /// subtracts instead, which is what the wrap was for.</para>
    /// </remarks>
    public static int At(long gameTimeIn2Seconds) {
        int hour = GameTime.HourOfDay(gameTimeIn2Seconds);
        int intoDay = (int)(((gameTimeIn2Seconds % GameTime.UnitsPerDay) + GameTime.UnitsPerDay)
            % GameTime.UnitsPerDay);

        if (hour >= FullDayHour && hour < DuskStartHour) {
            return Day;
        }
        if (hour < DawnStartHour || hour >= FullNightHour) {
            return Night;
        }
        if (hour < DuskStartHour) {
            return Night + (((intoDay - (DawnStartHour * GameTime.UnitsPerHour)) * Swing) / DawnUnits);
        }

        return Day - (((intoDay - (DuskStartHour * GameTime.UnitsPerHour)) * Swing) / DuskUnits);
    }

    /// <summary>Whether the world is at its brightest.</summary>
    public static bool IsFullDaylight(long gameTimeIn2Seconds) => At(gameTimeIn2Seconds) == Day;

    /// <summary>Whether the world is at its darkest.</summary>
    public static bool IsFullNight(long gameTimeIn2Seconds) => At(gameTimeIn2Seconds) == Night;
}
