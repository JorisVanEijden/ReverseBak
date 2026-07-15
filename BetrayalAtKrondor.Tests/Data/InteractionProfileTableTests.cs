namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using GameData.Resources.World;
using ResourceExtraction;
using Xunit;

public class InteractionProfileTableTests {
    [Fact]
    public void Corpse16_ResolvesToContainerBehaviorAndProfile() {
        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.Corpse, out string behavior, out InteractionProfile p));
        Assert.Equal("container", behavior);
        Assert.Equal(new InteractionRange(7000, 2500), p.Range);
        Assert.Contains(SaveGameContainerType.Corpse, p.ActionableContainerTypes);
        Assert.Contains(SaveGameContainerType.ScriptedLoot, p.ActionableContainerTypes);
        Assert.Equal(94, p.ExamineDialogId);
        Assert.Equal(78, p.ActionDialogId);
        Assert.Equal(154, p.NotActionableDialogId);
        Assert.True(p.OpensLoot);
        Assert.False(p.HasLock);
    }

    [Fact]
    public void UnknownType_ReturnsFalse() =>
        Assert.False(InteractionProfileTable.TryGet(WorldEntityType.Well, out _, out _));

    [Fact]
    public void Container6_ResolvesToChestProfile() {
        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.Container,
            out string behavior, out InteractionProfile p));
        Assert.Equal("container", behavior);
        Assert.Contains(SaveGameContainerType.Chest, p.ActionableContainerTypes);
        Assert.Contains(SaveGameContainerType.ScriptedLoot, p.ActionableContainerTypes);
        Assert.True(p.OpensLoot);
        Assert.True(p.HasLock);
        Assert.Null(p.Range);
    }
}
