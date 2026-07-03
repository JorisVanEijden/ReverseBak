namespace ResourceExtraction;

using GameData.Resources.Data;

/// <summary>
/// Synthesizes the ChapterCatalog by probing the archive (IResourceProvider.CanProvideResource) —
/// there is no single source file, the same way BookParchment is synthesized from BOOK.SCX. Faithful
/// to the original's Contents replay (playChapterAnimationsAndBook chapter,1 then chapter,2): each
/// chapter gets its CHAPTER{N} intro animation plus parts 1 and 2 (whichever books exist). Higher
/// books (C{N}3+) are in-game chapter beats, not Contents-replayable, so they are not included.
/// </summary>
public static class ChapterCatalogBuilder {
    private const int ChapterCount = 9;
    private const int MaxContentsParts = 2; // the original Contents replay plays parts 1 and 2

    public static ChapterCatalog Build(string id, IResourceProvider provider) {
        var catalog = new ChapterCatalog(id);
        for (int n = 1; n <= ChapterCount; n++) {
            var chapter = new Chapter {
                Number = n,
                ContentsActionId = n + 1,             // CONTENTS.DAT: chapter 1 = actionId 2 .. chapter 9 = 10
                IntroAnimation = $"CHAPTER{n}",        // presenter-facing (probe adds ".ADS")
            };
            for (int p = 1; p <= MaxContentsParts; p++) {
                string bookArchive = $"C{n}{p}.BOK";
                if (!provider.CanProvideResource(bookArchive)) {
                    continue; // part absent (e.g. chapter 2 has no C22) -> skip
                }
                string animArchive = $"C{n}{p}.ADS";
                chapter.Parts.Add(new ChapterPart {
                    Book = bookArchive,                                             // WITH extension
                    Animation = provider.CanProvideResource(animArchive) ? $"C{n}{p}" : string.Empty, // WITHOUT
                });
            }
            catalog.Chapters.Add(chapter);
        }
        return catalog;
    }
}
