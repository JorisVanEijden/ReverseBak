namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Audio;
using GameData.Resources.Character;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Playing the practice lute — <c>ITEMUSE.C</c>'s <c>case 0x51</c>, the item-81 arm of the
/// category-25 switch.
/// </summary>
/// <remarks>
/// <b>An unwired model is an untested claim.</b> <see cref="MusicSelection.ForLutePractice"/> spent
/// months written up as a healing item keyed on Health — wrong on both counts — precisely because
/// nothing called it. These are the tests that come with calling it.
/// </remarks>
public class PracticeLuteTests {
    private const int UsedRecord = 1800002;   // 0x1B7742

    // *** THE SHIPPED FLAGS, NOT CONVENIENT ONES. *** objinfo says the Practice Lute is
    // "DiscardWhenEmpty, NotUsableInCombat, LimitedUses" — so it is a CHARGED item, and a fixture
    // that left the flags off would pass while the lute played for ever.
    private static ObjectInfoSet Objects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("O") {
            Number = MusicSelection.PracticeLuteItemId, Name = "Practice Lute",
            ObjectType = ObjectType.Usable, InventorySlots = 2, MaxAmount = 1,
            Flags = ObjectFlags.LimitedUses | ObjectFlags.DiscardWhenEmpty
                | ObjectFlags.NotUsableInCombat,
        },
    });

    private const byte Charges = 20;

    private static RuntimeContainer Pack(byte charges = Charges) {
        var c = new RuntimeContainer();
        c.Items.Add(new RuntimeItem((byte)MusicSelection.PracticeLuteItemId, charges, 0));
        return c;
    }

    /// <summary>A character whose Barding reads <paramref name="barding"/> and who is unhurt.</summary>
    private static ActorStat[] Stats(byte barding, byte health = 40, byte healthMax = 40) {
        var stats = new ActorStat[16];
        for (var i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 0, Max = 99 };
        }
        stats[(int)ActorAttribute.Health] = new ActorStat { Base = health, Max = healthMax };
        stats[(int)ActorAttribute.Stamina] = new ActorStat { Base = 40, Max = 40 };
        stats[(int)ActorAttribute.Barding] = new ActorStat { Base = barding, Max = 99 };
        return stats;
    }

    private static ItemUseContext Context(ActorStat[] stats, int roll = 0) =>
        new ItemUseContext(stats, 1, _ => 0, (_, __) => { }, _ => roll);

    private static ItemUseResult Play(ActorStat[] stats, int roll = 0) =>
        InventoryUse.Use(Pack(), 0, InventoryUse.NoTarget, Objects(), Context(stats, roll));

    [Fact]
    public void TheTuneComesFromBarding() {
        // Four bands, and the boundaries are STRICTLY greater — 0x50 exactly gets the second tune.
        Assert.Equal(MusicSelection.ForLutePractice(0x51), Play(Stats(0x51)).MusicTrack);
        Assert.Equal(MusicSelection.ForLutePractice(0x50), Play(Stats(0x50)).MusicTrack);
        Assert.Equal(MusicSelection.ForLutePractice(0x20), Play(Stats(0x20)).MusicTrack);
    }

    [Fact]
    public void AWoundedMusicianPlaysAWorseTune() {
        // *** The Barding read is mode 0 — the EFFECTIVE value — so it carries the health scaling.
        // Reading the stored value instead would let a half-dead bard play like a healthy one. ***
        int healthy = Play(Stats(0x60, health: 40, healthMax: 40)).MusicTrack;
        int hurt = Play(Stats(0x60, health: 4, healthMax: 40)).MusicTrack;

        Assert.Equal(MusicSelection.ForLutePractice(0x60), healthy);
        Assert.NotEqual(healthy, hurt);
    }

    [Fact]
    public void TheTuneIsForTheSkillYouHadBEFOREPractising() {
        // The original raises Barding only after the dialog closes. A player at the top of a band
        // must not hear the better tune on the very run that earned it.
        ActorStat[] stats = Stats(0x50);
        ItemUseResult r = Play(stats, roll: MusicSelection.PracticeGainHigh);

        Assert.Equal(MusicSelection.ForLutePractice(0x50), r.MusicTrack);
        Assert.NotEqual(MusicSelection.ForLutePractice(0x51), r.MusicTrack);
    }

    [Fact]
    public void OnePracticeIsAFRACTIONOfAPoint() {
        // *** The roll goes to the modifier UNSHIFTED. *** Shifting it like every other caller
        // would train Barding 40-160x too fast — the whole skill in a couple of strums.
        ActorStat[] stats = Stats(0x30);
        byte before = stats[(int)ActorAttribute.Barding].Base;

        Play(stats, roll: MusicSelection.PracticeGainHigh - MusicSelection.PracticeGainLow);

        Assert.Equal(before, stats[(int)ActorAttribute.Barding].Base);
        Assert.Equal(MusicSelection.PracticeGainHigh, stats[(int)ActorAttribute.Barding].Experience);
        Assert.True(MusicSelection.PracticeGainIsFractional);
    }

    [Fact]
    public void RepeatedPracticeBanksUpIntoWholePoints() {
        // Which is the point of a sub-unit gain: it is not "no effect", it is deferred effect.
        ActorStat[] stats = Stats(0x30);
        RuntimeContainer pack = Pack(charges: 250);
        byte before = stats[(int)ActorAttribute.Barding].Base;

        for (var i = 0; i < 8; i++) {
            InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(),
                Context(stats, MusicSelection.PracticeGainHigh - MusicSelection.PracticeGainLow));
        }

        Assert.True(stats[(int)ActorAttribute.Barding].Base > before,
            "eight practices at the top of the roll are worth more than a whole point");
    }

    [Fact]
    public void ItPlaysTheUsedRecordAndKeepsTheLute() {
        ItemUseResult r = Play(Stats(0x30));

        Assert.Equal(ItemUseOutcome.Handled, r.Outcome);
        Assert.Equal(UsedRecord, r.DialogId);
        Assert.False(r.SourceRemoved);
    }

    [Fact]
    public void EachPracticeSPENDSAUse() {
        // The arm sets outcome and BREAKS — it does not return — so the common tail runs and takes
        // a charge. Skipping it gives an infinite lute, and every other assertion here still passes.
        RuntimeContainer pack = Pack(charges: 3);

        InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(), Context(Stats(0x30)));

        Assert.Equal(2, pack.Items[0].Variable);
        Assert.True(pack.Dirty, "the pack changed, so a save has to write it");
    }

    [Fact]
    public void TheLastPracticeDiscardsTheLute() {
        // DiscardWhenEmpty, from the shipped record.
        RuntimeContainer pack = Pack(charges: 1);

        ItemUseResult r = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(),
            Context(Stats(0x30)));

        Assert.True(r.SourceRemoved);
        Assert.Empty(pack.Items);
        Assert.Equal(UsedRecord, r.DialogId);
        Assert.NotEqual(MusicPlayback.QueryOnly, r.MusicTrack);
    }

    [Fact]
    public void AnOrdinaryUseAsksForNoMusicAtAll() {
        // QueryOnly, not NoTrack: NoTrack means SILENCE, so every other item use would stop the
        // music the moment it was used.
        Assert.Equal(MusicPlayback.QueryOnly, new ItemUseResult(ItemUseOutcome.Applied, 0, 0, false).MusicTrack);
        Assert.NotEqual(MusicPlayback.NoTrack, MusicPlayback.QueryOnly);
    }

    [Fact]
    public void WithNoCharacterBehindItNothingHappens() {
        // Silent rather than "no effect": the original would have played something.
        ItemUseResult r = InventoryUse.Use(Pack(), 0, InventoryUse.NoTarget, Objects(), context: null);

        Assert.Equal(ItemUseOutcome.NotPorted, r.Outcome);
        Assert.Equal(MusicPlayback.QueryOnly, r.MusicTrack);
    }
}
