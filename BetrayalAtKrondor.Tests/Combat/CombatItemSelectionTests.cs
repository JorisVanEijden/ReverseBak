namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Which items the inventory screen hands to the arena — the two <c>combat_result</c> sites in
/// <c>itemuse_dispatch_on_target</c> (TASK-263).
/// </summary>
public class CombatItemSelectionTests {
    [Fact]
    public void OutsideAFightNothingIsHandedOver() =>
        Assert.Null(CombatItemUse.CommandIdFrom(ObjectType.CombatItem, 0x0b,
            inCombat: false, equipped: true, intact: true));

    /// <summary>The combat-item category qualifies unconditionally — no equip, no condition.</summary>
    [Fact]
    public void ACombatItemQualifiesWhateverItsState() {
        Assert.Equal(0x0b, CombatItemUse.CommandIdFrom(ObjectType.CombatItem, 0x0b,
            inCombat: true, equipped: false, intact: false));
        Assert.Equal(0x33, CombatItemUse.CommandIdFrom(ObjectType.CombatItem, 0x33,
            inCombat: true, equipped: true, intact: true));
    }

    /// <summary>
    /// A staff qualifies only EQUIPPED and UNBROKEN — the guard is
    /// <c>(item-&gt;flags &amp; 0x40) &amp;&amp; item-&gt;condition != 0</c>.
    /// </summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void AStaffInThePackOrBrokenDoesNothing(bool equipped, bool intact) =>
        Assert.Null(CombatItemUse.CommandIdFrom(ObjectType.Staff, 0x02,
            inCombat: true, equipped, intact));

    /// <summary>
    /// Only staves 2 and 4 reach the arena, which is why the arm list holds exactly those two and
    /// no other staff.
    /// </summary>
    [Theory]
    [InlineData(0x02, true)]
    [InlineData(0x04, true)]
    [InlineData(0x03, false)]
    [InlineData(0x10, false)]
    public void OnlyTwoStavesQualify(int objectId, bool qualifies) {
        int? id = CombatItemUse.CommandIdFrom(ObjectType.Staff, objectId,
            inCombat: true, equipped: true, intact: true);

        Assert.Equal(qualifies ? objectId : (int?)null, id);
    }

    /// <summary>
    /// THE REFUSALS ARE NOT DUPLICATED HERE. The Lightning Staff underground and the Idol in
    /// chapter 8 are refused by BOTH the screen and the arena in the original; <c>Works</c> carries
    /// them, so selection stays silent about them and the caller runs both.
    /// </summary>
    [Fact]
    public void SelectionDoesNotRepeatTheRefusalsWorksAlreadyOwns() {
        Assert.Equal(0x02, CombatItemUse.CommandIdFrom(ObjectType.Staff, 0x02,
            inCombat: true, equipped: true, intact: true));

        CombatItemUse.Use? lightningStaff = CombatItemUse.For(0x02);
        Assert.NotNull(lightningStaff);
        Assert.False(CombatItemUse.Works(lightningStaff.Value, underground: true, chapter: 1));

        CombatItemUse.Use? idol = CombatItemUse.For(0x0c);
        Assert.NotNull(idol);
        Assert.False(CombatItemUse.Works(idol.Value, underground: false, chapter: 8));
    }
}
