namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The gate at the top of <c>encamp_run</c> — whether the camp screen opens at all.
/// </summary>
public class CampRefusalTests {
    [Fact]
    public void AROAMINGActorInTheOpenForbidsCampOnlyWithinTheVISIBLELISTSOwnReach() {
        // *** THIS TEST USED TO ASSERT THE OPPOSITE, AT 60000. *** Being in the visible list IS the
        // whole condition above ground — but that list is distance-filtered as it is BUILT
        // (`metric < g_aFilterTable[kind]`), so "any distance the scan listed" has a limit and the
        // old expectation read PROXSCAN.C:264's underground cap as the only one there is.
        //
        // Measured on the running original 2026-09-13 by parking the party at a series of distances
        // from a placed shade and asking the game: in view at 18749, out of view at 18750.
        Assert.True(CampRefusal.Watches(true, CampRefusal.VisibleListRange - 1, underground: false));
        Assert.False(CampRefusal.Watches(true, CampRefusal.VisibleListRange, underground: false));
        Assert.False(CampRefusal.Watches(true, 60000, underground: false));
    }

    [Fact]
    public void UNDERGROUNDTheCapAppliesONTOPOfTheListsReach() {
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
