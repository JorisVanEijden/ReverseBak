namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using System;
using Xunit;

public class CorpseDialogResolverTests {
    private static SaveGameContainerData Container(int type, uint? dialogId) =>
        new SaveGameContainerData(
            new SaveGameContainerLocationData(1, 1, 9, 195, 670423, 1059778, 0),
            (SaveGameContainerType)type, 0, 4,
            dialogId.HasValue ? SaveGameContainerDataType.Dialog : 0,
            Array.Empty<SaveGameInventoryItemData>(),
            null,
            dialogId.HasValue ? new SaveGameContainerDialogData(0, 0, dialogId.Value) : null,
            null, null, null, null);

    [Fact] public void RightClick_IsAlwaysExamine94() =>
        Assert.Equal(94, CorpseDialogResolver.Resolve(Container(5, null), isPrimary: false));

    [Fact] public void LeftClick_NoContainer_Body94() =>
        Assert.Equal(94, CorpseDialogResolver.Resolve(null, isPrimary: true));

    [Fact] public void LeftClick_LootableNoDialog_Loot78() =>
        Assert.Equal(78, CorpseDialogResolver.Resolve(Container(5, null), isPrimary: true)); // the intro corpse

    [Fact] public void LeftClick_LootableWithDialog_UsesContainerDialog() =>
        Assert.Equal(1234, CorpseDialogResolver.Resolve(Container(9, 1234), isPrimary: true));

    [Fact] public void LeftClick_NonLootableType_NotImportant154() =>
        Assert.Equal(154, CorpseDialogResolver.Resolve(Container(4, null), isPrimary: true));
}
