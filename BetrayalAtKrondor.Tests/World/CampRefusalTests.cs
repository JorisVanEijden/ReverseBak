namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The gate at the top of <c>encamp_run</c> — whether the camp screen opens at all.
/// </summary>
public class CampRefusalTests {
    [Fact]
    public void AROAMINGActorInTheOpenForbidsCamp_AtAnyDistanceTheScanListed() {
        // PROXSCAN.C:264 applies no distance test above ground — being in the visible list is the
        // whole condition, so a far-off roamer still stops you sleeping.
        Assert.True(CampRefusal.Watches(roams: true, octagonalDistance: 60000, underground: false));
    }

    [Fact]
    public void UNDERGROUNDOnlyAnActorWithinTheSightRangeCounts() {
        Assert.True(CampRefusal.Watches(true, CampRefusal.UndergroundSightRange, underground: true));
        Assert.False(CampRefusal.Watches(true, CampRefusal.UndergroundSightRange + 1, underground: true));
    }

    [Fact]
    public void SOMETHINGStandingStillDoesNotStopYouSleeping() {
        // rgnenc_slot_actor_kind_eq_placed is `state == 3` — Roaming. A standing guard or a corpse
        // is in the visible list too and does not refuse.
        Assert.False(CampRefusal.Watches(roams: false, octagonalDistance: 0, underground: false));
        Assert.False(CampRefusal.Watches(roams: false, octagonalDistance: 0, underground: true));
    }

    [Fact]
    public void THEDialogIsTheOriginalsRecord() {
        Assert.Equal(0x65u, CampRefusal.WatchedDialogId);
    }
}
