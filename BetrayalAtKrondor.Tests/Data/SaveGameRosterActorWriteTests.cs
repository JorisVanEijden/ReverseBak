namespace BetrayalAtKrondor.Tests.Data;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Data;
using ResourceExtraction;
using ResourceExtraction.Extractors;
using System;
using System.IO;
using System.Text;
using Xunit;

/// <summary>
/// Writing an ENEMY's live state back into a save — the 95-byte half of surviving a fight
/// (TASK-230).
/// </summary>
/// <remarks>
/// <b>The offset is the thing under test.</b> These go out through <see cref="SaveGameWriter"/> and
/// back in through <see cref="SaveGameExtractor"/>, so a wrong base or stride shows up as a wound
/// landing on the wrong creature rather than as a passing unit test. The base is independently
/// confirmed: <c>SaveEncounterNpcsToTempGam</c> (IDA <c>0x63265</c>) writes each surviving enemy at
/// <c>0x90E7 + slot * 0x5F</c>, and 0x90E7 is exactly
/// <c>StateDataSize + WorldDataSize</c>.
/// </remarks>
public class SaveGameRosterActorWriteTests {
    static SaveGameRosterActorWriteTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] EmptyBody() => new byte[SaveGameOffsets.BodySize];

    private static SaveGameFields Fields() => new SaveGameFields(
        Chapter: 1, PartyGold: 0, GameTime: 0, TimeSnapshot: 0, PaletteEventMask: 0,
        PartyDeathState: 0, ChapterTransitionPending: 0, PreviousZone: 0, CurrentZone: 1,
        WorldX: 0, WorldY: 0, PositionX: 0, PositionY: 0, PositionZ: 0, Rotation: 0);

    private static ActorStat[] Health(byte current, byte max) {
        var stats = new ActorStat[SaveGameOffsets.ActorAttributeCount];
        for (int i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat();
        }
        stats[(int)ActorAttribute.Health] = new ActorStat {
            Max = max, Base = current, Effective = current,
        };
        return stats;
    }

    private static SaveGame RoundTrip(params DirtyRosterActorEdit[] edits) {
        SaveGameWriteResult written = SaveGameWriter.Write(
            EmptyBody(), Fields(), "test", 0, 0, 0, rosterActorEdits: edits);

        byte[] body = new byte[SaveGameOffsets.BodySize];
        Buffer.BlockCopy(written.Bytes, SaveGameOffsets.HeaderSize, body, 0, body.Length);
        using var stream = new MemoryStream(body);
        return new SaveGameExtractor().Extract("test", stream);
    }

    [Fact]
    public void AWoundedEnemySurvivesTheRoundTrip() {
        SaveGame save = RoundTrip(new DirtyRosterActorEdit(400, Health(current: 9, max: 27)));

        SaveGameActorData actor = save.Data!.ActorStateData[400];
        Assert.Equal(9, actor.Health.Current);
        Assert.Equal(27, actor.Health.Maximum);
    }

    [Fact]
    public void ItLandsOnTHATSlotAndNoOther() {
        // *** What a wrong base or stride actually looks like. *** An off-by-one-record base writes
        // the wound onto the creature beside it, and every assertion about slot 400 still passes.
        SaveGame save = RoundTrip(new DirtyRosterActorEdit(400, Health(current: 9, max: 27)));

        Assert.Equal(0, save.Data!.ActorStateData[399].Health.Current);
        Assert.Equal(0, save.Data!.ActorStateData[401].Health.Current);
    }

    [Fact]
    public void TheROSTERTableIsNotThePartyTable() {
        // *** The conflation the separate edit type exists to prevent. *** Slot 3 of the actor table
        // is not character 3: writing one must leave the other alone, or an enemy's wounds land on a
        // party member.
        SaveGame save = RoundTrip(new DirtyRosterActorEdit(3, Health(current: 5, max: 30)));

        Assert.Equal(5, save.Data!.ActorStateData[3].Health.Current);
        Assert.Equal(0, save.Data!.StateData.PartyActors[3].Health.Current);
    }

    [Fact]
    public void EverySlotOfTheTableIsAddressable() {
        // The bound is 1730 records of 95 bytes, and the last one must fit inside the section rather
        // than run into the combat block that follows it.
        int last = SaveGameOffsets.RosterActorCount - 1;
        Assert.Equal(1730, SaveGameOffsets.RosterActorCount);

        SaveGame save = RoundTrip(new DirtyRosterActorEdit(last, Health(current: 4, max: 4)));

        Assert.Equal(4, save.Data!.ActorStateData[last].Health.Current);
    }

    [Fact]
    public void ASlotOutsideTheTableIsRefusedRatherThanWritten() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RoundTrip(new DirtyRosterActorEdit(SaveGameOffsets.RosterActorCount, Health(1, 1))));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RoundTrip(new DirtyRosterActorEdit(-1, Health(1, 1))));
    }

    [Fact]
    public void NullStatsLeaveTheSavedRecordAlone() {
        // What an enemy nothing touched produces; it must not blank the record.
        SaveGame save = RoundTrip(new DirtyRosterActorEdit(400, null));

        Assert.Equal(0, save.Data!.ActorStateData[400].Health.Current);
    }
}
