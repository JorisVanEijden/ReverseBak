namespace BetrayalAtKrondor.Tests.GameState;

using GameData.Resources.GameState;
using Xunit;

/// <summary>The procedure around a chapter change — <c>go_to_chapter_impl</c> @0x41f0a.</summary>
public class ChapterTransitionTests {
    /// <summary>
    /// <b>A chapter starts at the next day's MIDNIGHT, whatever hour the last one ended.</b>
    /// </summary>
    /// <remarks>
    /// Two very different end times must land on the same start, which is what distinguishes the
    /// engine's `+= day; -= remainder` from a plain `+ day`. Asserting one end time would pass for
    /// both.
    /// </remarks>
    [Theory]
    [InlineData(0)]                              // already midnight
    [InlineData(GameTime.UnitsPerHour * 6)]      // 06:00
    [InlineData(GameTime.UnitsPerHour * 18)]     // 18:00
    [InlineData(GameTime.UnitsPerDay - 1)]       // one unit to midnight
    public void EveryEndTimeInADayStartsTheNextChapterAtTheSameMidnight(long endedAt) {
        long start = ChapterTransition.NextChapterStart(endedAt);

        Assert.Equal(0, start % GameTime.UnitsPerDay);
        Assert.Equal(GameTime.UnitsPerDay, start);
    }

    /// <summary>And it always moves forward — midnight is not a fixed point.</summary>
    [Fact]
    public void AChapterEndingExactlyAtMidnightStillAdvancesADay() {
        long midnight = GameTime.UnitsPerDay * 3;

        Assert.Equal(GameTime.UnitsPerDay * 4, ChapterTransition.NextChapterStart(midnight));
    }

    /// <summary>
    /// The file's gold is ADDED to the carried purse, not substituted for it.
    /// </summary>
    /// <remarks>
    /// Shipped chapter files carry zero, so add and replace agree on the real data — which is why
    /// the distinction has to be pinned here rather than discovered from a save. A mod supplying a
    /// non-zero value grants it.
    /// </remarks>
    [Fact]
    public void TheCarriedPurseSurvivesAndTheFilesGoldIsAddedToIt() {
        Assert.Equal(1234, ChapterTransition.GoldAfter(carriedGold: 1234, fileGold: 0));
        Assert.Equal(1284, ChapterTransition.GoldAfter(carriedGold: 1234, fileGold: 50));
    }

    [Fact]
    public void FinishingGoldIsRecordedForEveryChapterButTheFirst() {
        Assert.False(ChapterTransition.RecordsFinishingGold(1), "no previous chapter to record");
        for (var chapter = 2; chapter <= 9; chapter++) {
            Assert.True(ChapterTransition.RecordsFinishingGold(chapter), $"chapter {chapter}");
        }
        // Stride 4 from the table base, indexed by the chapter being LEFT.
        Assert.Equal(4, ChapterTransition.FinishingGoldOffset(2));
        Assert.Equal(28, ChapterTransition.FinishingGoldOffset(8));
    }

    /// <summary>The cleared window is bounded at both ends — story flags outside it survive.</summary>
    [Fact]
    public void OnlyGlobalsInTheClearedWindowAreWiped() {
        Assert.False(ChapterTransition.IsCleared(399));
        Assert.True(ChapterTransition.IsCleared(400));
        // *** 5200 IS NOT CLEARED, AND THIS TEST ASSERTED THAT IT WAS. ***
        // ClearGlobalVars_400_5200 @0x74d9b is `for (i = 0; i < 4800; i++) SetGlobalValue(400+i, 0)`
        // — 400 through 5199. 5200 is the first key of the NEXT window, which has clears of its own
        // (ClearGlobal5200_5209, ClearGlobal5210_5219), so wiping it here reaches into a band the
        // chapter transition deliberately leaves alone.
        Assert.True(ChapterTransition.IsCleared(5199));
        Assert.False(ChapterTransition.IsCleared(5200));
        Assert.False(ChapterTransition.IsCleared(5201));
        // The event-field and high-bitmap regions are far outside it; chapter progress must survive.
        Assert.False(ChapterTransition.IsCleared(30007));
        Assert.False(ChapterTransition.IsCleared(56000));
    }

    /// <summary>
    /// <b>The two mappings a sequential reading of the arms gets wrong.</b>
    /// </summary>
    /// <remarks>
    /// The arms appear in the binary in an order that does not match the chapters; only the jump
    /// table says which is which. Chapter 3 skips the arm physically next to chapter 2's and lands
    /// on the default; chapters 7 and 8 share a single arm.
    /// </remarks>
    [Fact]
    public void ChapterThreeUsesTheDefaultArmAndSevenAndEightShareOne() {
        Assert.Equal(ChapterSetupArm.Default, ChapterTransition.ArmFor(3));
        Assert.Equal(ChapterTransition.ArmFor(7), ChapterTransition.ArmFor(8));
        Assert.Equal(ChapterSetupArm.ReadFromTempGam, ChapterTransition.ArmFor(7));
    }

    [Fact]
    public void TheMappedChaptersEachGetTheirOwnArm() {
        Assert.Equal(ChapterSetupArm.LocklearInventoryToZone15, ChapterTransition.ArmFor(2));
        Assert.Equal(ChapterSetupArm.OwynAndGorathInventoryToZone12, ChapterTransition.ArmFor(4));
        Assert.Equal(ChapterSetupArm.DisposeZoneZeroContainer, ChapterTransition.ArmFor(5));
        Assert.Equal(ChapterSetupArm.TwoZoneZeroContainersToZone15, ChapterTransition.ArmFor(6));
    }

    /// <summary>Outside 2..8 is harmless rather than undefined — the switch's own default.</summary>
    [Fact]
    public void AnUnmappedChapterFallsToTheDefaultArm() {
        Assert.Equal(ChapterSetupArm.Default, ChapterTransition.ArmFor(1));
        Assert.Equal(ChapterSetupArm.Default, ChapterTransition.ArmFor(9));
        Assert.Equal(ChapterSetupArm.Default, ChapterTransition.ArmFor(0));
    }

    [Fact]
    public void NothingWritesTheDestinationChapter_SoTheCallerADDSOne() {
        // *** THE BUG THIS PINS. *** GameFlow passed the session's own chapter into GoToChapter,
        // on a comment claiming "the same dialog writes Field.Chapter". No shipped dialog does:
        // across all 32 DDX files the SetVarEffect writes cover Vars 0, 4, 14, 15, 16 and 17 —
        // never Var 7. The original stores the CURRENT chapter at the exit
        // (g_nChapterAtLoopExit = g_gameState.nChapter, WORLDLP.C:403) and each consumer adds one
        // (GMAIN.C:112, 205, 753). Measured live before the fix: raising the flag in chapter 1
        // landed back at chapter 1 zone 1 tile (10,16), not chapter 2 zone 11 tile (11,11).
        Assert.Equal(2, ChapterTransition.NextChapter(1));
        Assert.Equal(9, ChapterTransition.NextChapter(8));
    }

    [Fact]
    public void ONLYRequestONEAdvances_BecauseTwoIsADifferentExit() {
        // worldloop branches 1 -> exit_mode 5 (advance) and anything else -> exit_mode 7, which
        // plays a cutscene and changes no chapter (WORLDLP.C:402-408, GMAIN.C:753 vs 758). Both
        // values ship: six dialogs write 1, DIAL_Z30:109569 writes 2. "Non-zero means advance"
        // would move the story on from that one.
        Assert.True(ChapterTransition.Advances(ChapterTransition.AdvanceRequest));
        Assert.True(ChapterTransition.Advances(1));
        Assert.False(ChapterTransition.Advances(2));
        Assert.False(ChapterTransition.Advances(0));
    }

    [Fact]
    public void FinishingChapterNINEEndsTheGame_ThereIsNoChapterTen() {
        // GMAIN.C:748-751 takes the main-menu arm instead of playing chapter ten's opening, and
        // CHAP10.DAT does not ship — OriginalGame carries CHAP1..CHAP9 only.
        Assert.Equal(9, ChapterTransition.LastChapter);
        Assert.False(ChapterTransition.EndsTheGame(8));
        Assert.True(ChapterTransition.EndsTheGame(9));

        // Defensive: a save that somehow carries a higher chapter must not ask for CHAP10.DAT.
        Assert.True(ChapterTransition.EndsTheGame(10));
    }
}
