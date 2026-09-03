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

    /// <summary>
    /// REPAIRS every damaged piece of party armour — condition back to 100, wear flag cleared.
    /// </summary>
    /// <remarks>
    /// <b>It does not count, despite what this member was called until 2026-09-03.</b> The routine
    /// takes a <c>do_repair</c> flag and has two callers: the dialog CONDITION side passes 0 and
    /// counts, which is the behaviour the old name and remark described; this sub-action passes
    /// <b>1</b>, which walks every party member's items and, for each of category 4 with
    /// <c>condition &lt; 100</c>, sets the condition to 100 and clears flag 0x20 (EVTCOND.C:21-53,
    /// case 2 at :193).
    ///
    /// <para>Armour is right — category 4 is armour on both sides of the port (equipped category-4
    /// items carry the damage-type resistances in CBSTAT.C:297, and our own
    /// <c>ObjectType.Armor = 4</c>). Only the verb was wrong, and it is the half that matters:
    /// implementing this from the old name gives a counter, and the armour the player just paid to
    /// have mended stays broken.</para>
    ///
    /// <para>The gold is NOT charged here — the count path multiplies the per-item cost into
    /// <c>lEvtArgGoldCost</c> and <see cref="PayPartyMoney" /> (subtype 0) deducts it, so a repair
    /// service is authored as separate sub-actions.</para>
    ///
    /// <para><b>The member keeps its inaccurate name on purpose.</b> This enum serialises BY NAME
    /// into the committed <c>generated/DDX/*.json</c>, so the name is a data-format contract:
    /// renaming it to <c>RepairPartyArmour</c> made every DDX file fail to deserialise
    /// (<c>PhillipConversationReachesHisTopicsTests</c> caught it immediately). Correcting a name
    /// here means regenerating derived data, which is a bigger change than the naming is worth —
    /// so the correction lives in this remark, where anyone reading the member sees it.</para>
    /// </remarks>
    CountArmorState = 2,   // NOT a count — see the remark. Name frozen by JSON serialisation.

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

    /// <summary>
    /// Raises a running stake/debt counter to at least <c>Field2</c> — <c>X = max(X, Field2)</c>.
    /// </summary>
    /// <remarks>
    /// <b>The old remark had this exactly backwards</b> ("min(global_reward_money, Field2), only
    /// lowers"). The body is <c>if (Field2 &gt; X) X = Field2;</c> — a MAX that only ever RAISES
    /// (EVTCOND.C case 11).
    ///
    /// <para>And it is not the reward money. <c>X</c> here is the same counter
    /// <see cref="GambleRoll" /> moves: a win SUBTRACTS the payout from it and a loss ADDS the
    /// stake, clamped below 0xea60. That reads as a running stake or debt rather than a reward, so
    /// the member's name is a guess and the direction was wrong on top of it. Establish what the
    /// counter is before implementing either subtype.</para>
    ///
    /// <para>Name frozen by JSON serialisation, as with <see cref="CountArmorState" />.</para>
    /// </remarks>
    CapReward = 11,

    /// <summary>
    /// Writes 1 to a single global. <b>What that global MEANS is not established.</b>
    /// </summary>
    /// <remarks>
    /// The most common subtype — 10 of the 39 shipped instances — and the one whose purpose is least
    /// known. The body is a bare <c>&lt;global&gt; = 1</c>. This member's name calls it a tutorial
    /// flag and canassa reads the same write as a hotspot-activate request; **neither is evidence**,
    /// they are two independent guesses at an unnamed variable, and this project does not take
    /// canassa's names.
    ///
    /// <para>Find the READERS of that global before implementing this. Ten instances make it the
    /// subtype most worth getting right and the one most likely to be built wrong from its name.</para>
    ///
    /// <para>Name frozen by JSON serialisation, as with <see cref="CountArmorState" />.</para>
    /// </remarks>
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
