namespace BetrayalAtKrondor.Tests.GameState;

using GameData.Resources.GameState;
using Xunit;

public class ChapterFinishingGoldTests {
    [Fact]
    public void TheRecordSitsWhereBothReadsLook() {
        // SAVEGAME.C:155 writes (chapter - 1) * 4 + 0x12f7; :231 reads (chapter - 2) * 4 + 0x12f7.
        Assert.Equal(0x12f7, ChapterFinishingGold.BodyOffset);
        Assert.Equal(36, ChapterFinishingGold.SaveSize);
    }

    [Fact]
    public void ChapterOneRecordsNothing() {
        var gold = new ChapterFinishingGold();
        gold.RecordStartOf(1, 500);
        Assert.Equal(0, gold.AtStartOf(1));
    }

    [Fact]
    public void ChaptersFiveToEightRestoreThePreviousChaptersStartingPurse() {
        var gold = new ChapterFinishingGold();
        gold.RecordStartOf(4, 650);
        Assert.Equal(650, gold.PurseAfterRestore(5, 0));
        gold.RecordStartOf(5, 1200);
        gold.RecordStartOf(6, 40);
        Assert.Equal(1200, gold.PurseAfterRestore(6, 999));
        Assert.Equal(40, gold.PurseAfterRestore(7, 999));
    }

    [Fact]
    public void OtherChaptersKeepThePurse() {
        var gold = new ChapterFinishingGold();
        gold.RecordStartOf(3, 300);
        Assert.Equal(777, gold.PurseAfterRestore(4, 777));
        Assert.Equal(777, gold.PurseAfterRestore(9, 777));
        Assert.False(ChapterFinishingGold.RestoresGold(9));
    }

    [Fact]
    public void TheRecordSurvivesASaveAndALoad() {
        var gold = new ChapterFinishingGold();
        gold.RecordStartOf(3, 123456);
        var body = new byte[ChapterFinishingGold.BodyOffset + ChapterFinishingGold.SaveSize];
        Assert.True(gold.Save(body));
        Assert.Equal(123456, System.BitConverter.ToInt32(body, 0x12f7 + 8));

        var reloaded = new ChapterFinishingGold();
        reloaded.Load(body);
        Assert.Equal(123456, reloaded.AtStartOf(3));
        Assert.Equal(0, reloaded.AtStartOf(2));
    }
}
