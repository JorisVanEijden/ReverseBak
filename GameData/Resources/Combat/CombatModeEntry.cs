namespace GameData.Resources.Combat;

/// <summary>
/// What happens when a tactical encounter starts — <c>combat_arena_mode_enter</c>
/// (ovr168 @0x5f2c0).
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
}
