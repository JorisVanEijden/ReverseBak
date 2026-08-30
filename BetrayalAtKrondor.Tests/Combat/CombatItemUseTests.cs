namespace BetrayalAtKrondor.Tests.Combat;

using System.Linq;
using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Using an item during a fight — <c>combat_arena_resume_dispatch</c>.
/// </summary>
/// <remarks>
/// <b>Found while chasing TASK-109's "the storm amplifier has no source on our side yet".</b> It has
/// one, and it is not weather: <c>g_bStormAmplify</c> is set by <c>case 0x0d</c>, which is object 13,
/// the <b>Infinity Pool</b>. canassa's name for the flag describes something the routine does not do.
/// </remarks>
public class CombatItemUseTests {
    [Fact]
    public void TheCommandIdIsTheOBJECTId_notAMenuAction() {
        // The shared tail consumes itemtbl_inv_consume_one_by_kind(inv, command_id) — the switch
        // value itself. So these ten arms are object ids, and a table keyed by anything else would
        // consume the wrong item.
        Assert.Equal(10, CombatItemUse.All.Count);
        Assert.Equal(CombatItemUse.All.Count,
            CombatItemUse.All.Select(u => u.ObjectId).Distinct().Count());
        Assert.NotNull(CombatItemUse.For(CombatItemUse.InfinityPoolObjectId));
        Assert.Null(CombatItemUse.For(1));
    }

    [Fact]
    public void TheINFINITYPOOLIsWhatTheSurchargeFlagIs() {
        // *** The finding. *** SpellCostModifiers.Effective's `surcharged` had no source because it
        // was being looked for in the world rather than in an item.
        CombatItemUse.Use pool = CombatItemUse.For(0x0d).Value;

        Assert.Equal(CombatItemUse.Effect.AmplifiedCast, pool.Effect);
        Assert.Equal(CombatItemUse.Targeting.None, pool.Targeting);

        // Same shift, same rounding: intensity += intensity >> 1 in cspell_resolve_cast.
        Assert.Equal(15, SpellCostModifiers.Effective(10, surcharged: true, targetIsWeak: false));
        Assert.Equal(10, SpellCostModifiers.Effective(10, surcharged: false, targetIsWeak: false));
    }

    [Fact]
    public void TWOArmsRefuseAndConsumeNOTHING() {
        // *** Both return BEFORE the shared tail, so refusal and consumption are one question. ***
        // Consuming first and checking after would eat the Idol in chapter 8 and the Lightning Staff
        // in every dungeon.
        CombatItemUse.Use idol = CombatItemUse.For(0x0c).Value;
        CombatItemUse.Use staff = CombatItemUse.For(0x02).Value;

        Assert.False(CombatItemUse.Works(idol, underground: false, chapter: 8));
        Assert.True(CombatItemUse.Works(idol, underground: false, chapter: 7));
        Assert.True(CombatItemUse.Works(idol, underground: true, chapter: 1),
            "the Idol's guard is about the chapter, not the place");

        Assert.False(CombatItemUse.Works(staff, underground: true, chapter: 1));
        Assert.True(CombatItemUse.Works(staff, underground: false, chapter: 8),
            "and the Staff's is about the place, not the chapter");
    }

    [Fact]
    public void EverythingElseWorksWherever() {
        foreach (CombatItemUse.Use use in CombatItemUse.All) {
            if (use.ObjectId == 0x0c || use.ObjectId == 0x02) {
                continue;
            }
            Assert.True(CombatItemUse.Works(use, underground: true, chapter: 8));
        }
    }

    [Fact]
    public void TheDoorIsTheINVENTORYScreen_notAHUDButton() {
        // *** The wiring fact worth having before anyone builds the entry point. ***
        // combat_arena_suspend_char_screen is the ONLY caller, and it passes
        // cmbinv_inventory_screen_run's return value straight in. So the command that opens this is
        // CharacterScreen; there is no Use command in the combat menu at all.
        Assert.True(CombatItemUse.EnteredFromTheInventoryScreen);
        Assert.Equal(CombatCommands.Command.CharacterScreen,
            CombatCommands.For(CombatCommands.CharacterScreenId));
        Assert.Equal(CombatCommands.Command.None, CombatCommands.For(0x0d));
    }

    [Fact]
    public void NoItemUsedMatchesNoArm() {
        // The shift-key branch never assigns the command id in the reconstruction. Whatever we pass
        // for "the screen closed without using anything" must match nothing — asserted rather than
        // assumed, because 0 being unused is a property of the table.
        Assert.Null(CombatItemUse.For(CombatItemUse.NoItemUsed));
    }

    [Fact]
    public void RORICSSealBackfiresThreeTimesInTen() {
        CombatItemUse.Use seal = CombatItemUse.For(0x0f).Value;

        Assert.Equal(30, seal.BackfirePercent);
        Assert.Equal(CombatItemUse.Targeting.AnyEnemy, seal.Targeting);
        // The target is still picked first, so the backfire is not a way to aim at yourself.
        Assert.All(CombatItemUse.All.Where(u => u.ObjectId != 0x0f),
            u => Assert.Equal(0, u.BackfirePercent));
    }

    [Fact]
    public void TheHORNSummonsTWICE_whichAOnePerUseReadingWouldHalve() {
        Assert.Equal(2, CombatItemUse.For(0x0b).Value.SummonCount);
        Assert.Equal(1, CombatItemUse.For(0x09).Value.SummonCount);
        Assert.NotEqual(CombatItemUse.For(0x0b).Value.SummonCreature,
            CombatItemUse.For(0x09).Value.SummonCreature);
    }

    [Fact]
    public void TWOItemsNeedTheTargetADJACENT_andTheRestDoNot() {
        // combat_arena_pick_target_actor's argument adds combatgrid_actors_ortho_adj. Getting it
        // backwards makes a thrown Powder Bag reach across the arena.
        int[] adjacent = CombatItemUse.All
            .Where(u => u.Targeting == CombatItemUse.Targeting.AdjacentEnemy)
            .Select(u => u.ObjectId).OrderBy(id => id).ToArray();

        Assert.Equal(new[] { 0x32, 0x33 }, adjacent);
    }

    [Fact]
    public void EVERYItemCostIsNEGATIVE_whichIsWhyTheSignIsAFlag() {
        // SpellCostModifiers treats a negative cost as sign-plus-magnitude rather than a negative
        // quantity, and these literals are where that convention comes from.
        foreach (CombatItemUse.Use use in CombatItemUse.All
                     .Where(u => u.Effect == CombatItemUse.Effect.CastSpell)) {
            Assert.True(use.Cost < 0, $"object {use.ObjectId:X} passes {use.Cost}");
            Assert.True(SpellCostModifiers.IsNegated(use.Cost));
        }
    }
}
