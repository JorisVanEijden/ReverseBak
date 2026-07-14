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

    [Fact] public void LeftClick_NoContainer_Examine() =>
        Assert.Equal(94, InteractionDialogResolver.Resolve(Corpse, null, isPrimary: true));

    [Fact] public void LeftClick_ActionableNoDialog_Action() =>
        Assert.Equal(78, InteractionDialogResolver.Resolve(Corpse, Container(SaveGameContainerType.Corpse, null), isPrimary: true));

    [Fact] public void LeftClick_ActionableWithDialog_UsesContainerDialog() =>
        Assert.Equal(1234, InteractionDialogResolver.Resolve(Corpse, Container(SaveGameContainerType.ScriptedLoot, 1234), isPrimary: true));

    [Fact] public void LeftClick_NonActionableType_NotActionable() =>
        Assert.Equal(154, InteractionDialogResolver.Resolve(Corpse, Container(SaveGameContainerType.FixedWorldItem, null), isPrimary: true));
}
