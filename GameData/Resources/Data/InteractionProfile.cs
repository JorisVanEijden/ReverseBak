namespace GameData.Resources.Data;

using System.Collections.Generic;

/// <summary>Proximity thresholds in DOS fine units (the GlobalKey depth gate). Null profile
/// range = no proximity gate.</summary>
public readonly record struct InteractionRange(int Overground, int Underground);

/// <summary>
/// Engine-independent, data-driven description of how a world entity type interacts when
/// clicked — the RE'd constants of one DOS interaction handler as data. Consumed by the
/// Unity "container" mechanism + <see cref="InteractionDialogResolver"/>. Authored by the
/// extractor (RE table) or by a modder.
/// </summary>
public sealed record InteractionProfile {
    /// <summary>Proximity gate; null = none (e.g. handle_Well has no range check).</summary>
    public InteractionRange? Range { get; init; }

    /// <summary>Container types this entity loots/acts on (else the not-actionable dialog).
    /// Never null.</summary>
    public IReadOnlyList<SaveGameContainerType> ActionableContainerTypes { get; init; }
        = System.Array.Empty<SaveGameContainerType>();

    /// <summary>DDX shown on a right-click (examine).</summary>
    public int ExamineDialogId { get; init; }

    /// <summary>DDX shown on left-click when the actionable container has no per-container
    /// dialog of its own.</summary>
    public int ActionDialogId { get; init; }

    /// <summary>DDX shown on left-click when the container type is not in
    /// <see cref="ActionableContainerTypes"/>.</summary>
    public int NotActionableDialogId { get; init; }

    /// <summary>Left-click opens the loot/transfer screen when the container has items.</summary>
    public bool OpensLoot { get; init; }

    /// <summary>Entity has a lock/trap stage (chest). Not yet implemented; corpse = false.</summary>
    public bool HasLock { get; init; }
}
