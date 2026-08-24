namespace GameData.Resources.Data;

/// <summary>
/// One combat slot to write back into a save — the mid-combat state of the actor at
/// <see cref="ActorSlot"/>.
/// </summary>
/// <remarks>
/// <b>The slot indexes the save's 1730-entry ACTOR table, not the party.</b> The combat block holds
/// one 22-byte record per actor, so slot N here is the same N a roster entry names — and resolving
/// it through the six-entry party array would field a party member for any slot under six. That is
/// the same distinction <c>CombatRuntime.EnterRoster</c> already turns on.
/// </remarks>
public readonly struct DirtyCombatantEdit {
    public DirtyCombatantEdit(int actorSlot, SaveGameCombatData record) {
        ActorSlot = actorSlot;
        Record = record;
    }

    /// <summary>Index into the save's actor table.</summary>
    public int ActorSlot { get; }

    /// <summary>The record to write.</summary>
    public SaveGameCombatData Record { get; }
}
