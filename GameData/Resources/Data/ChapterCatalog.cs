namespace GameData.Resources.Data;

using System.Collections.Generic;

/// <summary>
/// Engine-independent catalog of the game's chapters for the Contents (table-of-contents) screen:
/// each chapter's intro animation and its replayable book/animation parts. Synthesized from the
/// original archive by ChapterCatalogBuilder (see also the Contents replay design spec). Modders
/// override/extend it to add chapters.
/// </summary>
public class ChapterCatalog(string id) : IResource {
    /// <summary>Well-known resource id under which the (synthesized) catalog is provided/loaded.</summary>
    public const string ResourceId = "CHAPTERS.DAT";

    public ResourceType Type => ResourceType.DAT;
    public string Id { get; } = id;
    public List<Chapter> Chapters { get; set; } = [];
}

public class Chapter {
    /// <summary>1..N. Also the "reached &lt;= current chapter" gate on the Contents screen.</summary>
    public int Number { get; set; }

    /// <summary>The CONTENTS.DAT REQ actionId whose click plays this chapter (explicit binding).</summary>
    public int ContentsActionId { get; set; }

    /// <summary>Presenter-facing name of the chapter-intro animation, played once (e.g. "CHAPTER1").</summary>
    public string IntroAnimation { get; set; } = string.Empty;

    /// <summary>Ordered book+animation parts. Part 0 = chapter opening (played at start); replay plays all.</summary>
    public List<ChapterPart> Parts { get; set; } = [];
}

public class ChapterPart {
    /// <summary>Book resource id WITH extension (e.g. "C11.BOK"), for BookPresenter.ShowBookAsync.</summary>
    public string Book { get; set; } = string.Empty;

    /// <summary>Presenter-facing animation name WITHOUT extension (e.g. "C11"), for
    /// CutscenePresenter.PlayCutsceneAsync. Empty when the part has no animation.</summary>
    public string Animation { get; set; } = string.Empty;
}
