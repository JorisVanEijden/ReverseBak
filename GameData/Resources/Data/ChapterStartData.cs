namespace GameData.Resources.Data;

using GameData.Resources;
using GameData.Resources.Location;

/// <summary>
/// Per-chapter starting state, mirroring the original game's <c>go_to_chapter</c>
/// (KRONDOR.EXE 0x41f0a). A new game (or a chapter transition) reads STARTUP.GAM as a
/// template and then applies this record over it, which is what gives the party its
/// real starting zone/position — STARTUP.GAM's own state block leaves those fields zero.
///
/// Backed by CHAPx.DAT (23 bytes): the engine reads the first 16 bytes over its game-state
/// globals (chapter, gold, clock, death/transition flags) and the trailing 7 bytes as a
/// <see cref="Location"/>, then calls <c>SetPlayerLocation</c> (0x738b3). The
/// <see cref="FullMapIcon"/> is not part of CHAPx.DAT — it comes from a hard-coded
/// per-chapter table in <c>StartGameOrLoadSave</c> (0x20835), folded in here so a single
/// resource carries everything needed to begin a chapter.
/// </summary>
public sealed class ChapterStartData : IResource {
    public ChapterStartData(
        string id,
        int chapterNumber,
        int gold,
        int gameTimeIn2Seconds,
        int timeSnapshot,
        byte partyDeathState,
        byte chapterTransitionPending,
        Location startLocation,
        int positionX,
        int positionY,
        FullMapIcon fullMapIcon
    ) {
        Id = id;
        ChapterNumber = chapterNumber;
        Gold = gold;
        GameTimeIn2Seconds = gameTimeIn2Seconds;
        TimeSnapshot = timeSnapshot;
        PartyDeathState = partyDeathState;
        ChapterTransitionPending = chapterTransitionPending;
        StartLocation = startLocation;
        PositionX = positionX;
        PositionY = positionY;
        FullMapIcon = fullMapIcon;
    }

    public int ChapterNumber { get; }
    public int Gold { get; }
    public int GameTimeIn2Seconds { get; }
    public int TimeSnapshot { get; }
    public byte PartyDeathState { get; }
    public byte ChapterTransitionPending { get; }

    /// <summary>Zone + tile + sub-tile offsets + facing the party starts at.</summary>
    public Location StartLocation { get; }

    /// <summary>
    /// Start position in internal game units, equivalent to what
    /// <c>ToCenteredGamePosition</c> (0x73035) computes from the tile/offset:
    /// tile*64000 + offset*1600 + 800 (sub-tile centring). Z is left to the engine
    /// (terrain height), matching the original which never sets it here.
    /// </summary>
    public int PositionX { get; }
    public int PositionY { get; }

    public FullMapIcon FullMapIcon { get; }

    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
