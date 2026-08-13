namespace GameData.Resources.GameState;

using System;

/// <summary>
/// The game's wall clock. Time is stored in <b>two-second units</b> throughout the save game, so
/// every "what hour is it" question routes through here rather than re-deriving the constants.
///
/// <para>The original writes the same derivation two ways. <c>SaveGameSection0</c>'s flag readers
/// go via seconds (<c>time * 2 / 3600 % 24</c>); <c>cspell_check_castable</c> does it in raw units
/// as <c>(time % 0xa8c0) / 0x708</c>. Those agree exactly — 0xa8c0 is 43200 two-second units (a
/// 24-hour day) and 0x708 is 1800 of them (one hour) — which is the cross-check that the units are
/// really two seconds and not one.</para>
/// </summary>
public static class GameTime {
    /// <summary>Two-second units in one hour (<c>0x708</c>).</summary>
    public const int UnitsPerHour = 1800;

    /// <summary>Hours in a day.</summary>
    public const int HoursPerDay = 24;

    /// <summary>Two-second units in one day (<c>0xa8c0</c>). The engine's <c>time_1_day</c>.</summary>
    public const int UnitsPerDay = UnitsPerHour * HoursPerDay;

    /// <summary>Hour of day, 0..23, from a game time in two-second units.</summary>
    public static int HourOfDay(long gameTimeIn2Seconds) =>
        (int)((Math.Max(0, gameTimeIn2Seconds) % UnitsPerDay) / UnitsPerHour);

    /// <summary>Whole days elapsed since time zero.</summary>
    public static int DayOf(long gameTimeIn2Seconds) =>
        (int)(Math.Max(0, gameTimeIn2Seconds) / UnitsPerDay);
}
