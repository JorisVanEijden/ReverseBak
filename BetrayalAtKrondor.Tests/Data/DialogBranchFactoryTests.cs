namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;

using ResourceExtraction.Extractors.Dialog;

using Xunit;

public class DialogBranchFactoryTests {
    [Fact]
    public void Key0_IsDefaultBranch() {
        var b = DialogBranchFactory.Build(false, 0, 1, 0xFFFF, 0x10);
        Assert.IsType<DefaultBranch>(b);
    }

    [Fact]
    public void ChoiceMenu_IsKeywordChoice() {
        var b = DialogBranchFactory.Build(true, 7, 1, 0xFFFF, 0x10);
        Assert.IsType<KeywordChoiceBranch>(Assert.IsType<KeywordChoiceBranch>(b));
    }

    [Fact]
    public void StoryFlag_IsConditionalBranchWithFlagCondition() {
        var b = Assert.IsType<ConditionalBranch>(DialogBranchFactory.Build(false, 278, 1, 0xFFFF, 528));
        var flag = Assert.IsType<FlagCondition>(b.Condition);
        Assert.Equal(278, flag.Flag);
        Assert.True(flag.Set);
        Assert.Equal(528, b.TargetOffset);
    }

    [Fact]
    public void ItemKey_IsConditionalBranchWithHasItem() {
        var b = Assert.IsType<ConditionalBranch>(DialogBranchFactory.Build(false, 50137, 1, 0xFFFF, 0x10));
        Assert.IsType<HasItemCondition>(b.Condition);
    }

    [Fact]
    public void Flags2Masked_IsConditionalBranchWithComposite() {
        // group 0 (key 56000), matchMask sets bits 0 and 1, selector=All, any chapter
        ushort unknown3 = 0x0300; // matchMask=0x03 (high byte), xorMask=0x00 (low byte)
        ushort unknown4 = 0xFF01; // chapterMask=0xFF (high byte=any), selector=1 (low byte=All)
        var b = Assert.IsType<ConditionalBranch>(DialogBranchFactory.Build(false, 56000, unknown3, unknown4, 0x10));
        var all = Assert.IsType<AllOf>(b.Condition);
        Assert.Equal(2, all.Conditions.Count);
        Assert.All(all.Conditions, c => Assert.IsType<FlagCondition>(c));
    }

    [Fact]
    public void Flags2Masked_WithChapterRestriction_AddsInChapters() {
        ushort unknown3 = 0x0100; // matchMask=0x01 -> one bit
        ushort unknown4 = 0x0901; // chapterMask=0x09 (chapters 1 and 4), selector=1
        var b = Assert.IsType<ConditionalBranch>(DialogBranchFactory.Build(false, 56000, unknown3, unknown4, 0x10));
        var all = Assert.IsType<AllOf>(b.Condition);
        Assert.Contains(all.Conditions, c => c is InChapters ch && ch.Chapters.Contains(1) && ch.Chapters.Contains(4));
    }
}
