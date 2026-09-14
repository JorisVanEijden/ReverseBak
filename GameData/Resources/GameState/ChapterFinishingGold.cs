namespace GameData.Resources.GameState;

/// <summary>
/// The party's gold at the start of each chapter — the record <c>savegame_chapter_start_dispatch</c>
/// writes on the way in and reads back for chapters 5 to 8 (canassa SAVEGAME.C:152-156, :229-231).
/// </summary>
/// <remarks>
/// <b>Written before the CHAP file is applied, read after it.</b> Entering chapter N (N &gt; 1) stores the
/// purse at <see cref="ChapterTransition.FinishingGoldOffset"/>(N); entering 5, 6, 7 or 8 then replaces the
/// purse with the figure stored when chapter N-1 began — so chapter 5 gets back what the party had when
/// chapter 4's prison zeroed it.
/// </remarks>
public sealed class ChapterFinishingGold {
    /// <summary>One entry per chapter, 1 to <see cref="ChapterTransition.LastChapter"/>.</summary>
    public const int EntryCount = ChapterTransition.LastChapter;

    /// <summary>A four-byte purse.</summary>
    public const int EntrySize = 4;

    /// <summary>Bytes the record occupies.</summary>
    public const int SaveSize = EntryCount * EntrySize;

    /// <summary>Offset of the record <b>within the save body</b> — <c>0x12f7</c>, the base both reads use.</summary>
    public const int BodyOffset = 0x12f7;

    private readonly int[] _gold = new int[EntryCount];

    /// <summary>Whether entering this chapter restores the purse from the record — chapters 5, 6, 7 and 8.</summary>
    /// <remarks>
    /// Neither <c>case 5</c> nor <c>case 6</c> has a <c>break</c>: 5 falls through 6's <c>if (chapter == 6)</c>
    /// guard into <c>case 7: case 8:</c>, so 5 and 6 restore as well even though
    /// <see cref="ChapterTransition.ArmFor"/> names a different arm for each.
    /// </remarks>
    public static bool RestoresGold(int chapter) => chapter >= 5 && chapter <= 8;

    /// <summary>Read the record out of a save body; a body too short for it reads as all zero.</summary>
    public void Load(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            System.Array.Clear(_gold, 0, EntryCount);
            return;
        }
        for (var i = 0; i < EntryCount; i++) {
            _gold[i] = System.BitConverter.ToInt32(body, offset + (i * EntrySize));
        }
    }

    /// <summary>Write the record back into a save body, inverse of <see cref="Load"/>.</summary>
    public bool Save(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            return false;
        }
        for (var i = 0; i < EntryCount; i++) {
            System.BitConverter.GetBytes(_gold[i]).CopyTo(body, offset + (i * EntrySize));
        }
        return true;
    }

    /// <summary>
    /// Store the purse as chapter <paramref name="chapter"/> begins; chapter 1 and out-of-range chapters
    /// store nothing (<see cref="ChapterTransition.RecordsFinishingGold"/>).
    /// </summary>
    public void RecordStartOf(int chapter, int partyGold) {
        int index = IndexOf(chapter);
        if (ChapterTransition.RecordsFinishingGold(chapter) && index >= 0) {
            _gold[index] = partyGold;
        }
    }

    /// <summary>The purse stored when chapter <paramref name="chapter"/> began, or 0.</summary>
    public int AtStartOf(int chapter) {
        int index = IndexOf(chapter);
        return index >= 0 ? _gold[index] : 0;
    }

    /// <summary>
    /// The purse a chapter start leaves: the record for the chapter before it when
    /// <see cref="RestoresGold"/>, otherwise <paramref name="purse"/> unchanged.
    /// </summary>
    public int PurseAfterRestore(int chapter, int purse) =>
        RestoresGold(chapter) ? AtStartOf(chapter - 1) : purse;

    private static int IndexOf(int chapter) {
        int index = ChapterTransition.FinishingGoldOffset(chapter) / EntrySize;
        return index >= 0 && index < EntryCount ? index : -1;
    }
}
