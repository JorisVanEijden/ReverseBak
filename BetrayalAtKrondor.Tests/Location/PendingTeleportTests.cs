namespace BetrayalAtKrondor.Tests.Location;

using GameData.Resources.Location;
using Xunit;

/// <summary>
/// The teleport hand-off slot (<c>teleportationData</c>). What carries: a destination is two
/// independent halves, taken by two different loops at two different moments.
/// </summary>
public class PendingTeleportTests {
    // Row 0 of the shipped TELEPORT.DAT: the Temple of Kilian, which has both halves.
    private static TeleportDestination Temple() => new TeleportDestination {
        Id = 0,
        GdsNumber = 70,
        GdsLetter = 1,
        Location = new Location { ZoneNumber = 1, X = 11, Y = 11, XOffset = 33, YOffset = 16, ZRotation = 8192 },
    };

    // A dialog teleport — a ladder or tunnel. 18 of the 40 shipped rows look like this.
    private static TeleportDestination LadderOrTunnel() => new TeleportDestination {
        Id = 12,
        GdsNumber = 0,
        GdsLetter = 0,
        Location = new Location { ZoneNumber = 2, X = 4, Y = 7 },
    };

    [Fact]
    public void AnEmptySlotOffersNothing() {
        var slot = new PendingTeleport();

        Assert.False(slot.HasAnything);
        Assert.False(slot.HasScene);
        Assert.False(slot.TryTakeScene(out _, out _));
        Assert.Null(slot.TakeLocation());
    }

    [Fact]
    public void ATempleSendsTheLocationLoopToItsOwnScene() {
        var slot = new PendingTeleport();
        slot.Queue(Temple());

        Assert.True(slot.TryTakeScene(out int number, out int letter));
        Assert.Equal(70, number);
        Assert.Equal(1, letter);
    }

    [Fact]
    public void TakingTheSceneLeavesTheWorldMoveBehind() {
        // The half the location loop takes is not the half ProcessTeleportation takes. Consuming the
        // whole record on the scene switch is what would strand the party in the old zone.
        var slot = new PendingTeleport();
        slot.Queue(Temple());
        slot.TryTakeScene(out _, out _);

        Location? where = slot.TakeLocation();

        Assert.NotNull(where);
        Assert.Equal(1, where!.ZoneNumber);
        Assert.Equal(11, where.X);
    }

    [Fact]
    public void TheSceneSwitchHappensOnlyOnce() {
        // Otherwise the location loop re-enters the same scene forever.
        var slot = new PendingTeleport();
        slot.Queue(Temple());

        Assert.True(slot.TryTakeScene(out _, out _));
        Assert.False(slot.TryTakeScene(out _, out _));
        Assert.False(slot.HasScene);
    }

    [Fact]
    public void ALadderHasNoSceneAndJustMovesYou() {
        var slot = new PendingTeleport();
        slot.Queue(LadderOrTunnel());

        Assert.True(slot.HasAnything);
        Assert.False(slot.HasScene);
        Assert.False(slot.TryTakeScene(out _, out _));
        Assert.Equal(2, slot.TakeLocation()!.ZoneNumber);
    }

    [Fact]
    public void TakingTheLocationEmptiesTheSlot() {
        var slot = new PendingTeleport();
        slot.Queue(Temple());

        slot.TakeLocation();

        Assert.False(slot.HasAnything);
        Assert.Null(slot.TakeLocation());
    }

    [Fact]
    public void QueueingAgainRedirectsRatherThanStacking() {
        // One global, last writer wins: a scene that queues a teleport during another is changing
        // where you end up, not adding a second hop.
        var slot = new PendingTeleport();
        slot.Queue(Temple());
        slot.Queue(LadderOrTunnel());

        Assert.False(slot.HasScene);
        Assert.Equal(2, slot.TakeLocation()!.ZoneNumber);
    }

    [Fact]
    public void ClearingAbandonsBothHalves() {
        var slot = new PendingTeleport();
        slot.Queue(Temple());

        slot.Clear();

        Assert.False(slot.HasAnything);
        Assert.False(slot.HasScene);
    }
}
