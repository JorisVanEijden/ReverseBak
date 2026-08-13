namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using GameData.Resources.Data;
using GameData.Resources.GameState;
using GameData.Resources.Inventory;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The rules around casting: who may cast, what they may cast where and when, how much power they
/// may choose, and what it costs them. The zone and timing gates are the ones worth pinning — they
/// key off specific spell ids, so a wrong constant silently makes the wrong spell unavailable.
/// </summary>
public class SpellCastingTests {
    private const int CandleGlowId = 2;
    private const int SkyfireId = 5;
    private const int StarduskId = 0x1a;

    private static Spell SpellWith(int minimumCost = 1, int maximumCost = 20, int objectId = -1,
        int targetingType = 1) =>
        new Spell("t") {
            MinimumCost = minimumCost,
            MaximumCost = maximumCost,
            ObjectId = objectId,
            TargetingType = targetingType,
        };

    private static SpellCastContext ContextKnowing(params int[] spellIds) {
        ushort[] book = SpellBook.Empty();
        foreach (int id in spellIds) {
            SpellBook.Learn(book, id);
        }
        return new SpellCastContext {
            Chapter = 1,
            ZoneKind = 1,
            KnownSpells = book,
            HealthStaminaPool = 40,
            Inventory = new RuntimeContainer { Capacity = 20 },
        };
    }

    [Fact]
    public void CastingSkillIsReadFromTheMaximumSoADrainedCasterIsStillACaster() {
        // gstate_actor_is_caster reads max, not the current value.
        var drained = new ActorStat { Base = 0, Max = 30 };

        Assert.True(SpellCasting.IsCaster(drained.Max));
        Assert.False(SpellCasting.IsCaster(0));
    }

    [Fact]
    public void ASpellMustBeKnownNoMatterWhatElseIsTrue() {
        SpellCastContext context = ContextKnowing();

        Assert.False(SpellCasting.IsCastable(7, SpellWith(), context));
        Assert.False(SpellCasting.IsCastable(7, SpellWith(), context, knowledgeOnly: true));
    }

    [Fact]
    public void KnowledgeOnlySkipsEveryOtherRule() {
        // The book renders from this reading, so a spell the caster cannot afford or use here must
        // still appear in it.
        SpellCastContext context = ContextKnowing(CandleGlowId);
        context.HealthStaminaPool = 0;

        Assert.True(SpellCasting.IsCastable(CandleGlowId, SpellWith(), context, knowledgeOnly: true));
        Assert.False(SpellCasting.IsCastable(CandleGlowId, SpellWith(), context));
    }

    [Fact]
    public void CandleGlowIsCastableOnlyInAnEnclosedZone() {
        SpellCastContext context = ContextKnowing(CandleGlowId);

        context.ZoneKind = SpellCasting.EnclosedZoneKind;
        Assert.True(SpellCasting.IsCastable(CandleGlowId, SpellWith(), context));

        context.ZoneKind = 1;
        Assert.False(SpellCasting.IsCastable(CandleGlowId, SpellWith(), context));
    }

    [Fact]
    public void SkyfireAndStarduskAreRefusedInAnEnclosedZone() {
        SpellCastContext context = ContextKnowing(SkyfireId, StarduskId);
        context.ZoneKind = SpellCasting.EnclosedZoneKind;

        Assert.False(SpellCasting.IsCastable(SkyfireId, SpellWith(), context));
        Assert.False(SpellCasting.IsCastable(StarduskId, SpellWith(), context));
    }

    [Theory]
    [InlineData(0, true)]    // midnight
    [InlineData(7, true)]    // last hour before the block
    [InlineData(8, false)]   // block starts
    [InlineData(16, false)]  // last blocked hour
    [InlineData(17, true)]   // block ends
    [InlineData(23, true)]
    public void StarduskIsRefusedOutdoorsInDaylight(int hour, bool expected) {
        SpellCastContext context = ContextKnowing(StarduskId);
        context.ZoneKind = 1;
        context.GameTimeIn2Seconds = hour * GameClock.UnitsPerHour;

        Assert.Equal(expected, SpellCasting.IsCastable(StarduskId, SpellWith(), context));
    }

    [Fact]
    public void TheHourMatchesTheOriginalsRawUnitFormula() {
        // cspell_check_castable computes (time % 0xa8c0) / 0x708 directly; SaveGameSection0 goes via
        // seconds. They must agree or one of the two derivations is wrong about the unit.
        for (var t = 0; t < GameClock.UnitsPerDay * 2; t += 97) {
            Assert.Equal((t % 0xa8c0) / 0x708, GameClock.HourOfDay(t));
        }
    }

    [Fact]
    public void ThePoolMustExceedTheBaseCostNotMerelyMatchIt() {
        SpellCastContext context = ContextKnowing(7);
        Spell spell = SpellWith(minimumCost: 10);

        context.HealthStaminaPool = 10;
        Assert.False(SpellCasting.IsCastable(7, spell, context));

        context.HealthStaminaPool = 11;
        Assert.True(SpellCasting.IsCastable(7, spell, context));
    }

    [Fact]
    public void AMissingComponentBlocksTheSpell() {
        SpellCastContext context = ContextKnowing(7);
        Spell spell = SpellWith(objectId: 17);

        Assert.False(SpellCasting.IsCastable(7, spell, context));

        context.Inventory.Items.Add(new RuntimeItem(17, 1, 0));
        Assert.True(SpellCasting.IsCastable(7, spell, context));
    }

    [Fact]
    public void AComponentWithNoChargesLeftReadsAsAbsent() {
        // count_by_kind sums condition when it is non-zero, so a spent stack counts as nothing even
        // though the item is still in the pack.
        SpellCastContext context = ContextKnowing(7);
        context.Inventory.Items.Add(new RuntimeItem(17, 0, 0));

        Assert.Equal(1, InventoryQuery.CountByKind(context.Inventory, 17));

        context.Inventory.Items.Clear();
        context.Inventory.Items.Add(new RuntimeItem(17, 3, 0));
        Assert.Equal(3, InventoryQuery.CountByKind(context.Inventory, 17));
    }

    [Fact]
    public void SummoningIsRefusedWhenTheGridIsFull() {
        SpellCastContext context = ContextKnowing(7);
        Spell summon = SpellWith(targetingType: 6);

        context.CombatActorCount = 6;
        Assert.True(SpellCasting.IsCastable(7, summon, context));

        context.CombatActorCount = 7;
        Assert.False(SpellCasting.IsCastable(7, summon, context));
    }

    [Fact]
    public void ThePowerCeilingDropsToJustBelowTheCastersPool() {
        SpellCastContext context = ContextKnowing(7);
        context.HealthStaminaPool = 8;

        PowerRange range = SpellCasting.GetPowerRange(SpellWith(minimumCost: 2, maximumCost: 20),
            context);

        Assert.Equal(2, range.Minimum);
        Assert.Equal(7, range.Maximum);
        Assert.False(range.IsFixed);
    }

    [Fact]
    public void ASpellWhoseBandCollapsesOffersNoChoice() {
        SpellCastContext context = ContextKnowing(7);
        context.HealthStaminaPool = 100;

        PowerRange range = SpellCasting.GetPowerRange(SpellWith(minimumCost: 12, maximumCost: 12),
            context);

        Assert.True(range.IsFixed);
        Assert.Equal(12, range.Minimum);
    }

    [Fact]
    public void ABudgetBelowTheBaseCostLeavesAnEmptyRangeRatherThanABackwardsOne() {
        // The original has no branch for this and would open a slider running the wrong way, so the
        // range reports it instead of pretending to be valid.
        SpellCastContext context = ContextKnowing(7);
        context.HealthStaminaPool = 3;

        PowerRange range = SpellCasting.GetPowerRange(SpellWith(minimumCost: 10, maximumCost: 20),
            context);

        Assert.True(range.IsEmpty);
    }

    [Fact]
    public void InChapterEightNothingIsCastableWithoutAPowerSource() {
        SpellCastContext context = ContextKnowing(7);
        context.Chapter = SpellCasting.PowerSourceChapter;

        Assert.False(SpellCasting.IsCastable(7, SpellWith(), context));

        // Present but unflagged is still refused by the castability rule.
        context.Inventory.Items.Add(new RuntimeItem(SpellCasting.PowerSourceObjectId, 30, 0));
        Assert.False(SpellCasting.IsCastable(7, SpellWith(), context));

        context.Inventory.Items.Clear();
        context.Inventory.Items.Add(new RuntimeItem(SpellCasting.PowerSourceObjectId, 30,
            SpellCasting.PowerSourceReadyFlag));
        Assert.True(SpellCasting.IsCastable(7, SpellWith(), context));
    }

    [Fact]
    public void TheChapterEightPowerSourceCapsTheSelectableRange() {
        SpellCastContext context = ContextKnowing(7);
        context.Chapter = SpellCasting.PowerSourceChapter;
        context.HealthStaminaPool = 100;
        context.Inventory.Items.Add(new RuntimeItem(SpellCasting.PowerSourceObjectId, 6,
            SpellCasting.PowerSourceReadyFlag));

        PowerRange range = SpellCasting.GetPowerRange(SpellWith(minimumCost: 1, maximumCost: 40),
            context);

        Assert.Equal(5, range.Maximum); // the source's 6 charges, minus one
    }

    [Fact]
    public void CastingDrainsBothThePoolAndTheChapterEightSource() {
        SpellCastContext context = ContextKnowing(7);
        context.Chapter = SpellCasting.PowerSourceChapter;
        var source = new RuntimeItem(SpellCasting.PowerSourceObjectId, 20,
            SpellCasting.PowerSourceReadyFlag);
        context.Inventory.Items.Add(source);

        var health = new ActorStat { Base = 30, Max = 30 };
        var stamina = new ActorStat { Base = 30, Max = 30 };

        SpellCasting.ApplyCost(context, 8, health, stamina, out bool collapsed);

        Assert.False(collapsed);
        Assert.Equal(52, health.Base + stamina.Base); // 60 - 8
        Assert.Equal(12, source.Variable);
    }

    [Fact]
    public void AnOverdrawnPowerSourceFloorsAtZeroRatherThanWrapping() {
        // Variable is a byte, so subtracting past zero would wrap to a near-full source.
        SpellCastContext context = ContextKnowing(7);
        context.Chapter = SpellCasting.PowerSourceChapter;
        var source = new RuntimeItem(SpellCasting.PowerSourceObjectId, 3,
            SpellCasting.PowerSourceReadyFlag);
        context.Inventory.Items.Add(source);

        var health = new ActorStat { Base = 30, Max = 30 };
        var stamina = new ActorStat { Base = 30, Max = 30 };

        SpellCasting.ApplyCost(context, 10, health, stamina, out _);

        Assert.Equal(0, source.Variable);
    }

    [Fact]
    public void OutsideChapterEightThePowerSourceIsIgnoredEntirely() {
        SpellCastContext context = ContextKnowing(7);
        context.Chapter = 7;
        var source = new RuntimeItem(SpellCasting.PowerSourceObjectId, 2, 0);
        context.Inventory.Items.Add(source);
        context.HealthStaminaPool = 40;

        PowerRange range = SpellCasting.GetPowerRange(SpellWith(minimumCost: 1, maximumCost: 30),
            context);

        Assert.Equal(30, range.Maximum);

        var health = new ActorStat { Base = 30, Max = 30 };
        var stamina = new ActorStat { Base = 30, Max = 30 };
        SpellCasting.ApplyCost(context, 5, health, stamina, out _);
        Assert.Equal(2, source.Variable);
    }
}
