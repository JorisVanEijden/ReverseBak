namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The two ways a cast is billed: straight off the pool in the field, through the damage pipeline
/// in combat — and the immunities that only the combat path honours.
/// </summary>
public class SpellCostPathsTests {
    [Fact]
    public void NoOrdinaryDefenceMakesCastingCheaper() {
        // The combat cost call turns armour off and sets the shield flag, which also switches off
        // the Hocho's Haven absorb and the Skin of the Dragon negate.
        Assert.True(SpellCasting.CombatCostBypassesArmourAndShields);
    }

    [Fact]
    public void ButFourStatesWaiveACombatCastEntirely() {
        Assert.True(SpellCasting.CombatCostIsWaived(casterIsWindElemental: true,
            casterUnderDannonsDelusions: false, casterCreatureTypeIsMinusOne: false,
            casterIncapacitated: false));
        Assert.True(SpellCasting.CombatCostIsWaived(casterIsWindElemental: false,
            casterUnderDannonsDelusions: true, casterCreatureTypeIsMinusOne: false,
            casterIncapacitated: false));
        Assert.True(SpellCasting.CombatCostIsWaived(casterIsWindElemental: false,
            casterUnderDannonsDelusions: false, casterCreatureTypeIsMinusOne: true,
            casterIncapacitated: false));
        Assert.True(SpellCasting.CombatCostIsWaived(casterIsWindElemental: false,
            casterUnderDannonsDelusions: false, casterCreatureTypeIsMinusOne: false,
            casterIncapacitated: true));
    }

    [Fact]
    public void AnOrdinaryCasterPaysNormally() {
        Assert.False(SpellCasting.CombatCostIsWaived(casterIsWindElemental: false,
            casterUnderDannonsDelusions: false, casterCreatureTypeIsMinusOne: false,
            casterIncapacitated: false));
    }

    [Fact]
    public void TheFieldPathHonoursNoneOfThem() {
        // Sharing one cost function between the two would make overworld casting free in states
        // where the original still charges.
        Assert.True(SpellCasting.FieldCostIsAlwaysCharged);
    }

    [Fact]
    public void TheFieldCostStillComesOffThePoolAndDrainsTheStaff() {
        var health = new GameData.Resources.Character.ActorStat { Base = 40, Max = 40 };
        var stamina = new GameData.Resources.Character.ActorStat { Base = 40, Max = 40 };
        var context = new SpellCastContext { Chapter = 8 };

        SpellCasting.ApplyCost(context, cost: 10, health, stamina, out bool collapsed);

        Assert.False(collapsed);
        Assert.Equal(70, health.Base + stamina.Base);
    }
}
