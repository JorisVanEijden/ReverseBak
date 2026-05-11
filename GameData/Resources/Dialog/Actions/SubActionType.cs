namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// The 17 hardcoded game-state effects dispatched by <see cref="SubAction"/>
/// (action type 7) via <c>PerformSubAction</c> at 0x40f19 in the original
/// binary. The payload's first <c>ushort</c> selects one of these; the
/// remaining 6 bytes (Field2/4/6) are subtype-specific parameters. See
/// <c>docs/specs/dialog-system.md §"SubAction Subtypes"</c> for the full
/// per-subtype documentation.
/// </summary>
public enum SubActionType {
    /// <summary>Subtract global_30014_money from party gold (clamping at 0).</summary>
    PayPartyMoney = 0,

    /// <summary>Add global_30015 to party gold.</summary>
    RewardPartyMoney = 1,

    /// <summary>Count good/damaged armor in party via sub_ovr128_0.</summary>
    CountArmorState = 2,

    /// <summary>Remove a combat encounter by id (Field2). Used to peacefully resolve a fight.</summary>
    CancelCombatEncounter = 3,

    /// <summary>Sibling of CancelCombatEncounter via a different sub-routine path.</summary>
    CancelCombatEncounter2 = 4,

    /// <summary>Copy container from (zone=0,X=20) to (zone=1,X=2,Y=3) and dispose source.</summary>
    MoveContainer = 5,

    /// <summary>Same as MoveContainer but the source is not disposed.</summary>
    MoveContainerKeepSource = 6,

    /// <summary>Add Field2 to global_30015.</summary>
    IncrementGlobal30015 = 7,

    /// <summary>Two-roll gambling/check. Field2/4 are dice sides; Field6 is reward percentage.</summary>
    GambleRoll = 8,

    /// <summary>For each active party member, set all equipped Swords to blessed3 with full charge.</summary>
    BlessAllPartySwords = 9,

    /// <summary>Copy containers from (zone=0,X=20) and (zone=0,X=30) to (zone=15,X=60,Y=3) and (zone=15,X=64,Y=3).</summary>
    RelocateContainers = 10,

    /// <summary>global_reward_money = min(global_reward_money, Field2). Only lowers.</summary>
    CapReward = 11,

    /// <summary>Set word_dseg_1A28 = 1. Most common subtype (10× in data).</summary>
    SetTutorialFlag = 12,

    /// <summary>Zero all expiry-timer entries of type Light with key Torch, then tick the timer system.</summary>
    ExtinguishTorches = 13,

    /// <summary>Clear items in container at (zone=3, X=1308000, Y=1002400).</summary>
    EmptyTrapCacheContainer = 14,

    /// <summary>ChangeAttributeValue(primary speaker, attribute=global_30015, +512).</summary>
    BoostPrimarySpeakerAttribute512 = 15,

    /// <summary>OR Owyn's and Pug's knownSpells1..3 together; both end up with the union.</summary>
    SyncOwynPugSpells = 16,
}
