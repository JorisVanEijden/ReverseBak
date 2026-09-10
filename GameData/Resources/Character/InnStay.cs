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
    /// Whether the stay is over — <b>the clock has reached the inn's waking hour</b>.
    /// </summary>
    /// <param name="hourOfDay">The hour the game clock currently reads, 0–23.</param>
    /// <param name="innWakeHour">
    /// <c>SaveGameContainerShopData.InnRestHours</c>, which despite its name is an HOUR OF DAY.
    /// </param>
    /// <remarks>
    /// <b>The stored byte is when you are woken, not how long you sleep, and the field name says
    /// the opposite.</b> The original divides the clock by a day, then the remainder by an hour, and
    /// compares THAT against the byte (0x50310–0x5033c) — so it is comparing an hour of day, and the
    /// function is called <c>UI_RestUntilTime</c> for a reason. The shipped inns store 5, 6 and 7:
    /// they wake you at dawn. Read as a duration, a night booked at 8pm ends at 1am after five
    /// hours instead of at 5am after nine.
    ///
    /// <para>Exact equality, not "at least" — the loop advances exactly one hour per pass, so it
    /// cannot step over the target. A <c>&gt;=</c> reading would behave identically here and break
    /// the moment the clock wraps past midnight, which every one of these stays does.</para>
    /// </remarks>
    public static bool StayComplete(int hourOfDay, int innWakeHour) =>
        hourOfDay == innWakeHour;

    /// <summary>Game-clock units in an hour.</summary>
    public const int TicksPerHour = 1800;

    /// <summary>Game-clock units in a day.</summary>
    public const int TicksPerDay = 24 * TicksPerHour;

    /// <summary>The hour of day (0–23) a clock reading falls in — what <see cref="StayComplete"/>
    /// tests.</summary>
    public static int HourOfDay(long ticks) =>
        (int)(((ticks % TicksPerDay) + TicksPerDay) % TicksPerDay / TicksPerHour);

    /// <summary>
    /// How many hours a stay actually runs, given the hour it is booked at.
    /// </summary>
    /// <param name="startHourOfDay">The hour the clock reads when the offer is accepted, 0–23.</param>
    /// <param name="innWakeHour">The inn's waking hour — <c>InnRestHours</c>.</param>
    /// <remarks>
    /// <b>Never fewer than two hours, even when the waking hour is minutes away.</b> The original
    /// advances the clock once on accepting (0x50219) and again at the top of each pass before it
    /// compares (0x502d5, 0x50310), so the earliest the target can be seen is two hours in — and a
    /// stay booked AT the waking hour, or an hour before it, therefore runs a full extra day rather
    /// than ending immediately.
    ///
    /// <para>Expressed as arithmetic rather than left implicit in a loop because that off-by-one is
    /// the entire difference between selling a night and selling a doze, and a
    /// test-before-advance loop gets it silently wrong.</para>
    /// </remarks>
    public static int HoursOfStay(int startHourOfDay, int innWakeHour) {
        const int hoursPerDay = 24;
        int difference = (((innWakeHour - startHourOfDay) % hoursPerDay) + hoursPerDay) % hoursPerDay;

        return difference >= 2 ? difference : difference + hoursPerDay;
    }

    /// <summary>
    /// The rest quality an inn's hourly tick is given.    /// <summary>
    /// The rest quality an inn's hourly tick is given.
    /// </summary>
    /// <remarks>
    /// <b>133, and it is not a percentage of anything the player sees.</b> It is the figure
    /// <c>gstate_hourly_tick</c> takes as its rest argument (0x5021e pushes 0x85; camping pushes
    /// 0x64 at 0x7061a), and that function does two things with it: it picks the pool ceiling —
    /// <see cref="UpkeepEngine.PartialRestQuality"/> exactly means the 80% cap, ANY other non-zero
    /// value means 100% — and it scales the hour's regeneration by <c>quality / 100</c>.
    ///
    /// <para>So the price buys BOTH halves: an inn fills the pools where camping stops at 80%, and
    /// it refills them a third faster per hour. Passing 100 here would silently buy neither.</para>
    /// </remarks>
    public const int RestQuality = 133;

    /// <summary>
    /// How far an inn rests the party, as a percentage of each pool — the RESULT of
    /// <see cref="RestQuality"/>, not a figure the original passes anywhere.
    /// </summary>
    public const int RestedPercent = 100;

    /// <summary>
    /// Whether the nightmaster offers again once a stay has finished.
    /// </summary>
    /// <remarks>
    /// <b>An inn keeps selling nights until the party is whole.</b> After a completed stay the
    /// original walks the party comparing each member's effective HealthStaminaCombo with their
    /// maximum (0x5035f) and loops back to the offer unless every one of them is full. So a badly
    /// hurt party is asked to buy a second and third night, with
    /// <see cref="RepeatOfferGlobal"/> set so the wording does not re-introduce the innkeeper.
    /// </remarks>
    public static bool OfferAnotherNight(bool everyMemberAtFullPool) => !everyMemberAtFullPool;

    /// <summary>
    /// <b>Payment happens AFTER the night, and no C-level code checks the party can afford it —
    /// because the DIALOG has already refused them.</b>
    /// </summary>
    /// <remarks>
    /// The deduction is at 0x50353, past the loop and past the completion test, so a stay that is
    /// somehow interrupted is free and gold is only ever spent on a night actually slept.
    ///
    /// <para><b>This remark used to stop at "there is no balance check anywhere on the path" and
    /// warn that adding one would make a dialog branch dead. Both halves were wrong, and they cost
    /// a wrong "faithful" verdict on 2026-09-10.</b> The check is real and it lives in the dialog
    /// graph, two hops below the offer: <see cref="OfferDialog"/> routes on Var 0 to the
    /// nightmaster's line, whose YES branch lands on <c>base:ddx:dial_z13:16238</c> — a text-less
    /// router that tests <b>Var 3</b> (global 30003, <c>party gold &gt;= the quoted price</c>) and
    /// falls through to <see cref="RefusedDialogKey"/>, where the innkeeper turns a pauper away and
    /// the stay never happens.</para>
    ///
    /// <para>So the port must not deduct unconditionally. Reading only the top-level record's
    /// branches finds a Var 0 test and no Var 3, which is exactly the false negative that produced
    /// the original mistake — the gate is on the ACCEPT path, not on the offer.</para>
    /// </remarks>
    public const bool ChargedAfterTheStay = true;

    /// <summary>
    /// The condition the offer's accept branch tests — global 30003, <c>Var 3</c>.
    /// </summary>
    /// <remarks>
    /// <c>Min = 1</c> on that branch, and 30003 is computed as <c>PartyGold &gt;= price</c>, so a
    /// party holding exactly the asking price CAN stay. Both figures are in royals.
    /// </remarks>
    public static bool CanAfford(int partyGoldRoyals, int priceRoyals) =>
        partyGoldRoyals >= priceRoyals;

    /// <summary>The nightmaster refusing a party that cannot pay — terminal, so nothing is charged.</summary>
    /// <remarks>
    /// "The innkeeper frowned. 'Let me guess,' he said, reading @4's distressed face as he searched
    /// his pack for the money…". It carries no branches, so the conversation ends there.
    /// </remarks>
    public const string RefusedDialogKey = "base:ddx:dial_z13:16267";
}
