namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The shot's weapon term — <c>combataiturn_armor_eff_stat</c>, which is not about armour.
/// </summary>
/// <remarks>
/// <b>TASK-252 recorded this as "combataiturn_armor_eff_stat is not ported; shots are slightly less
/// accurate than the original for armoured characters".</b> The direction was right and the reason
/// was not: the helper looks up the intact CROSSBOW and returns its accuracy scaled by condition, so
/// the term applies to every shooter holding a bow rather than to armoured ones.
/// </remarks>
public class RangedWeaponTermTests {
    [Fact]
    public void ItIsTheWEAPONSTerm_notTheShootersArmour() {
        // cbstat_find_intact_equip_cat(actor, 2) — and category 2 is Crossbow, the same constant
        // the shot already uses to decide what wears out.
        Assert.Equal((int)ObjectType.Crossbow, RangedExchange.ShooterWearCategory);
        Assert.NotEqual((int)ObjectType.Armor, RangedExchange.ShooterWearCategory);
    }

    [Fact]
    public void ItIsABONUS_soOmittingItMadeEveryShotWORSE() {
        // The old call passed 0. A positive term left out lowers the hit chance, so the port was
        // shooting below the original — the opposite of what "less accurate for armoured
        // characters" suggests if you read it as a penalty being skipped.
        int withWeapon = RangedExchange.EffectiveSkill(40, RangedExchange.WeaponTerm(30, 100));
        int without = RangedExchange.EffectiveSkill(40, 0);

        Assert.Equal(70, withWeapon);
        Assert.True(withWeapon > without);
    }

    [Fact]
    public void CONDITIONScalesIt_theSameWayTheMeleeWeaponTermIsScaled() {
        Assert.Equal(30, RangedExchange.WeaponTerm(30, 100));
        Assert.Equal(15, RangedExchange.WeaponTerm(30, 50));
        Assert.Equal(0, RangedExchange.WeaponTerm(30, 0));
    }

    [Fact]
    public void ThePercentageDivisionTRUNCATES_soAPoorEnoughBowContributesNothing() {
        // Integer division, as the original writes it. A 30-accuracy bow at 3% is 0, not "almost 1".
        Assert.Equal(0, RangedExchange.WeaponTerm(30, 3));
        Assert.Equal(1, RangedExchange.WeaponTerm(30, 4));
    }

    [Fact]
    public void NoWeaponMeansNoTerm() {
        // The lookup is find_INTACT_equip_cat, so a broken bow contributes nothing at all rather
        // than its accuracy at a low condition — the caller passes 0 when there is no intact one.
        Assert.Equal(0, RangedExchange.WeaponTerm(0, 100));
        Assert.Equal(40, RangedExchange.EffectiveSkill(40, 0));
    }
}
