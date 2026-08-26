namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Config;
using Xunit;

/// <summary>
/// The pose the arena is viewed from.
/// </summary>
/// <remarks>
/// The trap these pin down: the arena camera's height is ABSOLUTE while the explore camera's
/// neighbouring value is an offset above the ground. Two cameras, two rules.
/// </remarks>
public class CombatArenaCameraTests {
    // The shipped START.DAT values.
    private static StartData Shipped() => new("START.DAT") {
        CombatCameraHeightAboveGround = 1024,
        CombatCameraHeightUnderground = 800,
        CombatCameraPitchAboveGround = -2112,
        CombatCameraPitchUnderground = -3030,
    };

    [Fact]
    public void TheZoneKindPicksThePair_NotTheChapter() {
        StartData s = Shipped();
        Assert.Equal(1024, CombatArenaCamera.HeightFor(s, underground: false));
        Assert.Equal(800, CombatArenaCamera.HeightFor(s, underground: true));
        Assert.Equal(-2112, CombatArenaCamera.PitchFor(s, underground: false));
        Assert.Equal(-3030, CombatArenaCamera.PitchFor(s, underground: true));
    }

    [Fact]
    public void ADungeonSitsLowerAndLooksFurtherDown() {
        StartData s = Shipped();
        Assert.True(CombatArenaCamera.HeightFor(s, true) < CombatArenaCamera.HeightFor(s, false));
        // More negative is steeper: both are downward tilts in 16-bit angle units.
        Assert.True(CombatArenaCamera.PitchFor(s, true) < CombatArenaCamera.PitchFor(s, false));
    }

    [Fact]
    public void TheHeightIsAWorldZ_NotAnOffsetAboveTheGround() {
        // *** The failure this catches. *** Applying it the explore camera's way — ground + value —
        // lifts the arena by the terrain height under the party, which is invisible on flat ground
        // and tilts the whole fight off the floor on a slope.
        Assert.True(CombatArenaCamera.HeightIsAbsolute);
    }

    [Fact]
    public void AnUnloadedStartRecordIsRefusedRatherThanUsedAsZero() {
        Assert.False(CombatArenaCamera.IsUsable(null, false));
        Assert.False(CombatArenaCamera.IsUsable(new StartData("START.DAT"), false));
        Assert.True(CombatArenaCamera.IsUsable(Shipped(), false));
        Assert.True(CombatArenaCamera.IsUsable(Shipped(), true));
    }
}
