namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using ResourceExtraction;
using Xunit;

public class InteractionProfileTableTests {
    [Fact]
    public void Corpse16_ResolvesToContainerBehaviorAndProfile() {
        Assert.True(InteractionProfileTable.TryGet(16, out string behavior, out InteractionProfile p));
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
        Assert.False(InteractionProfileTable.TryGet(99, out _, out _));
}
