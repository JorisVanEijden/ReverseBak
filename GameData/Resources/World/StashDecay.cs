namespace GameData.Resources.World;

using System.Collections.Generic;
using GameData.Resources.Data;
using GameData.Resources.Inventory;

/// <summary>
/// The whole stash-exposure decision for one container, composed —
/// <c>actor_maybeEmptyStashByExposure</c> (0x5B148).
///
/// <para><b>Composed here rather than in the caller so the ORDER cannot drift.</b> The four early
/// returns come first and are immune to the force event; the zero-score cases are not. Splitting
/// that across a service and a model is how the two get muddled.</para>
/// </summary>
public static class StashDecay {
    /// <summary>What the sweep decided about one container.</summary>
    public readonly struct Verdict {
        public Verdict(bool exempt, long score, bool empties) {
            Exempt = exempt;
            Score = score;
            Empties = empties;
        }

        /// <summary>Returned before the roll, and immune to the force event.</summary>
        public bool Exempt { get; }

        /// <summary>Chance in ten-thousandths, before the roll.</summary>
        public long Score { get; }

        /// <summary>The stash is emptied.</summary>
        public bool Empties { get; }
    }

    /// <summary>
    /// Decide one container against its surroundings.
    /// </summary>
    /// <param name="container">The live container. Its type IS the actor's <c>bResidence</c>.</param>
    /// <param name="now">Game time, in the 2-second units the save uses.</param>
    /// <param name="inCombat">Nothing is pilfered mid-fight.</param>
    /// <param name="surroundings">Every loaded placement — see <see cref="StashExposure.AccumulateWeights"/>.</param>
    /// <param name="roll">A d10000.</param>
    /// <param name="forceEmptyEventSet">Global event 0xdc54.</param>
    /// <remarks>
    /// <b>The weights are only accumulated when they can matter.</b> The sweep is over every
    /// placement in the zone and runs per container; doing it after the exemptions and after a
    /// zero score keeps a day boundary from walking the whole world once per bag for no reason.
    /// That is an optimisation, not a rule — the original sweeps first — and it is safe only
    /// because a zero score stays zero however the weights come out.
    /// </remarks>
    public static Verdict Decide(RuntimeContainer container, uint now, bool inCombat,
        IEnumerable<StashExposure.NearbyEntity> surroundings, int roll, bool forceEmptyEventSet) {
        if (container == null) {
            return new Verdict(exempt: true, score: 0, empties: false);
        }

        bool hasLastTouch = (container.DataTypes & SaveGameContainerDataType.Timestamp) != 0
            && container.Timestamp.HasValue;
        if (StashExposure.IsExempt(hasLastTouch, (uint)(container.Timestamp ?? 0), inCombat,
                container.Items.Count, (int)container.DataTypes)) {
            return new Verdict(exempt: true, score: 0, empties: false);
        }

        long days = StashExposure.WholeDaysSince(now, (uint)container.Timestamp.Value);
        bool zeroed = container.IsShop
            || StashExposure.ResidenceZeroesScore(container.ContainerType)
            || (container.Params?.ProximityHundredFlag ?? false);

        long score = 0;
        if (!zeroed && days > 0) {
            (int cover, int traffic) =
                StashExposure.AccumulateWeights(container.X, container.Y, surroundings);
            score = StashExposure.ScoreFor(
                isEventState: false,
                residenceIsPartySlotOrCombat: false,
                proximityIntensity: container.Params?.ProximityIntensity ?? 0,
                hundredFlag: false,
                proximityFlagBit2: container.Params?.ProximityFlagBit2 ?? false,
                trafficWeight: traffic,
                coverWeight: cover,
                wholeDaysSinceTouched: days);
        }

        return new Verdict(exempt: false, score,
            StashExposure.IsEmptied(score, roll, forceEmptyEventSet));
    }
}
