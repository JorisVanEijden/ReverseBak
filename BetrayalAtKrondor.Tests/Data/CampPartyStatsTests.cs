namespace BetrayalAtKrondor.Tests.Data;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Config;
using Xunit;

/// <summary>
/// The camp screen's party table (<c>UI_show_actor_healthStatus</c> @0x70d2d).
/// </summary>
public class CampPartyStatsTests {
    private static ActorConditions With(ActorCondition condition, int rank = 50) {
        var c = new ActorConditions();
        c[condition] = rank;
        return c;
    }

    [Fact]
    public void RowsAreSixteenOriginalRowsApart() {
        Assert.Equal(37 * 6, CampPartyStats.RowY(0));
        Assert.Equal(16 * 6, CampPartyStats.RowY(1) - CampPartyStats.RowY(0));
        Assert.Equal(16 * 6, CampPartyStats.RowY(2) - CampPartyStats.RowY(1));
    }

    [Fact]
    public void TheHeadingsCentreWhereTheOriginalPutsThem() {
        // Column bases 84 and 144 from the original's own table, plus the 134 centring offset —
        // VGA 218 and 278, which is where a capture of the original shows them.
        Assert.Equal(218 * 5, CampPartyStats.HeadingCentreX(0));
        Assert.Equal(278 * 5, CampPartyStats.HeadingCentreX(1));
        Assert.Equal(CampPartyStats.ColumnCount, CampPartyStats.ColumnX.Count);
    }

    // ---- the name's ink -----------------------------------------------------------------------

    [Fact]
    public void AHealthyMemberIsDrawnPlain() =>
        Assert.Equal(CampPartyStats.HealthyTextColour, CampPartyStats.NameColour(new ActorConditions()));

    [Theory]
    [InlineData(ActorCondition.Sick)]
    [InlineData(ActorCondition.Plagued)]
    [InlineData(ActorCondition.Poisoned)]
    [InlineData(ActorCondition.Drunk)]
    [InlineData(ActorCondition.Starving)]
    [InlineData(ActorCondition.NearDeath)]
    public void EveryAfflictionRecoloursTheName(ActorCondition condition) =>
        Assert.Equal(CampPartyStats.AfflictedTextColour, CampPartyStats.NameColour(With(condition)));

    [Fact]
    public void HealingIsNotAnAffliction() =>
        // The beneficial entry in the vector. The original tests the other six by name and never
        // looks at this one — the same rule the temple prices by.
        Assert.False(CampPartyStats.IsAfflicted(With(ActorCondition.Healing, rank: 100)));

    [Fact]
    public void AfflictedIsNotTheInverseOfNone() {
        // The trap: ActorConditions.None asks whether the vector is EMPTY, and a regenerating
        // character's is not. Reading `!None` as "afflicted" paints them as sick.
        ActorConditions healing = With(ActorCondition.Healing, rank: 100);

        Assert.False(healing.None);
        Assert.False(CampPartyStats.IsAfflicted(healing));
    }

    [Fact]
    public void AnAfflictionAlongsideHealingStillCounts() =>
        // Being cured of nothing in particular does not mask being poisoned.
        Assert.True(CampPartyStats.IsAfflicted(new ActorConditions {
            [ActorCondition.Healing] = 100,
            [ActorCondition.Poisoned] = 10,
        }));

    [Fact]
    public void NoConditionsAtAllIsSafe() =>
        Assert.False(CampPartyStats.IsAfflicted(null));

    // ---- rations ------------------------------------------------------------------------------

    [Fact]
    public void RationsCountSpoiledAndPoisonedToo() {
        // The column answers "how many meals are in the pack", not "how many are safe to eat".
        int total = CampPartyStats.RationsFor(id => id switch {
            72 => 4,   // Rations
            73 => 2,   // Rations (Poisoned)
            74 => 1,   // Rations (Spoiled)
            _ => 0,
        });

        Assert.Equal(7, total);
    }

    [Fact]
    public void NothingEdibleIsZeroRatherThanBlank() =>
        Assert.Equal(0, CampPartyStats.RationsFor(_ => 0));

    [Fact]
    public void OtherItemsAreNotFood() =>
        // 80 is Picklocks; a pack full of them is still no dinner.
        Assert.Equal(0, CampPartyStats.RationsFor(id => id == 80 ? 99 : 0));
}
