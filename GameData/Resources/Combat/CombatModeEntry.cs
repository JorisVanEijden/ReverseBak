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
    /// <b>Table A is the PARTY and table B is the monster roster — always, not just at entry.</b>
    /// </summary>
    /// <remarks>
    /// <b>Corrected 2026-08-20; this said the opposite.</b> The two tables are filled from
    /// unambiguous sources: A is copied straight out of the save's active party
    /// (<c>actors_A[i] = characters[activeParty[i]]</c>), and B is read from the ENCOUNTER roster —
    /// creature types, monster names, and a stamina top-up scaled by how long ago the encounter was
    /// last fought. Neither is ever refilled from the other.
    ///
    /// <para><b>What went wrong is worth keeping, because the same trap is everywhere in this
    /// subsystem: the ARRAYS are the sides, the POINTERS are the roles.</b> Mode entry aims a pair
    /// of pointers at the tables ("the acting side" / "the other side"), and a swap re-aims them
    /// whenever the turn changes hands. Almost all combat code — the monster AI included — reads the
    /// POINTERS, so a scan that means "my allies" during a monster's turn means "the party" during
    /// a party member's. The earlier reading took the AI's heal scan as evidence that table A was
    /// the monsters' own side; the scan is through the pointer, so it is evidence about the acting
    /// side and says nothing about which array is which.</para>
    ///
    /// <para>The behavioural conclusions drawn from it still hold — a monster's heal does reach its
    /// fellows, and the target picker does reach the enemy — because those go through the pointers.
    /// Only the statement about the arrays was inverted, and nothing consumed it yet.</para>
    ///
    /// <para>It matters for anything that touches an array directly, which is where side identity is
    /// fixed: <see cref="PartyMembersLeaveCombatAlive"/> is exactly such a rule.</para>
    /// </remarks>
    public static bool TableAIsThePartyRoster => true;

    /// <summary>
    /// <b>A party member cannot die in combat.</b>
    /// </summary>
    /// <remarks>
    /// The teardown walks table A — the party — and any actor whose health has fallen below 1 leaves
    /// the encounter rewritten to <b>1 health and 0 stamina</b> rather than dead. Party death is not
    /// a combat outcome, so a port that lets the encounter kill a member has invented a rule.
    ///
    /// <para>It reads the ARRAY, not the acting-side pointer, which is why the correction above
    /// matters: through the pointer this loop would restore whichever side happened to be acting.</para>
    /// </remarks>
    public static bool PartyMembersLeaveCombatAlive => true;

    /// <summary>The health a downed party member leaves combat on.</summary>
    public const int DownedPartyMemberHealth = 1;

    /// <summary>The stamina a downed party member leaves combat on.</summary>
    public const int DownedPartyMemberStamina = 0;

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
