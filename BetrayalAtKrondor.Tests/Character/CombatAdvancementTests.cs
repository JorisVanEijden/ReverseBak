namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Use-based advancement in a melee exchange. The headline is a negative: there is no kill XP, so
/// these awards are the whole of what combat pays out.
/// </summary>
public class CombatAdvancementTests {
    private static ActorStat Skill(byte value = 20, byte max = 100) =>
        new ActorStat { Base = value, Max = max };

    [Fact]
    public void BeingAttackedTrainsTheDefender() {
        // The defender is paid for standing there, before any roll and win or lose.
        ActorStat defense = Skill();
        ActorStat melee = Skill();

        CombatAdvancement.OnMeleeDeclared(defense, melee);

        Assert.True(defense.Experience > 0 || defense.Base > 20,
            "the defender should have banked progress toward Defense");
    }

    [Fact]
    public void SwingingTrainsTheAttackerEvenOnAMiss() {
        ActorStat melee = Skill();

        CombatAdvancement.OnMeleeDeclared(null, melee);

        Assert.True(melee.Experience > 0 || melee.Base > 20);
    }

    [Fact]
    public void ALandedHitPaysMeleeASecondTime() {
        // Once for trying (declared), once for connecting (hit) — so a hit is worth double.
        ActorStat tryingOnly = Skill();
        ActorStat alsoConnected = Skill();

        CombatAdvancement.OnMeleeDeclared(null, tryingOnly);

        CombatAdvancement.OnMeleeDeclared(null, alsoConnected);
        CombatAdvancement.OnMeleeHit(alsoConnected, Skill());

        Assert.True(Progress(alsoConnected) > Progress(tryingOnly),
            $"connected {Progress(alsoConnected)} should exceed trying-only {Progress(tryingOnly)}");
    }

    [Fact]
    public void ALandedHitAlsoTrainsStrength() {
        ActorStat strength = Skill();

        CombatAdvancement.OnMeleeHit(Skill(), strength);

        Assert.True(Progress(strength) > 0);
    }

    [Fact]
    public void RepetitionIsWhatAdvancesASkill() {
        // Each award banks a sub-unit remainder; only accumulation produces whole points. One swing
        // moving the number would be wrong.
        ActorStat melee = Skill();
        int startingBase = melee.Base;

        for (var swing = 0; swing < 200; swing++) {
            CombatAdvancement.OnMeleeDeclared(null, melee);
        }

        Assert.True(melee.Base > startingBase,
            $"200 swings should have produced whole points; base is still {melee.Base}");
    }

    [Fact]
    public void AStatTheActorDoesNotHaveIsInert() {
        // Max 0 means the actor has no such skill — no award, no banked remainder.
        var absent = new ActorStat { Base = 0, Max = 0 };

        CombatAdvancement.OnMeleeDeclared(absent, null);

        Assert.Equal(0, Progress(absent));
    }

    [Fact]
    public void NullStatsAreSkippedRatherThanThrowing() {
        CombatAdvancement.OnMeleeDeclared(null, null);
        CombatAdvancement.OnMeleeHit(null, null);
        CombatAdvancement.OnSpellHit(null);
        CombatAdvancement.OnSpellCast(null);
    }

    [Fact]
    public void ASuccessfulCastTrainsCasting() {
        ActorStat casting = Skill();

        CombatAdvancement.OnSpellHit(casting);

        Assert.True(Progress(casting) > 0);
    }

    [Fact]
    public void CastingAtAllTrainsCastingEvenWhenItMisses() {
        // The first award is unconditional, exactly as the attacker's is on a declared melee swing.
        ActorStat casting = Skill();

        CombatAdvancement.OnSpellCast(casting);

        Assert.True(Progress(casting) > 0);
    }

    [Fact]
    public void ALandedCastPaysCastingTwiceOverJustAsMeleeDoes() {
        // Once for trying, once for connecting. Treating the spell award as hit-only under-rewards
        // every successful cast by half and pays nothing for a miss.
        ActorStat missed = Skill();
        ActorStat landed = Skill();

        CombatAdvancement.OnSpellCast(missed);

        CombatAdvancement.OnSpellCast(landed);
        CombatAdvancement.OnSpellHit(landed);

        Assert.True(Progress(landed) > Progress(missed));
        Assert.True(Progress(missed) > 0);
    }


    private static int Progress(ActorStat stat) => stat.Base * 0x100 + stat.Experience;
}
