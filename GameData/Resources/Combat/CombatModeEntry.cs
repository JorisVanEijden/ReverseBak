namespace GameData.Resources.Combat;

/// <summary>
/// What happens when a tactical encounter starts and ends — <c>combat_arena_mode_enter</c>
/// (ovr168 @0x5f2c0) and <c>combat_arena_mode_exit</c> (@0x5f459).
///
/// <para>The two are an exact mirror: every load has a matching dispose in reverse order, the
/// bitmap-slot swap is undone, and the world zone is reloaded.</para>
/// </summary>
public static class CombatModeEntry {
    /// <summary>
    /// <b>Entering combat unloads the overworld zone.</b>
    /// </summary>
    /// <remarks>
    /// The arena is not an overlay on the world — the first thing mode entry does is throw the zone
    /// away. So a port cannot keep the world scene alive underneath and reveal it again on exit; the
    /// world has to be reloaded, which is why leaving a fight is a load rather than a resume.
    /// </remarks>
    public static bool UnloadsTheWorldZone => true;

    /// <summary>The three combat songs, one chosen at random per encounter.</summary>
    /// <remarks>
    /// <b>Combat music is not one track.</b> A d3 picks between them as the fight begins, so the same
    /// encounter sounds different on a replay. Playing a single fixed combat theme is the obvious
    /// implementation and is wrong.
    /// </remarks>
    public static readonly int[] CombatSongs = { 1034, 1005, 1043 };

    /// <summary>The song for a given roll.</summary>
    public static int SongFor(int rollUnder3) =>
        rollUnder3 >= 0 && rollUnder3 < CombatSongs.Length ? CombatSongs[rollUnder3] : CombatSongs[2];

    /// <summary>The value passed when combat music is switched off.</summary>
    public const int NoSong = -1;

    /// <summary>
    /// What is actually played, honouring the configuration.
    /// </summary>
    /// <remarks>
    /// With the combat-music option off the routine still calls the player, passing
    /// <see cref="NoSong"/> — it <i>stops</i> the music rather than leaving whatever was playing. So
    /// turning the option off is not "don't start a track", it is "go silent on entering a fight".
    /// </remarks>
    public static int SongToPlay(int rollUnder3, bool combatMusicEnabled) =>
        combatMusicEnabled ? SongFor(rollUnder3) : NoSong;

    /// <summary>
    /// <b>Table A is the NPC roster and table B is the party — at entry.</b>
    /// </summary>
    /// <remarks>
    /// Mode entry assigns them explicitly: the monster roster into A with its count, the party's
    /// seven slots into B with theirs. They are then <i>swapped</i> for whichever side is acting —
    /// which is why routines that a party member can trigger call the swap first, and why the same
    /// scan means "my allies" or "the enemy" depending on who is taking the turn.
    ///
    /// <para>This resolves what the monster AI could not settle from its own code: a monster's heal
    /// scans table A, and A is its own side, so it really is healing its fellows. The target picker
    /// scans table B, which is the party — the enemy — as it should be.</para>
    /// </remarks>
    public static bool TableAIsTheNpcRosterAtEntry => true;

    /// <summary>Actor slots each side has.</summary>
    public const int SideSlots = 7;

    /// <summary>
    /// <b>Enemy idle animations are given a random starting phase.</b>
    /// </summary>
    /// <remarks>
    /// Every actor on the field is set to its idle animation and then given a random phase, so a rank
    /// of identical creatures does not breathe in unison. Cheap, and its absence is immediately
    /// visible.
    /// </remarks>
    public static bool IdleAnimationsAreRandomlyPhased => true;

    /// <summary>
    /// <b>The spell catalogue is loaded for the whole encounter, not per cast.</b>
    /// </summary>
    /// <remarks>
    /// Mode entry loads the spell table and the weakness/resistance tables and initialises the
    /// active-effect pool, and they stay for the fight. That is the opposite of the overworld, where
    /// the catalogue is paged in around the cast screen and disposed as soon as it closes — so the
    /// same data has two different lifetimes depending on where you are casting from.
    /// </remarks>
    public static bool CatalogueIsResidentForTheEncounter => true;

    // ---------------------------------------------------------------- leaving
    // combat_arena_mode_exit @0x5f459.

    /// <summary>
    /// <b>The previous song is saved on entry and replayed on exit.</b>
    /// </summary>
    /// <remarks>
    /// The call that starts the combat track <i>returns</i> whatever was playing, and mode entry
    /// keeps it; exit passes that value straight back. So the overworld music resumes as the track it
    /// was, rather than being restarted from a default or from silence — which is what a port
    /// stopping and restarting music around a fight would get wrong.
    /// </remarks>
    public static bool PreviousSongIsRestoredOnExit => true;

    /// <summary>
    /// <b>Exit disposes the active-effect pool.</b>
    /// </summary>
    /// <remarks>
    /// Confirms from the other end what the pool itself records: it is encounter-scoped and never
    /// saved. Nothing a spell hung on a combatant survives the fight, so there is no lingering state
    /// to carry back to the overworld.
    /// </remarks>
    public static bool EffectPoolIsDisposedOnExit => true;

    /// <summary>
    /// <b>The world zone is reloaded on the way out.</b>
    /// </summary>
    /// <remarks>
    /// The mirror of the unload at entry, and the other half of why leaving a fight is a load rather
    /// than a resume: the world the party returns to is freshly loaded, not the one they left
    /// suspended.
    /// </remarks>
    public static bool ReloadsTheWorldZoneOnExit => true;

    /// <summary>
    /// Everything mode entry acquires and mode exit releases, in the order exit releases it.
    /// </summary>
    /// <remarks>
    /// Listed because the pairing is the useful part: a port that acquires these at different
    /// lifetimes than the original will not notice until something outlives an encounter that should
    /// not have. Exit runs the spell tables, the effect pool, the encounter, the actor pool, the two
    /// REQ layouts, the two bitmap sets, the combat sounds, the object table — then the zone.
    /// </remarks>
    public static readonly string[] TeardownOrder = {
        "spell weakness/resistance tables",
        "spell catalogue",
        "active spell-effect pool",
        "encounter",
        "combat actor pool",
        "shoot.dat layout",
        "combat.dat layout",
        "figs.bmx",
        "parch.bmx",
        "combat sounds",
        "object table",
        "world zone (reloaded)",
    };
}
