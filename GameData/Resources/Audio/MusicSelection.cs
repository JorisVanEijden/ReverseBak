namespace GameData.Resources.Audio;

using GameData.Resources.World;

/// <summary>
/// Which track each context asks for. The counterpart to <see cref="MusicPlayback"/>, which is only
/// what a request <i>does</i>.
///
/// <para><b>There is no central music dispatcher.</b> Every context calls <c>audio_music_play</c>
/// with its own track id at the point it changes screen or zone, so "background music" is not a
/// system with a policy — it is a handful of independent decisions. They are collected here so the
/// remake has one place to look, not because the original had one.</para>
///
/// <para>Sources: <c>zone_load_definition</c> (<c>R3D/SCENE/ZONE.C</c>),
/// <c>combat_arena_run</c> (<c>COMBAT/ARENA/COMBAT.C</c>), <c>itemuse_dispatch</c> case 0x51
/// (<c>SCREENS/ITEMUSE.C</c>), <c>bookview_show</c> (<c>SCREENS/BOOKVIEW.C</c>) and the fixed tracks
/// in <c>MAINMENU.C</c> / <c>CIPHER.C</c> / <c>GMAIN.C</c>.</para>
/// </summary>
public static class MusicSelection {
    /// <summary>Underground zones.</summary>
    public const int UndergroundTrack = 0x3ec;

    /// <summary>Zone 6, which has a track of its own.</summary>
    public const int ZoneSixTrack = 0x3fe;

    /// <summary>Every zone once the final chapter is reached.</summary>
    public const int FinalChapterTrack = 0x405;

    /// <summary>Main menu, and the chapter cutscene.</summary>
    public const int MainMenuTrack = 0x3f7;

    /// <summary>The riddle screen.</summary>
    public const int RiddleTrack = 0x3eb;

    /// <summary>The zone that carries its own track regardless of chapter.</summary>
    public const int ZoneWithOwnTrack = 6;

    /// <summary>The chapter from which every zone plays <see cref="FinalChapterTrack"/>.</summary>
    public const int FinalChapter = 8;

    /// <summary>
    /// The three combat tracks, chosen at random on entering an encounter.
    /// </summary>
    public static readonly int[] CombatTracks = { 0x40a, 0x3ed, 0x413 };

    /// <summary>
    /// The background track for a zone, decided when its definition is loaded.
    /// </summary>
    /// <param name="zoneKind">
    /// The zone's kind — <see cref="ZoneDefinition.ZoneLocation"/>, the first field of
    /// <c>Z##DEF.DAT</c>.
    /// </param>
    /// <remarks>
    /// <b>The overworld is silent.</b> An ordinary outdoor zone before chapter 8 asks for
    /// <see cref="MusicPlayback.NoTrack"/> — the game plays no travelling music at all, and what you
    /// hear out there is ambience and footsteps. A remake that loops a world theme is adding
    /// something the original does not have.
    ///
    /// <para>The tests run in order and the <b>first</b> wins: underground beats zone 6, and both
    /// beat the final chapter. So chapter 8 does not re-score the caves, and zone 6 keeps its own
    /// track to the end of the game.</para>
    /// </remarks>
    public static int ForZone(int zoneKind, int zoneId, int chapter) {
        if (zoneKind == ZoneDefinition.UndergroundZoneLocation) {
            return UndergroundTrack;
        }
        if (zoneId == ZoneWithOwnTrack) {
            return ZoneSixTrack;
        }
        if (chapter == FinalChapter) {
            return FinalChapterTrack;
        }
        return MusicPlayback.NoTrack;
    }

    /// <summary>
    /// The track for a combat encounter.
    /// </summary>
    /// <param name="roll">A roll in <c>[0, 3)</c> — the original's <c>RND(3)</c>.</param>
    /// <param name="combatMusicEnabled">The combat-music preference (engine prefs flag 4).</param>
    /// <remarks>
    /// <b>Combat music is a separate preference from music itself</b>, and when it is off the
    /// encounter is explicitly silenced rather than left playing whatever the zone had. Either way
    /// the caller keeps the returned previous track and plays it back when combat ends, so the world
    /// music resumes.
    /// </remarks>
    public static int ForCombat(int roll, bool combatMusicEnabled) {
        if (!combatMusicEnabled) {
            return MusicPlayback.NoTrack;
        }
        return roll >= 0 && roll < CombatTracks.Length
            ? CombatTracks[roll]
            : CombatTracks[CombatTracks.Length - 1];
    }

    /// <summary>
    /// The track played behind the dialog when a healing item is used on a party member.
    /// </summary>
    /// <param name="health">The member's current health, before the item heals them.</param>
    /// <remarks>
    /// <b>The worse their state, the grimmer the track</b> — four bands, chosen from health
    /// <i>before</i> the heal, so the music reflects what you are treating rather than the result.
    /// It is restored as soon as the dialog closes.
    /// </remarks>
    public static int ForHealingItem(int health) =>
        health > 0x50 ? 0x3ef
        : health > 0x41 ? 0x40f
        : health > 0x2d ? 0x410
        : 0x3f0;

    /// <summary>Book page whose display number starts <see cref="BookPageTrackA"/>.</summary>
    public const int BookPageA = 0x1d5;

    /// <summary>Book page whose display number starts <see cref="BookPageTrackB"/>.</summary>
    public const int BookPageB = 0x1dc;

    /// <summary>Track started on reaching <see cref="BookPageA"/>.</summary>
    public const int BookPageTrackA = 0x425;

    /// <summary>Track started on reaching <see cref="BookPageB"/>.</summary>
    public const int BookPageTrackB = 0x411;

    /// <summary>
    /// The track a book page starts, if any.
    /// </summary>
    /// <returns>
    /// <see cref="MusicPlayback.QueryOnly"/> for every other page — <b>not</b> silence. Only two
    /// pages in the whole game change the music, and every other page leaves it alone; returning
    /// <see cref="MusicPlayback.NoTrack"/> instead would stop the music on each page turn.
    /// </returns>
    public static int ForBookPage(int displayNumber) =>
        displayNumber == BookPageA ? BookPageTrackA
        : displayNumber == BookPageB ? BookPageTrackB
        : MusicPlayback.QueryOnly;

    // ---- the options menu ----------------------------------------------------------------------
    // UI_showMainMenu @0x6de78. The same track for both forms of the menu: the openedFromGame flag
    // picks REQ_OPT1 over REQ_OPT0, it does not change the music.

    /// <summary>How the options menu was left — what decides whether the music is put back.</summary>
    public enum MenuExit {
        /// <summary>Closed back to whatever was underneath (the in-game menu's Cancel).</summary>
        Resume,

        /// <summary>Start a new game.</summary>
        NewGame,

        /// <summary>Load a save.</summary>
        LoadGame,

        /// <summary>Open the table of contents.</summary>
        Contents,

        /// <summary>Leave the game.</summary>
        Quit,
    }

    /// <summary>
    /// Whether closing the options menu restores the track that was playing when it opened.
    /// </summary>
    /// <remarks>
    /// <b>Only <see cref="MenuExit.Resume"/> restores.</b> The original tests the four departing
    /// choices by name and returns before the restore call — exit, new game, load game and contents
    /// all leave the menu's own theme playing, because each destination sets its own music as it
    /// comes up. Restoring on those would put the previous track back for the moment it takes the
    /// destination to load, which is a stutter the original does not have.
    ///
    /// <para>The saved value comes from the menu's opening call: <c>audio_music_play</c> returns
    /// what was playing, which is the whole reason it has a return value.</para>
    /// </remarks>
    public static bool RestoresPreviousTrack(MenuExit exit) => exit == MenuExit.Resume;
}
