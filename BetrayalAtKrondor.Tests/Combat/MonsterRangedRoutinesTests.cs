namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The two ranged creature routines. Both spend a lot of turns not attacking, and one of them is
/// filed among the melee handlers while preferring to shoot.
/// </summary>
public class MonsterRangedRoutinesTests {
    private const int Spitter = 0x29;
    private const int Hurler = 0x2a;
    private const int Breather = 0x2b;

    [Fact]
    public void HalfOfItsTurnsAreSpentNotShootingEvenWithAClearLine() {
        // The abort roll runs before anything else. Skipping it would roughly double these
        // creatures' damage output.
        Assert.Equal(MonsterTurnRoutines.RangedChoice.Reconsider,
            MonsterTurnRoutines.ChooseRangedTurn(true, 0, 0, Spitter).Choice);
        Assert.Equal(MonsterTurnRoutines.RangedChoice.Reconsider,
            MonsterTurnRoutines.ChooseRangedTurn(true, 0x31, 0, Spitter).Choice);
        Assert.NotEqual(MonsterTurnRoutines.RangedChoice.Reconsider,
            MonsterTurnRoutines.ChooseRangedTurn(true, 0x32, 0, Spitter).Choice);
    }

    [Fact]
    public void NoLineOfFireMeansNoShotWhateverTheRolls() {
        Assert.Equal(MonsterTurnRoutines.RangedChoice.Reconsider,
            MonsterTurnRoutines.ChooseRangedTurn(false, 99, 0, Spitter).Choice);
    }

    [Fact]
    public void TheHeavyShotIsTheCommonCaseNotTheRareOne() {
        // Three rolls in four take it.
        for (var roll = 0; roll <= MonsterTurnRoutines.HeavyShotRoll; roll++) {
            Assert.Equal(MonsterTurnRoutines.RangedChoice.HeavyShot,
                MonsterTurnRoutines.ChooseRangedTurn(true, 99, roll, Spitter).Choice);
        }
        Assert.Equal(MonsterTurnRoutines.RangedChoice.LightShot,
            MonsterTurnRoutines.ChooseRangedTurn(true, 99, 3, Spitter).Choice);
    }

    [Fact]
    public void OneCreatureTakesTheHeavyShotWhateverTheRollSays() {
        Assert.Equal(MonsterTurnRoutines.RangedChoice.HeavyShot,
            MonsterTurnRoutines.ChooseRangedTurn(true, 99, 3,
                MonsterTurnRoutines.AlwaysHeavyCreature).Choice);
    }

    [Fact]
    public void EachCreatureHasItsOwnAnimationAndKnockback() {
        Assert.Equal(2, MonsterTurnRoutines.HeavyShotFor(Spitter)!.Value.ActionId);
        Assert.Equal(1, MonsterTurnRoutines.HeavyShotFor(Spitter)!.Value.KnockbackFrames);

        Assert.Equal(3, MonsterTurnRoutines.HeavyShotFor(Hurler)!.Value.ActionId);
        Assert.Equal(3, MonsterTurnRoutines.HeavyShotFor(Hurler)!.Value.KnockbackFrames);

        Assert.Equal(0x32, MonsterTurnRoutines.HeavyShotFor(Breather)!.Value.ActionId);
        Assert.Equal(0x32,
            MonsterTurnRoutines.HeavyShotFor(MonsterTurnRoutines.AlwaysHeavyCreature)!.Value.ActionId);
    }

    [Fact]
    public void TheHeavyShotHurtsWellOverTwiceAsMuchAsTheLightOne() {
        // Which branch a port takes by default therefore matters a lot.
        MonsterTurnRoutines.RangedTurn heavy = MonsterTurnRoutines.HeavyShotFor(Spitter)!.Value;
        MonsterTurnRoutines.RangedTurn light = MonsterTurnRoutines.LightShot();

        Assert.Equal(0x14, heavy.MinDamage);
        Assert.Equal(0x1d, heavy.MaxDamage);
        Assert.Equal(4, light.MinDamage);
        Assert.Equal(8, light.MaxDamage);
        Assert.True(heavy.MinDamage > light.MaxDamage * 2);
    }

    [Fact]
    public void ACreatureWithNoHeavyShotIsRefusedRatherThanGuessedAt() {
        // The original has no default in this switch and uses the values uninitialised. We do not
        // reproduce undefined behaviour.
        Assert.Null(MonsterTurnRoutines.HeavyShotFor(0x01));
        Assert.Equal(MonsterTurnRoutines.RangedChoice.LightShot,
            MonsterTurnRoutines.ChooseRangedTurn(true, 99, 0, 0x01).Choice);
    }

    [Fact]
    public void TheVolleyRoutinePrefersShootingDespiteBeingFiledWithTheMeleeHandlers() {
        // A port that reads the name and closes to melee first inverts the whole behaviour.
        Assert.True(MonsterTurnRoutines.VolleysRatherThanClosing(true, 2));
        Assert.True(MonsterTurnRoutines.VolleysRatherThanClosing(true, 9));
    }

    [Fact]
    public void ItClosesOnlyWhenAdjacentOrBlocked() {
        Assert.False(MonsterTurnRoutines.VolleysRatherThanClosing(true, 1));
        Assert.False(MonsterTurnRoutines.VolleysRatherThanClosing(false, 9));
    }

    [Fact]
    public void TheVolleyStepsFourKnockbackFramesAndHitsHard() {
        Assert.Equal(4, MonsterTurnRoutines.VolleyKnockbackFrames);
        Assert.Equal(0xf, MonsterTurnRoutines.VolleyMinDamage);
        Assert.Equal(0x22, MonsterTurnRoutines.VolleyMaxDamage);
    }

    [Fact]
    public void OnOurBuildNoTargetMeansNoTurnRatherThanACrash() {
        // The 1.02 CD release returns early; the floppy build dereferences the null.
        Assert.False(MonsterTurnRoutines.CanAct(false));
        Assert.True(MonsterTurnRoutines.CanAct(true));
    }
}
