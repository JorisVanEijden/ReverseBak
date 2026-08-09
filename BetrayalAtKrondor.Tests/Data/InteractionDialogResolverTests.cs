namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using System;
using Xunit;

public class InteractionDialogResolverTests {
    // The corpse profile as data (mirrors the extractor's InteractionProfileTable corpse entry).
    private static readonly InteractionProfile Corpse = new() {
        Range = new InteractionRange(7000, 2500),
        ActionableContainerTypes = new[] { SaveGameContainerType.Corpse, SaveGameContainerType.ScriptedLoot },
        ExamineDialogId = 94, ActionDialogId = 78, NotActionableDialogId = 154,
        OpensLoot = true, HasLock = false,
    };

    private static SaveGameContainerData Container(SaveGameContainerType type, uint? dialogId) =>
        new SaveGameContainerData(
            new SaveGameContainerLocationData(1, 1, 9, 195, 670423, 1059778, 0),
            type, 0, 4,
            dialogId.HasValue ? SaveGameContainerDataType.Dialog : 0,
            Array.Empty<SaveGameInventoryItemData>(), null,
            dialogId.HasValue ? new SaveGameContainerDialogData(0, 0, dialogId.Value) : null,
            null, null, null, null);

    [Fact] public void RightClick_IsAlwaysExamine() =>
        Assert.Equal(94, InteractionDialogResolver.Resolve(Corpse, Container(SaveGameContainerType.Corpse, null), isPrimary: false));

    /// <summary>
    /// No container at the location answers the same as a container of the wrong type. Every DOS
    /// handler reaches its not-actionable label from the null test and the type test alike —
    /// handle_Corpse @0x76a98 and @0x76aa9 both jump to the ddx-154 branch — so "nothing here" and
    /// "wrong thing here" are one case, not two.
    ///
    /// <para>This asserted the EXAMINE dialog until 2026-08-09, which is a different string in
    /// every handler and simply is not what the original shows.</para>
    /// </summary>
    [Fact] public void LeftClick_NoContainer_IsTheSameAsAWrongTypeContainer() {
        int noContainer = InteractionDialogResolver.Resolve(Corpse, null, isPrimary: true);
        int wrongType = InteractionDialogResolver.Resolve(
            Corpse, Container(SaveGameContainerType.FixedWorldItem, null), isPrimary: true);

        Assert.Equal(154, noContainer);
        Assert.Equal(wrongType, noContainer);
        Assert.NotEqual(Corpse.ExamineDialogId, noContainer);
    }

    /// <summary>
    /// The well is the counter-example that stops the fix above being read as "null always means
    /// 154": handle_Well's null and wrong-type paths both jump to its DRINK dialog (@0x78be6 /
    /// @0x78bf0 -> `useWell`), so a well with nothing placed under it still works. The resolver
    /// must take that from the profile rather than from a constant.
    /// </summary>
    [Fact] public void LeftClick_NoContainer_FollowsTheProfile_NotAFixed154() {
        var well = new InteractionProfile {
            ActionableContainerTypes = new[] { SaveGameContainerType.FixedWorldItem },
            ExamineDialogId = 189, ActionDialogId = 188, NotActionableDialogId = 188,
        };
        Assert.Equal(188, InteractionDialogResolver.Resolve(well, null, isPrimary: true));
    }

    /// <summary>
    /// A describe-only handler (Ashes, RockPile, Corn) never looks a container up at all. Modelled
    /// as an empty actionable list, which must give the one left-click line for EVERY container
    /// state — that equivalence is what makes the model exact rather than approximate.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(SaveGameContainerType.FixedWorldItem)]
    [InlineData(SaveGameContainerType.Corpse)]
    [InlineData(SaveGameContainerType.ScriptedLoot)]
    public void DescribeOnly_AnswersTheSameWhateverIsAtTheLocation(SaveGameContainerType? type) {
        var ashes = new InteractionProfile {
            ActionableContainerTypes = Array.Empty<SaveGameContainerType>(),
            ExamineDialogId = 166, ActionDialogId = 165, NotActionableDialogId = 165,
        };
        SaveGameContainerData? container = type.HasValue ? Container(type.Value, 1234) : null;

        Assert.Equal(165, InteractionDialogResolver.Resolve(ashes, container, isPrimary: true));
        Assert.Equal(166, InteractionDialogResolver.Resolve(ashes, container, isPrimary: false));
    }

    [Fact] public void LeftClick_ActionableNoDialog_Action() =>
        Assert.Equal(78, InteractionDialogResolver.Resolve(Corpse, Container(SaveGameContainerType.Corpse, null), isPrimary: true));

    [Fact] public void LeftClick_ActionableWithDialog_UsesContainerDialog() =>
        Assert.Equal(1234, InteractionDialogResolver.Resolve(Corpse, Container(SaveGameContainerType.ScriptedLoot, 1234), isPrimary: true));

    [Fact] public void LeftClick_NonActionableType_NotActionable() =>
        Assert.Equal(154, InteractionDialogResolver.Resolve(Corpse, Container(SaveGameContainerType.FixedWorldItem, null), isPrimary: true));
}
