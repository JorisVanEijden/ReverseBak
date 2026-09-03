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

    /// <summary>
    /// RE-ARMS an encounter — clears its fought/done/scouted flags so it can happen again.
    /// </summary>
    /// <remarks>
    /// <b>Not a cancel, despite the name, and the opposite of <see cref="CancelCombatEncounter2" />
    /// rather than its sibling.</b> The body finds the zone hotspot matching the id and clears the
    /// trigger's done, scout-tried and scouted flags, then calls
    /// <c>hotspotevt_enc_fought_clear(id)</c> — which writes <c>ENCOUNTER_FOUGHT(id) = <b>0</b></c>
    /// (HOTSPOT.C:1093) — and reloads the encounter's enemy party. Every one of those is a RESET.
    ///
    /// <para>Implementing this from the old remark ("remove a combat encounter... peacefully resolve
    /// a fight") would make a dialog that talks a fight down re-arm it instead. Corrected 2026-09-03
    /// by reading both bodies; the flag directions are unambiguous, one writes 1 and the other 0.</para>
    ///
    /// <para><b>Not implemented.</b> The flag half is a global write, but the per-hotspot done and
    /// scouted clears need the zone's hotspot entries, and a flag-only version would half-arm the
    /// encounter — worse than leaving it loud. Shipped ids: 151, 152, 375.</para>
    ///
    /// <para>Name frozen by JSON serialisation, as with <see cref="CountArmorState" />.</para>
    /// </remarks>
    CancelCombatEncounter = 3,

    /// <summary>
    /// RESOLVES an encounter — marks it already fought, so it does not happen.
    /// </summary>
    /// <remarks>
    /// <b>This is the one that peacefully resolves a fight</b>, which the old remark attributed to
    /// <see cref="CancelCombatEncounter" /> while calling this a sibling of it "via a different
    /// sub-routine path". They are not siblings: this one calls
    /// <c>hotspotevt_enc_fought_set(id)</c> — <c>ENCOUNTER_FOUGHT(id) = <b>1</b></c>
    /// (HOTSPOT.C:1084) — and the other writes 0.
    ///
    /// <para><b>Field2 = 0 means EVERY loaded encounter record</b>, not "no id": the original loops
    /// the zone's records and filters on the id only when it is non-zero. Two of the five shipped
    /// instances pass 0. The other three are ids 343 and 645.</para>
    ///
    /// <para>Implemented for a specific id (the flag is what stops the fight happening); the id-0
    /// case and the original's kind-3 object-state reset are not. See TASK-304.</para>
    ///
    /// <para>Name frozen by JSON serialisation, as with <see cref="CountArmorState" />.</para>
    /// </remarks>
    CancelCombatEncounter2 = 4,

    /// <summary>Copy container from (zone=0,X=20) to (zone=1,X=2,Y=3) and dispose source.</summary>
    MoveContainer = 5,

    /// <summary>Same as MoveContainer but the source is not disposed.</summary>
    MoveContainerKeepSource = 6,

    /// <summary>Add Field2 to global_30015.</summary>
    IncrementGlobal30015 = 7,

    /// <summary>
    /// A two-roll wager against an NPC, settled into that NPC's own running ledger.
    /// </summary>
    /// <remarks>
    /// Rolls <c>RND(Field2)</c> and <c>RND(Field4)</c>. On a win the party gains
    /// <c>quotedPrice * Field6 / 100</c> and the same amount comes OFF the ledger (floored at zero);
    /// on a loss the party pays the quoted price and it goes ON, capped by a guard at 0xea60.
    ///
    /// <para><b>The ledger is per-NPC and persistent</b>, which is the fact that makes this and
    /// <see cref="CapReward" /> intelligible — see that member for what is established about it.</para>
    ///
    /// <para><b>One decompilation oddity, recorded rather than ported:</b> the win/loss chain ends
    /// with the SAME comparison twice (<c>else if (a &lt; b)</c> after <c>else if (a &lt; b)</c>), so
    /// the third arm is unreachable as written. Establish what a DRAW does from the disassembly
    /// before implementing, rather than copying a branch that cannot run.</para>
    /// </remarks>
    GambleRoll = 8,

    /// <summary>
    /// Mends and blesses every EQUIPPED sword in the active party.
    /// </summary>
    /// <remarks>
    /// <b>"Full charge" was the wrong word</b> — the body writes <c>condition = 100</c>
    /// (EVTCOND.C case 9), which is durability, not a spell charge. It is a repair, and it is why
    /// this and <see cref="CountArmorState" /> share a shape.
    ///
    /// <para><b>Equipped only</b> (<c>flags &amp; 0x40</c>), category 1, and the blessing is SET
    /// rather than raised: <c>flags &amp;= 0x1fff</c> clears all three blessing bits and
    /// <c>flags |= 0x8000</c> puts back only the third, so a first-tier blessing is replaced by the
    /// third and an unblessed sword arrives at the top tier directly.</para>
    ///
    /// <para><b>It does NOT clear the repairable bit</b>, unlike case 2 which does. That asymmetry
    /// is the original's; a blessed sword ends at full condition still flagged damaged.</para>
    /// </remarks>
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
    /// <para><b>And it is not the reward money — it is a PER-NPC LEDGER, established 2026-09-03 by
    /// reading its other users.</b> The town scene zeroes it, loads it from the conversing actor's
    /// own persisted event-state subrecord, plays the dialog, writes it back clamped to 0xfa, and
    /// zeroes it again (TOWNSCN.C:467-508). So it is a byte of per-character state that survives the
    /// conversation, not a global. A dialog CONDITION reads it as <c>&gt; 0</c> (GSTATE.C:110), and
    /// <see cref="GambleRoll" /> moves it: losing a wager puts the stake ON, winning takes the payout
    /// OFF.</para>
    ///
    /// <para><b>Most consistent reading — flagged as interpretation, not fact:</b> a running debt or
    /// tab with that character. It grows when the party loses to them, shrinks when the party wins,
    /// can be floored by this subtype, and their dialog can branch on whether any is outstanding.
    /// canassa calls it a "popup retry counter", which fits none of that; this project does not take
    /// its names.</para>
    ///
    /// <para>Name frozen by JSON serialisation, as with <see cref="CountArmorState" />.</para>
    /// </remarks>
    CapReward = 11,

    /// <summary>
    /// Asks the world loop to run the hotspot pass at the party's tile on its next iteration,
    /// without the party having moved.
    /// </summary>
    /// <remarks>
    /// <b>Settled 2026-09-03 by reading the global's READERS, not its name.</b> It is written <c>= 1</c>
    /// here and read in exactly two places — the world loop (WORLDLP.C:185) and the map loop
    /// (MAP.C:208) — which do the same thing:
    /// <code>
    /// if (nothing_moved &amp;&amp; flag != 0) {
    ///     hotspotevt_activate_at_player();   // run the hotspot pass where the party stands
    ///     flag = 0;                          // one-shot: consumed by whichever loop is running
    ///     moved = 1;                         // then finish the iteration as if the party HAD moved
    /// }
    /// </code>
    ///
    /// <para>So it is not a tutorial flag, which is what this member is named and what its remark
    /// claimed until the readers were checked. It is a dialog saying <i>"re-evaluate the tile I am
    /// standing on now"</i> — which is what a dialog that has just set a flag or filled a container
    /// needs, so the trigger under the party's feet fires immediately instead of after a step.</para>
    ///
    /// <para>The <c>moved = 1</c> matters as much as the activation: the rest of that iteration —
    /// pending events, the time advance, the redraw — then runs as a normal world step. Both loops
    /// also clear the flag on ENTRY (WORLDLP.C:86, MAP.C:115), so a request never survives into a
    /// new loop.</para>
    ///
    /// <para><b>Not implemented</b>: reaching the hotspot pass needs a collaborator
    /// <c>DialogExecutor</c> does not have — the same seam subtypes 3 and 4 need. See TASK-304.</para>
    ///
    /// <para>Name frozen by JSON serialisation, as with <see cref="CountArmorState" />.</para>
    /// </remarks>
    SetTutorialFlag = 12,

    /// <summary>
    /// Runs the party's carried light out — every Light timer on source 0, then a zero-length tick.
    /// </summary>
    /// <remarks>
    /// <b>The key is the light SOURCE, not an object id.</b> The body matches
    /// <c>bKind == 1 &amp;&amp; wSub_id == 0</c> (EVTCOND.C case 13), and source 0 is the lit item
    /// in the party's hands — <c>World.LightSourceDecay.Source.Item</c>. Sources 1..3 are dragon's
    /// breath, candle glow and stardusk, and this leaves them burning; a port that read "torches"
    /// as "all light" would blow out the spell that lights caves.
    ///
    /// <para><b>The <c>timerpool_tick(0)</c> that follows is half the effect.</b> Zeroing the
    /// entries only marks them; the tick is what runs the per-tick hooks and removes them, so
    /// without it the light stays on until the clock next moves.</para>
    /// </remarks>
    ExtinguishTorches = 13,

    /// <summary>Clear items in container at (zone=3, X=1308000, Y=1002400).</summary>
    EmptyTrapCacheContainer = 14,

    /// <summary>ChangeAttributeValue(primary speaker, attribute=global_30015, +512).</summary>
    BoostPrimarySpeakerAttribute512 = 15,

    /// <summary>OR Owyn's and Pug's knownSpells1..3 together; both end up with the union.</summary>
    SyncOwynPugSpells = 16,
}
