namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The per-encounter retreat lock — TRAPS.DAT element type -18.
/// </summary>
public class TrapRetreatLockTests {
    private static TrapData WithElements(int encounterNumber, params int[] types) {
        var data = new TrapData("TRAPS.DAT");
        var record = new TrapEncounter { Index = encounterNumber };
        foreach (int type in types) {
            record.Elements.Add(new TrapElement { Type = type });
        }
        data.Encounters.Add(record);
        return data;
    }

    [Fact]
    public void TheLockIsAnOptOutSoAnOrdinaryEncounterAllowsRetreat() {
        // The polarity is the whole point: the original raises the flag before reading the record,
        // so "has a record but no lock element" must answer the same as "has no record at all".
        TrapData data = WithElements(12, (int)TrapElementType.RedCrystal,
            (int)TrapElementType.ActorSlot0);
        Assert.True(data.AllowsRetreat(12));
        Assert.True(data.AllowsRetreat(999));
    }

    [Fact]
    public void TheLockElementForbidsIt() {
        TrapData data = WithElements(12, (int)TrapElementType.RedCrystal,
            (int)TrapElementType.RetreatLock);
        Assert.False(data.AllowsRetreat(12));
    }

    [Fact]
    public void AnExitTileIsNotTheLock() {
        // 35 encounters carry an exit and only 5 carry the lock. Deriving "no retreat" from the exit
        // would lock thirty fights the game lets you leave, which is why these are asserted apart.
        TrapData data = WithElements(12, (int)TrapElementType.Exit,
            (int)TrapElementType.ActorSlot0, (int)TrapElementType.ActorSlot1);
        Assert.True(data.AllowsRetreat(12));
    }
}
