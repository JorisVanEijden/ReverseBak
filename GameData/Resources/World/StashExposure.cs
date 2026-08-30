namespace GameData.Resources.World;

/// <summary>
/// Items left lying in the world get found and taken — <c>actor_maybeEmptyStashByExposure</c>
/// (canassa ACTSPAWN.C:265, IDA 0x5B148).
///
/// <para><b>A whole mechanic with no counterpart in the port until now.</b> A periodic check sets
/// <c>itemCount = 0</c> on a roll weighted by how exposed the spot is and how long the cache has
/// sat there. A player who never goes back never sees it happen, which is exactly why it can be
/// missing without anything looking broken.</para>
/// </summary>
/// <remarks>
/// <b>The design reads beautifully once the shape kinds are decoded.</b> Cover DIVIDES the risk and
/// traffic MULTIPLIES it, and both are gathered by sweeping the combat-zone entries around the
/// stash: cover is tree stumps, corn and two unnamed kinds; traffic is buildings, wells and way
/// markers — the places people actually go. So a cache dropped beside a building on a marked road
/// is the worst hiding place in the game and a hollow in the corn far from anywhere is the best.
///
/// <para><b>Nothing is ever stolen within a day of the last touch.</b> The elapsed term is integer
/// whole days, so it is 0 for the first 24 hours and the score with it. That is the single most
/// load-bearing line here and the easiest to lose to a float.</para>
/// </remarks>
public static class StashExposure {
    /// <summary>The score every check starts from.</summary>
    public const int BaseScore = 1000;

    /// <summary>The roll is <c>RND(10000)</c>, so the score is a probability in ten-thousandths.</summary>
    public const int RollRange = 10000;

    /// <summary>Game-time units in one day — the elapsed-time divisor.</summary>
    /// <remarks>
    /// <c>0xa8c0</c> = 43200. <c>GSTATE.C</c> gates its hourly update on
    /// <c>(game_time % 0xa8c0) / 0x708</c> changing, and 43200/1800 = 24 — so <c>0x708</c> is an
    /// hour, this is a day, and one unit is two seconds of game time.
    /// </remarks>
    public const int UnitsPerDay = 0xa8c0;

    /// <summary>Divisor applied when the proximity record's bit 2 is set.</summary>
    public const int FlaggedProximityDivisor = 0x32;

    /// <summary>The actor flag that exempts a stash outright.</summary>
    /// <remarks>
    /// <b>One flag, two protections.</b> The same 0x40 also resists slot eviction in
    /// <c>actor_allocEncounterSlotEvictingLru</c> — so "do not lose this actor" and "do not rob
    /// this actor" are the same bit, and clearing it for one purpose silently costs the other.
    /// </remarks>
    public const int ProtectedFlag = 0x40;

    /// <summary>The global event that empties every eligible stash regardless of the roll.</summary>
    public const int ForceEmptyEvent = 0xdc54;

    // ---------------------------------------------------------------- who is checked at all

    /// <summary>
    /// Whether the check returns before scoring anything.
    /// </summary>
    /// <param name="hasLastTouch">A LAST_TOUCH subrecord exists.</param>
    /// <param name="lastTouchTime">Its timestamp; zero means the stash has never been touched.</param>
    /// <param name="inCombat">A fight is running.</param>
    /// <param name="itemCount">How many items the actor holds.</param>
    /// <param name="actorFlags">The actor's flag word — see <see cref="ProtectedFlag"/>.</param>
    /// <remarks>
    /// <b>Four separate early returns, and they are not the same as scoring zero.</b> The
    /// difference matters because the zero-score cases below still run the whole sweep and can be
    /// emptied anyway by <see cref="ForceEmptyEvent"/>; these return before that test and so are
    /// immune to it too.
    ///
    /// <para><b>An untouched stash is exempt.</b> A zero timestamp is not "touched at the epoch" —
    /// it is the absence of a touch, and treating it as a very old one would rob every such cache
    /// on the first check.</para>
    /// </remarks>
    public static bool IsExempt(bool hasLastTouch, uint lastTouchTime, bool inCombat, int itemCount,
        int actorFlags) =>
        !hasLastTouch
        || lastTouchTime == 0
        || inCombat
        || itemCount == 0
        || (actorFlags & ProtectedFlag) != 0;

    // ---------------------------------------------------------------- the surroundings

    /// <summary>Shape kinds that give cover. Two of the five have no name in our world enum.</summary>
    /// <remarks>
    /// 30 is <see cref="WorldEntityType.TreeStump"/> and 18 is <see cref="WorldEntityType.Corn"/>;
    /// 5, 21 and 22 are not named there. Recorded as raw kinds rather than guessed at.
    /// </remarks>
    public static bool GivesCover(int shapeKind) =>
        shapeKind == 5 || shapeKind == 0x1e || shapeKind == 0x15 || shapeKind == 0x16
        || shapeKind == 0x12;

    /// <summary>Shape kinds that draw traffic: buildings, wells and way markers.</summary>
    public static bool DrawsTraffic(int shapeKind) =>
        shapeKind == (int)WorldEntityType.Building
        || shapeKind == (int)WorldEntityType.Well
        || shapeKind == (int)WorldEntityType.WayMarker;

    /// <summary>What one nearby entity adds to the cover weight.</summary>
    /// <remarks>
    /// <b>Both weights start at 1, not 0</b>, which is what keeps the division safe and means a
    /// stash with no cover at all is divided by one rather than being undefined.
    /// </remarks>
    public static int CoverContribution(int shapeKind, long distance) {
        if (!GivesCover(shapeKind) || distance >= 6000) {
            return 0;
        }
        return distance < 1000 ? 2 : 1;
    }

    /// <summary>What one nearby entity adds to the traffic weight.</summary>
    /// <remarks>
    /// <b>Traffic reaches five times further than cover and counts six times as hard.</b> A
    /// building 25,000 away still doubles the risk of a stash that has only one bush by it; the two
    /// scales are not comparable and averaging them would flatten the whole mechanic.
    /// </remarks>
    public static int TrafficContribution(int shapeKind, long distance) {
        if (!DrawsTraffic(shapeKind) || distance >= 30000) {
            return 0;
        }
        return distance < 15000 ? 12 : 6;
    }

    /// <summary>The starting value of both weights.</summary>
    public const int WeightBase = 1;

    // ---------------------------------------------------------------- the score

    /// <summary>
    /// The chance in ten-thousandths that this stash is emptied on this check.
    /// </summary>
    /// <param name="isEventState">The actor carries an EVENT_STATE subrecord.</param>
    /// <param name="residenceIsPartySlotOrCombat">Its residence is a party slot or a fight.</param>
    /// <param name="proximityIntensity">The PARAMS subrecord's intensity, or 0 when there is none.</param>
    /// <param name="hundredFlag">The PARAMS subrecord's hundred flag — see the remarks.</param>
    /// <param name="proximityFlagBit2">Bit 2 of the PARAMS subrecord's flags.</param>
    /// <param name="trafficWeight">Accumulated from <see cref="TrafficContribution"/>, starting at 1.</param>
    /// <param name="coverWeight">Accumulated from <see cref="CoverContribution"/>, starting at 1.</param>
    /// <param name="wholeDaysSinceTouched">
    /// <c>(now - lastTouch) / <see cref="UnitsPerDay"/></c>, in INTEGER arithmetic.
    /// </param>
    /// <remarks>
    /// <b>The hundred flag is where the two builds part company, and we target the CD one.</b> On
    /// the 1.02 CD build it sets the score to zero outright — an absolute exemption. The 1.00 floppy
    /// divides by 100 instead, a hundredfold reduction that still leaves the stash robbable. Taking
    /// the floppy branch would slowly empty caches the CD build protects completely.
    ///
    /// <para><b>Order is as written and the divisions truncate.</b> The intensity and flag divisions
    /// happen before the traffic/cover ratio, so a score already driven to 0 by them stays 0
    /// whatever the surroundings.</para>
    /// </remarks>
    public static long ScoreFor(bool isEventState, bool residenceIsPartySlotOrCombat,
        int proximityIntensity, bool hundredFlag, bool proximityFlagBit2,
        int trafficWeight, int coverWeight, long wholeDaysSinceTouched) {
        long score = BaseScore;
        if (isEventState || residenceIsPartySlotOrCombat) {
            score = 0;
        }

        if (proximityIntensity != 0) {
            score /= (proximityIntensity / 2) + 1;
        }
        if (hundredFlag) {
            // V102CD. The floppy's `score /= 100` is deliberately not ported — see the remarks.
            score = 0;
        }
        if (proximityFlagBit2) {
            score /= FlaggedProximityDivisor;
        }

        if (coverWeight <= 0) {
            coverWeight = WeightBase;
        }
        score = score * trafficWeight / coverWeight;
        return score * wholeDaysSinceTouched;
    }

    /// <summary>
    /// Whether a record's residence zeroes the score outright —
    /// <c>bResidence == RES_PARTY_SLOT || bResidence == RES_COMBAT</c>.
    /// </summary>
    /// <remarks>
    /// <b>Our container-type byte IS the DOS <c>bResidence</c></b>, so this is a direct test rather
    /// than a mapping — see <see cref="Data.SaveGameContainerType"/>, whose own summary records the
    /// correspondence. Spelled out here because the two exempt values wear domain names on our
    /// side: RES_PARTY_SLOT is <c>Inventory</c> and RES_COMBAT is <c>NpcInventory</c>. Both are
    /// somebody's carried inventory rather than a stash left in the world, which is why nothing can
    /// be pilfered from them.
    ///
    /// <para><b>Zeroing the score is not the same as being exempt.</b> A zero-scored record still
    /// runs the sweep and can be emptied by the force event — see <see cref="IsExempt"/> for the
    /// four early returns that are immune to it.</para>
    /// </remarks>
    public static bool ResidenceZeroesScore(Data.SaveGameContainerType residence) =>
        residence == Data.SaveGameContainerType.Inventory
        || residence == Data.SaveGameContainerType.NpcInventory;

    /// <summary>Whole days between a touch and now, truncating.</summary>
    /// <remarks>
    /// <b>Integer division, and that is the mechanic's safety catch.</b> Under one day the term is
    /// 0 and the whole score with it, so a cache is never robbed within 24 hours of being visited.
    /// A float here would start stealing from the first check.
    /// </remarks>
    public static long WholeDaysSince(uint now, uint lastTouchTime) =>
        now <= lastTouchTime ? 0 : (now - lastTouchTime) / UnitsPerDay;

    /// <summary>
    /// Whether this check empties the stash.
    /// </summary>
    /// <param name="roll">A roll in <c>[0, <see cref="RollRange"/>)</c>.</param>
    /// <param name="forceEmptyEventSet">Global event <see cref="ForceEmptyEvent"/> is set.</param>
    /// <remarks>
    /// <b>The event is an OR, not a gate.</b> With it set every non-exempt stash is emptied whatever
    /// its score — including the ones scored to zero by residence or the hundred flag, which are
    /// otherwise untouchable. Only the <see cref="IsExempt"/> cases escape it.
    /// </remarks>
    public static bool IsEmptied(long score, int roll, bool forceEmptyEventSet) =>
        roll < score || forceEmptyEventSet;
}
