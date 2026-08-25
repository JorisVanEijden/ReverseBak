namespace GameData.Resources.Combat;

/// <summary>
/// Clicking a fallen encounter actor to loot it — <c>wcursor_loot_corpse</c>
/// (canassa INPUT/WCURSOR.C:1290).
/// </summary>
/// <remarks>
/// <b>This is NOT the world corpse.</b> <c>handle_Corpse</c> @0x76a0a — the fixed object modelled by
/// <see cref="Data.InteractionDialogResolver"/> — is a placed scenery container. This one is a body
/// left by an ENCOUNTER, addressed through the encounter's roster rather than through a world entity,
/// and it spawns its container on demand from that roster. Two different clicks, two different code
/// paths, and the same word for both.
/// </remarks>
public static class EncounterCorpseLoot {
    /// <summary>Roster slots per encounter record — the same seven everything else uses.</summary>
    public const int RosterSlots = World.EncounterDefeat.RosterSlots;

    /// <summary>
    /// The hotspot's slot number carries BOTH coordinates: record above, roster slot below.
    /// </summary>
    /// <remarks>
    /// The original indexes <c>g_anEncounterRecordIds[nSlot / 7]</c> and passes <c>nSlot % 7</c> as
    /// the roster slot. Reading the number as a single index finds the wrong body — or, for any
    /// slot under seven, always the first record's.
    /// </remarks>
    public static int RecordIndexOf(int hotspotSlot) => hotspotSlot / RosterSlots;

    /// <inheritdoc cref="RecordIndexOf"/>
    public static int RosterSlotOf(int hotspotSlot) => hotspotSlot % RosterSlots;

    /// <summary>Reach above ground, in world units.</summary>
    public const int ReachAboveGround = 7000;

    /// <summary>
    /// Reach underground — <b>well under half</b> the above-ground one.
    /// </summary>
    /// <remarks>
    /// <c>g_game_mode == 2</c> picks this. It is the same "a dungeon is a tighter space" rule the
    /// movement code applies by quartering the step underground, so a port that uses one reach
    /// everywhere lets the party loot through walls in exactly the place the original will not.
    /// </remarks>
    public const int ReachUnderground = 2500;

    /// <inheritdoc cref="ReachUnderground"/>
    public static int Reach(bool underground) =>
        underground ? ReachUnderground : ReachAboveGround;

    /// <summary>Whether the body is close enough to loot.</summary>
    /// <remarks>
    /// <b>Out of reach is SILENT.</b> The original returns before the click sound and before any
    /// dialog — no refusal line, no cue. So "nothing happened" is the correct feedback for a distant
    /// body, and a port that explains itself here is adding a message the game does not have.
    /// </remarks>
    public static bool WithinReach(long distance, bool underground) =>
        distance <= Reach(underground);

    /// <summary>The click cue, played once the body is in reach.</summary>
    /// <remarks>
    /// <b>Before the validity checks, not after.</b> It sounds even when the click is then refused
    /// or the body turns out to hold nothing — the cue acknowledges the CLICK, not the outcome.
    /// </remarks>
    public const int ClickSoundId = 0x30;

    /// <summary>Played when the menu state forbids looting right now.</summary>
    public const int RefusedDialog = 0x5f;

    /// <summary>Played when the slot yields no lootable body.</summary>
    /// <remarks>
    /// Two different failures share it: the roster slot is empty (<c>-1</c>, so nothing spawns) and
    /// the spawned actor is not resident in combat. Both mean "there is nothing to loot here".
    /// </remarks>
    public const int NothingToLootDialog = 0x9a;

    /// <summary>Played when a body IS looted and carries no message of its own.</summary>
    public const int DefaultLootDialog = 0x4e;

    /// <summary>
    /// The line a successful loot plays.
    /// </summary>
    /// <param name="interactMessageId">
    /// The actor's <c>SUBREC_INTERACT_MSG</c> id, or 0 when it has none.
    /// </param>
    /// <remarks>
    /// <b>Zero means "no message", not "message zero".</b> The original tests both that the
    /// subrecord exists and that its id is non-zero before using it, so a body carrying id 0 falls
    /// back to the default rather than playing record 0.
    /// </remarks>
    public static int LootDialogFor(int interactMessageId) =>
        interactMessageId != 0 ? interactMessageId : DefaultLootDialog;

    /// <summary>
    /// <b>The spawned actor is destroyed and persisted afterwards — on EVERY path that spawned one.</b>
    /// </summary>
    /// <remarks>
    /// Including the failure path where the actor was not lootable. The body is materialised only
    /// for the duration of the interaction; what survives is the persisted state, which is what
    /// makes a looted corpse stay looted.
    /// </remarks>
    public static bool AlwaysDestroysAndPersists => true;
}
