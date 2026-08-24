namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>Which cue a melee swing plays.</summary>
public class MeleeSwingSoundTests {
    [Fact]
    public void ACreatureClassWithItsOwnCueBeatsWhateverItHolds() {
        // The class table is tested BEFORE the staff branch, so arming one of these changes nothing.
        Assert.Equal(MeleeSwingSound.CreatureHeavy,
            MeleeSwingSound.Hit(0x13, attackerHasStaff: true));
        Assert.Equal(MeleeSwingSound.CreatureLight,
            MeleeSwingSound.Hit(0x27, attackerHasStaff: true));
    }

    [Fact]
    public void EveryoneElseIsWoodOrMetalByWhatTheyHold() {
        // The default branch is the material rule, and it is the one a party member takes.
        Assert.Equal(MeleeSwingSound.HitWithoutStaff, MeleeSwingSound.Hit(0, attackerHasStaff: false));
        Assert.Equal(MeleeSwingSound.HitWithStaff, MeleeSwingSound.Hit(0, attackerHasStaff: true));
    }

    [Fact]
    public void TheCdBuildGivesFourMoreClassesTheStaffCue() {
        // *** Ours is the 1.02 CD build, so this arm is the rule and not an alternative. *** On the
        // floppy these four fall through to the material branch and would answer metal here.
        foreach (int cls in new[] { 0x1d, 0x1f, 0x20, 0x21 }) {
            Assert.Equal(MeleeSwingSound.HitWithStaff,
                MeleeSwingSound.Hit(cls, attackerHasStaff: false));
        }
    }

    [Fact]
    public void AnUnparriedMissHasItsOwnCue() {
        Assert.Equal(MeleeSwingSound.Miss,
            MeleeSwingSound.MissCue(defenderParried: false, defenderCreatureType: 0,
                defenderHasStaff: true, attackerHasStaff: true));
    }

    [Fact]
    public void TheParryClangIsWoodMetalOrOneOfEach() {
        // Read as "does this creature have a special sound" the table looks arbitrary; read as the
        // MATERIALS meeting it is obvious, and that is what these three assert together.
        Assert.Equal(MeleeSwingSound.ParryBothStaves,
            MeleeSwingSound.MissCue(true, 0, defenderHasStaff: true, attackerHasStaff: true));
        Assert.Equal(MeleeSwingSound.ParryNeitherStaff,
            MeleeSwingSound.MissCue(true, 0, defenderHasStaff: false, attackerHasStaff: false));
        Assert.Equal(MeleeSwingSound.ParryMixed,
            MeleeSwingSound.MissCue(true, 0, defenderHasStaff: false, attackerHasStaff: true));
        Assert.Equal(MeleeSwingSound.ParryMixed,
            MeleeSwingSound.MissCue(true, 0, defenderHasStaff: true, attackerHasStaff: false));
    }

    [Fact]
    public void OneClassAlwaysClangsAsWood() {
        // Tested beside the both-staves case, so it wins over the mixed and metal readings.
        Assert.Equal(MeleeSwingSound.ParryBothStaves,
            MeleeSwingSound.MissCue(true, MeleeSwingSound.AlwaysWoodParryClass,
                defenderHasStaff: false, attackerHasStaff: false));
    }

    [Fact]
    public void ParryMixedAndHitWithStaffAreTheSameCueOnPurpose() {
        // Not a copy/paste slip: the original pushes 0x42 in both places. Pinned so nobody
        // "fixes" one of them into a distinct id.
        Assert.Equal(MeleeSwingSound.HitWithStaff, MeleeSwingSound.ParryMixed);
    }
}
