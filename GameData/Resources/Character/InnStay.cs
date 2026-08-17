namespace GameData.Resources.Character;

using GameData.Money;

/// <summary>
/// A paid overnight stay at an inn — <c>UI_RestUntilTime</c> @0x4ff5c (ovr150).
/// </summary>
/// <remarks>
/// <b>Not the camp rest wearing a price tag.</b> Camping (<c>UI_Encamp</c> @0x703d0) runs until
/// every member is above a threshold and is free; an inn is bought by the night, runs for a fixed
/// number of hours the location itself states, and rests the party harder. The two are separate
/// functions and only share the dial artwork — <c>UI_RestUntilTime</c> loads ENCAMP.DAT too.
/// </remarks>
public static class InnStay {
    /// <summary>The nightmaster's offer — "do you want to stay the night?", a confirm.</summary>
    public const int OfferDialog = 1300082;

    /// <summary>Global the offer's text reads the price from.</summary>
    public const int PriceGlobal = 30014;

    /// <summary>Global the offer's text reads the length of the stay from.</summary>
    public const int HoursGlobal = 30015;

    /// <summary>
    /// Global that distinguishes the first offer from a repeat one.
    /// </summary>
    /// <remarks>
    /// 0 before any stay has completed, 1 afterwards — the original computes it with
    /// <c>neg/sbb/inc</c> off its "still resting" flag. It only selects the wording, so an inn
    /// asking a second time does not repeat the introduction.
    /// </remarks>
    public const int RepeatOfferGlobal = 30000;

    /// <summary>
    /// What a night costs, in ROYALS.
    /// </summary>
    /// <param name="costPerNight">
    /// The location's stored rate — <c>SaveGameContainerShopData.InnCostPerNight</c>.
    /// </param>
    /// <remarks>
    /// <b>The stored byte is a rate in SOVEREIGNS, and the field name does not say so.</b> The
    /// original multiplies it by ten before both printing it (0x501d3) and deducting it (0x5034c),
    /// and ten royals make a sovereign — so this is a unit conversion, not a scale factor. A port
    /// that spends the byte directly undercharges by a factor of ten and prints the wrong price
    /// with it.
    /// </remarks>
    public static int CostInRoyals(int costPerNight) =>
        costPerNight * MoneyFormatter.RoyalsPerSovereign;

    /// <summary>
    /// Whether the stay is over, given how long the party has been resting.
    /// </summary>
    /// <param name="hoursRested">Whole hours elapsed since the stay began.</param>
    /// <param name="innRestHours">
    /// The location's stated length — <c>SaveGameContainerShopData.InnRestHours</c>.
    /// </param>
    /// <remarks>
    /// <b>Exact equality, not "at least".</b> The original compares the two and only completes when
    /// they match, advancing an hour per pass; a <c>&gt;=</c> reading behaves the same while the
    /// loop is the only caller, and diverges the moment anything else can advance the clock
    /// mid-stay — the party would sail past the end and never be charged or healed.
    /// </remarks>
    public static bool StayComplete(int hoursRested, int innRestHours) =>
        hoursRested == innRestHours;

    /// <summary>Game-clock units in an hour, the divisor the elapsed count uses.</summary>
    public const int TicksPerHour = 1800;

    /// <summary>Whole hours in a tick count.</summary>
    public static int HoursFrom(long ticks) => (int)(ticks / TicksPerHour);

    /// <summary>
    /// How far an inn rests the party, as a percentage of each pool.
    /// </summary>
    /// <remarks>
    /// <b>A hundred, where camping passes eighty.</b> Both call the same clock-advance with a
    /// quality figure — camp passes 0x50 (80), the inn passes 0x64 (100) — which is the mechanical
    /// difference the price buys: you leave an inn whole, and a camp merely rested.
    /// </remarks>
    public const int RestedPercent = 100;
}
