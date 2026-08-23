namespace BetrayalAtKrondor.Tests.Data;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Data;
using GameData.Resources.Dialog.Actions;
using ResourceExtraction;
using ResourceExtraction.Extractors;
using System.IO;
using System.Text;
using Xunit;

/// <summary>
/// Writing live party state back into a save. The offsets these use are not declared by the
/// reader — it walks StateData sequentially — so every test here goes out through
/// <see cref="SaveGameWriter"/> and back in through <see cref="SaveGameExtractor"/>. If the parse
/// order ever moves, these fail instead of the writer quietly corrupting somebody's save.
/// </summary>
public class SaveGameActorWriteTests {
    static SaveGameActorWriteTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] EmptyBody() => new byte[SaveGameOffsets.BodySize];

    private static SaveGameFields Fields() => new SaveGameFields(
        Chapter: 1, PartyGold: 0, GameTime: 0, TimeSnapshot: 0, PaletteEventMask: 0,
        PartyDeathState: 0, ChapterTransitionPending: 0, PreviousZone: 0, CurrentZone: 1, WorldX: 0, WorldY: 0,
        PositionX: 0, PositionY: 0, PositionZ: 0, Rotation: 0);

    private static SaveGame RoundTrip(DirtyActorEdit[] edits) {
        SaveGameWriteResult written = SaveGameWriter.Write(
            EmptyBody(), Fields(), "test", 0, 0, 0, containerEdits: null, actorEdits: edits);

        // Strip the 100-byte slot header so the extractor reads it back as a TEMP.GAM body.
        byte[] body = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(written.Bytes, SaveGameOffsets.HeaderSize, body, 0, body.Length);
        using var stream = new MemoryStream(body);
        return new SaveGameExtractor().Extract("test", stream);
    }

    private static ActorStat[] StatsWith(ActorAttribute attribute, ActorStat stat) {
        var stats = new ActorStat[SaveGameOffsets.ActorAttributeCount];
        for (int i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat();
        }
        stats[(int)attribute] = stat;
        return stats;
    }

    [Fact]
    public void AnAttributeWrittenForAnActorReadsBackFromThatActorsRecord() {
        var stat = new ActorStat { Max = 90, Base = 71, Effective = 68, Experience = 200, Modifier = -5 };

        SaveGame save = RoundTrip(new[] {
            new DirtyActorEdit(2, StatsWith(ActorAttribute.Stealth, stat), null),
        });

        SaveGameAttributeValuesData read = save.Data!.StateData.PartyActors[2].Stealth;
        Assert.Equal(90, read.Maximum);
        Assert.Equal(71, read.Current);
        Assert.Equal(68, read.CurrentEffective);
        Assert.Equal(200, read.Experience);
        Assert.Equal(unchecked((byte)-5), read.Modifier);
    }

    [Fact]
    public void EachPartyMemberLandsInTheirOwnRecord() {
        SaveGame save = RoundTrip(new[] {
            new DirtyActorEdit(0, StatsWith(ActorAttribute.Health, new ActorStat { Max = 60, Base = 11 }), null),
            new DirtyActorEdit(5, StatsWith(ActorAttribute.Health, new ActorStat { Max = 70, Base = 55 }), null),
        });

        Assert.Equal(11, save.Data!.StateData.PartyActors[0].Health.Current);
        Assert.Equal(55, save.Data!.StateData.PartyActors[5].Health.Current);
        Assert.Equal(0, save.Data!.StateData.PartyActors[3].Health.Current);
    }

    [Fact]
    public void EveryAttributeSlotLandsOnTheRightAttribute() {
        var stats = new ActorStat[SaveGameOffsets.ActorAttributeCount];
        for (int i = 0; i < stats.Length; i++) {
            // A distinct value per slot, so a stride mistake shows up as a swap.
            stats[i] = new ActorStat { Max = 100, Base = (byte)(i + 1) };
        }

        SaveGame save = RoundTrip(new[] { new DirtyActorEdit(1, stats, null) });
        SaveGameActorData actor = save.Data!.StateData.PartyActors[1];

        Assert.Equal(1, actor.Health.Current);
        Assert.Equal(2, actor.Stamina.Current);
        Assert.Equal(3, actor.Speed.Current);
        Assert.Equal(4, actor.Strength.Current);
        Assert.Equal(9, actor.Assessment.Current);
        Assert.Equal(14, actor.Lockpick.Current);
        Assert.Equal(16, actor.Stealth.Current);
    }

    [Fact]
    public void AfflictionRanksReadBackForTheRightActor() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Poisoned] = 42;
        conditions[ActorCondition.NearDeath] = 7;

        SaveGame save = RoundTrip(new[] { new DirtyActorEdit(4, null, conditions) });

        SaveGameActorStatusEffectsData read = save.Data!.StateData.PartyConfigurationData.ActorStatusEffects[4];
        Assert.Equal(42, read.Poisoned);
        Assert.Equal(7, read.NearDeath);
        Assert.Equal(0, read.Sick);
        Assert.Equal(0, save.Data!.StateData.PartyConfigurationData.ActorStatusEffects[0].Poisoned);
    }

    [Fact]
    public void EveryAfflictionSlotLandsOnTheRightAffliction() {
        var conditions = new ActorConditions();
        for (int i = 0; i < ActorConditions.Count; i++) {
            conditions[(ActorCondition)i] = i + 1;
        }

        SaveGame save = RoundTrip(new[] { new DirtyActorEdit(0, null, conditions) });
        SaveGameActorStatusEffectsData read = save.Data!.StateData.PartyConfigurationData.ActorStatusEffects[0];

        Assert.Equal(1, read.Sick);
        Assert.Equal(2, read.Plagued);
        Assert.Equal(3, read.Poisoned);
        Assert.Equal(4, read.Drunk);
        Assert.Equal(5, read.Healing);
        Assert.Equal(6, read.Starving);
        Assert.Equal(7, read.NearDeath);
    }

    [Fact]
    public void WritingStatsLeavesAfflictionsAloneAndViceVersa() {
        byte[] body = EmptyBody();
        body[SaveGameOffsets.ActorStatusEffects + 2] = 33; // actor 0, Poisoned

        SaveGameWriteResult written = SaveGameWriter.Write(
            body, Fields(), "test", 0, 0, 0, containerEdits: null,
            actorEdits: new[] {
                new DirtyActorEdit(0, StatsWith(ActorAttribute.Health, new ActorStat { Max = 50, Base = 50 }), null),
            });

        byte[] roundTripped = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(written.Bytes, SaveGameOffsets.HeaderSize, roundTripped, 0, roundTripped.Length);
        using var stream = new MemoryStream(roundTripped);
        SaveGame save = new SaveGameExtractor().Extract("test", stream);

        Assert.Equal(50, save.Data!.StateData.PartyActors[0].Health.Current);
        Assert.Equal(33, save.Data!.StateData.PartyConfigurationData.ActorStatusEffects[0].Poisoned);
    }

    [Fact]
    public void NoActorEditsLeavesTheRecordsExactlyAsTheyWere() {
        byte[] body = EmptyBody();
        body[SaveGameOffsets.PartyActors + SaveGameOffsets.ActorAttributesInRecord + 1] = 77;

        SaveGameWriteResult written = SaveGameWriter.Write(body, Fields(), "test", 0, 0, 0);

        Assert.Equal(77, written.Bytes[SaveGameOffsets.HeaderSize + SaveGameOffsets.PartyActors
            + SaveGameOffsets.ActorAttributesInRecord + 1]);
    }

    [Fact]
    public void ACharacterIndexOutsideThePartyIsRejected() {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => SaveGameWriter.Write(
            EmptyBody(), Fields(), "test", 0, 0, 0, containerEdits: null,
            actorEdits: new[] { new DirtyActorEdit(6, null, null) }));
    }

    [Fact]
    public void WrittenActorBytesCountTowardsCoverage() {
        SaveGameWriteResult bare = SaveGameWriter.Write(EmptyBody(), Fields(), "test", 0, 0, 0);
        SaveGameWriteResult withActors = SaveGameWriter.Write(
            EmptyBody(), Fields(), "test", 0, 0, 0, containerEdits: null,
            actorEdits: new[] {
                new DirtyActorEdit(0, StatsWith(ActorAttribute.Health, new ActorStat()), new ActorConditions()),
            });

        // 16 attributes x 5 bytes + 7 affliction ranks.
        Assert.Equal(bare.Coverage.AuthoredBytes + 16 * 5 + 7, withActors.Coverage.AuthoredBytes);
    }

    // ---- the pending-timer pool -----------------------------------------------------------

    private static SaveGame RoundTripTimers(SaveGameTimerData[] timers) {
        SaveGameWriteResult written = SaveGameWriter.Write(
            EmptyBody(), Fields(), "test", 0, 0, 0, containerEdits: null, actorEdits: null,
            timers: timers);
        byte[] body = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(written.Bytes, SaveGameOffsets.HeaderSize, body, 0, body.Length);
        using var stream = new MemoryStream(body);
        return new SaveGameExtractor().Extract("test", stream);
    }

    /// <summary>
    /// A queued timer has to survive a save: the shipped example is the corpse-flavour flag 8127,
    /// whose clear is scheduled two hours out. Lose the timer and the flag stays set for good.
    /// </summary>
    [Fact]
    public void APendingTimerReadsBackWithItsTypeKeyAndRemainingTime() {
        SaveGame save = RoundTripTimers(new[] {
            new SaveGameTimerData(TimerType.ClearFlag, 0, 8127, 3600),
        });

        Assert.Equal(1, save.Data!.StateData.CurrentTimerAmount);
        SaveGameTimerData read = save.Data!.StateData.Timers[0];
        Assert.Equal(TimerType.ClearFlag, read.Type);
        Assert.Equal((short)8127, read.Key);
        Assert.Equal(3600, read.Time);
    }

    [Fact]
    public void EveryTimerSlotLandsInItsOwnPlace() {
        var timers = new SaveGameTimerData[3];
        for (int i = 0; i < timers.Length; i++) {
            timers[i] = new SaveGameTimerData(TimerType.SetFlag, 0, (short)(100 + i), 1000 + i);
        }

        SaveGame save = RoundTripTimers(timers);

        Assert.Equal(3, save.Data!.StateData.CurrentTimerAmount);
        for (int i = 0; i < timers.Length; i++) {
            Assert.Equal((short)(100 + i), save.Data!.StateData.Timers[i].Key);
            Assert.Equal(1000 + i, save.Data!.StateData.Timers[i].Time);
        }
    }

    /// <summary>A shrinking pool must not leave a stale timer sitting past the live count.</summary>
    [Fact]
    public void SlotsBeyondTheLiveCountAreBlanked() {
        byte[] body = EmptyBody();
        int stale = SaveGameOffsets.TimerPool + SaveGameOffsets.TimerStride;
        body[stale] = 3;
        body[stale + 4] = 99;

        SaveGameWriteResult written = SaveGameWriter.Write(
            body, Fields(), "test", 0, 0, 0, containerEdits: null, actorEdits: null,
            timers: new[] { new SaveGameTimerData(TimerType.SetFlag, 0, 1, 500) });

        byte[] roundTripped = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(written.Bytes, SaveGameOffsets.HeaderSize, roundTripped, 0, roundTripped.Length);
        using var stream = new MemoryStream(roundTripped);
        SaveGame save = new SaveGameExtractor().Extract("test", stream);

        Assert.Equal(1, save.Data!.StateData.CurrentTimerAmount);
        Assert.Equal(0, save.Data!.StateData.Timers[1].Time);
    }

    [Fact]
    public void TheLastRestSnapshotIsWritten() {
        var fields = new SaveGameFields(
            Chapter: 1, PartyGold: 0, GameTime: 90000, TimeSnapshot: 87654, PaletteEventMask: 0,
        PartyDeathState: 0, ChapterTransitionPending: 0, PreviousZone: 0, CurrentZone: 1,
            WorldX: 0, WorldY: 0, PositionX: 0, PositionY: 0, PositionZ: 0, Rotation: 0);
        SaveGameWriteResult written = SaveGameWriter.Write(EmptyBody(), fields, "test", 0, 0, 0);

        byte[] body = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(written.Bytes, SaveGameOffsets.HeaderSize, body, 0, body.Length);
        using var stream = new MemoryStream(body);
        SaveGame save = new SaveGameExtractor().Extract("test", stream);

        Assert.Equal(87654, save.Data!.StateData.TimeSnapshot);
    }
}
