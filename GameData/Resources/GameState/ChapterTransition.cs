namespace GameData.Resources.GameState;

/// <summary>
/// What happens around the <see cref="Data.ChapterStartData"/> apply when the game moves to the
/// next chapter — <c>go_to_chapter_impl</c> @0x41f0a.
/// </summary>
/// <remarks>
/// <b><see cref="Data.ChapterStartData"/> is the FILE; this is the PROCEDURE.</b> That type models
/// CHAPx.DAT's 23 bytes and mentions a chapter transition in passing; the steps the engine performs
/// around reading it were never written down, and several of them are not recoverable from the file.
///
/// <para><b>The chapter number is not assigned — it ARRIVES.</b> The routine reads the new chapter's
/// file straight over the head of the game state, so <c>nChapter</c> changes as a side effect of the
/// load. That is why no shipped dialog writes global 30007 and why two prior censuses of write
/// effects correctly found nothing: the write is an <c>fread</c> through a pointer.</para>
///
/// <para><b>AWAITING ITS FEATURE (TASK-145).</b> Only chapter 1 is wired; nothing transitions yet.
/// <c>GameSession.ChapterTransitionPending</c> already round-trips through the save and no code acts
/// on it, which is the seam this belongs behind.</para>
/// </remarks>
public static class ChapterTransition {
    /// <summary>
    /// The clock the new chapter starts on: <b>the NEXT day's midnight</b>, not 24 hours later.
    /// </summary>
    /// <remarks>
    /// The engine adds one day and then subtracts the remainder
    /// (<c>t += time_1_day; t -= t % time_1_day</c>), so any part-day is discarded. A chapter that
    /// ends at 18:00 and one that ends at 06:00 both begin at the same hour — which is the point,
    /// and which a port doing <c>t + oneDay</c> would lose along with every time-of-day gate in the
    /// new chapter.
    ///
    /// <para>Already-midnight is a fixed point: adding a day leaves the remainder zero, so it
    /// advances exactly one day rather than staying put.</para>
    /// </remarks>
    public static long NextChapterStart(long gameTimeIn2Seconds) {
        long advanced = gameTimeIn2Seconds + GameTime.UnitsPerDay;
        return advanced - advanced % GameTime.UnitsPerDay;
    }

    /// <summary>
    /// <b>Gold and the clock survive the chapter change; nothing else in those 16 bytes does.</b>
    /// </summary>
    /// <remarks>
    /// The routine stashes both in locals BEFORE the file read and adds them back after, because the
    /// read overwrites them with the file's template values. So CHAPx.DAT's own gold and time are
    /// dead fields on a transition — they are only live on a NEW GAME, where there is nothing to
    /// carry. A port that applied the file's values on both paths would reset the party's purse at
    /// every chapter boundary.
    ///
    /// <para>Note it is <c>+=</c>, not assignment: the file's value is ADDED to the carried one. It
    /// is zero in the shipped chapter files, so the two readings agree today — but a mod supplying a
    /// non-zero gold would grant it rather than replace the purse.</para>
    /// </remarks>
    public static int GoldAfter(int carriedGold, int fileGold) => carriedGold + fileGold;

    /// <summary>Whether the finishing gold is recorded for this chapter — <c>chapter &gt; 1</c>.</summary>
    /// <remarks>
    /// Before the file read, <c>WriteObjectToTempGam(&amp;party_gold, GoldPerChapter +
    /// (chapter-1)*4, 4)</c> stores the gold the party finished with, per chapter, in TEMP.GAM.
    /// Chapter 1 is skipped because there is no previous chapter to record.
    /// </remarks>
    public static bool RecordsFinishingGold(int chapterNumber) => chapterNumber > 1;

    /// <summary>Where that record goes, relative to the table base.</summary>
    public static int FinishingGoldOffset(int chapterNumber) => (chapterNumber - 1) * 4;

    /// <summary>Lowest global variable the transition clears.</summary>
    /// <remarks>
    /// <c>ClearGlobalVars(400..5200)</c> runs after the file apply and before the per-chapter arm,
    /// so a chapter's own setup can write into the range it just cleared. The story flags outside
    /// this window are deliberately untouched — chapter progress has to survive.
    /// </remarks>
    public const int ClearedGlobalsFirst = 400;

    /// <summary>Highest global variable the transition clears.</summary>
    public const int ClearedGlobalsLast = 5200;

    /// <summary>Whether a global is wiped by the transition.</summary>
    public static bool IsCleared(int globalKey) =>
        globalKey >= ClearedGlobalsFirst && globalKey <= ClearedGlobalsLast;

    /// <summary>
    /// Which setup arm a chapter runs — <b>read from the jump table, not from the order the arms
    /// appear in.</b>
    /// </summary>
    /// <remarks>
    /// <c>switch (chapterNumber - 2)</c> over seven cases, table at 0x42236 with entries relative to
    /// segment base 0x41c00. Two facts the arm order hides and a sequential reading would get wrong:
    /// <b>chapter 3 lands on the DEFAULT arm</b>, and <b>chapters 7 and 8 share one arm</b>.
    ///
    /// <para>The default arm is not merely a fallback — it re-tests the chapter (<c>cmp 3</c>,
    /// <c>cmp 7</c>), sets two globals, heals the whole party and shows a dialog. So chapter 3
    /// reaches it by table and is then special-cased inside it.</para>
    ///
    /// <para>Anything outside 2..8 also lands on the default arm, which is what makes a transition
    /// to an unknown chapter harmless rather than undefined.</para>
    /// </remarks>
    public static ChapterSetupArm ArmFor(int chapterNumber) => chapterNumber switch {
        2 => ChapterSetupArm.LocklearInventoryToZone15,
        4 => ChapterSetupArm.OwynAndGorathInventoryToZone12,
        5 => ChapterSetupArm.DisposeZoneZeroContainer,
        6 => ChapterSetupArm.TwoZoneZeroContainersToZone15,
        7 or 8 => ChapterSetupArm.ReadFromTempGam,
        _ => ChapterSetupArm.Default,
    };
}

/// <summary>The per-chapter setup arms of <c>go_to_chapter_impl</c>'s switch.</summary>
public enum ChapterSetupArm {
    /// <summary>Chapter 3 and anything unmapped: two global writes, heal the party, show a dialog.</summary>
    Default,

    /// <summary>Chapter 2 — Locklear's inventory into a container in zone 15 at (2,2).</summary>
    LocklearInventoryToZone15,

    /// <summary>Chapter 4 — Owyn's and Gorath's inventories into zone 12; light timer and sources.</summary>
    OwynAndGorathInventoryToZone12,

    /// <summary>Chapter 5 — take the container at zone 0 and dispose it.</summary>
    DisposeZoneZeroContainer,

    /// <summary>Chapter 6 — two zone-0 containers copied into zone 15, both disposed, two globals set.</summary>
    TwoZoneZeroContainersToZone15,

    /// <summary>Chapters 7 and 8 — read from the TEMP.GAM stream.</summary>
    ReadFromTempGam,
}
