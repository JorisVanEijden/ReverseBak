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

    /// <summary>
    /// Copies the container at (0, 20, 0) over the one at (1, 2, 3). Nothing is moved or disposed.
    /// </summary>
    /// <remarks>
    /// <b>*** 5 AND 6 ARE THE SAME BODY, AND 5 FALLS THROUGH INTO 6. ***</b> EVTCOND.C case 5 has
    /// no <c>break</c>, so it runs the copy and then runs case 6's identical copy immediately
    /// after. Both members' old summaries described a difference — "dispose source" against "the
    /// source is not disposed" — that does not exist in either body. A second identical copy
    /// changes nothing, so porting this as one copy is faithful in outcome.
    ///
    /// <para><b>Nothing is disposed by either.</b> <c>itemuse_actor_spawn_clone_inv</c>
    /// (ITEMUSE.C:555) writes the destination's list from the source's and calls
    /// <c>actorspawn_destroy_and_persist</c> — which flushes the record and frees the in-memory
    /// actor. The source keeps every item; the "destroy" is an allocation going away, not a
    /// container being emptied. Implementing "move" from the member name would strip a shop's
    /// stockroom.</para>
    ///
    /// <para><b>The first argument is the ZONE.</b> <c>actorspawn_objfixed</c> names it
    /// <c>kind</c> and matches it against the OBJFIXED record's <c>kind</c> header field, which
    /// reads as an object category — but every caller passes a zone id, plainly so in
    /// <c>itemuse_ground_pile_open_inv</c>, which passes <c>g_gameState.nZoneId</c>.</para>
    ///
    /// <para>The <see cref="ItemFlags.Equipped"/> bit is cleared on every item copied
    /// (<c>flags &amp;= 0xffbf</c>): something being worn where it came from must not read as worn
    /// in a chest.</para>
    ///
    /// <para><b>NEITHER IS USED BY THE SHIPPED GAME — zero instances across every DDX — and their
    /// destination does not exist.</b> No container sits at (1, 2, 3) in the save or in
    /// OBJFIXED.DAT in ANY of the nine chapters, checked. So the original's
    /// <c>itemuse_actor_spawn_clone_inv</c> would dereference a null actor here; the missing
    /// <c>break</c> never mattered because the path is never taken. Ported anyway, since a mod can
    /// author one and the copy is shared with <see cref="RelocateContainers" /> — but it is the one
    /// sub-action with no shipped behaviour to be faithful to.</para>
    ///
    /// <para>Names frozen by JSON serialisation, as with <see cref="CountArmorState" />.</para>
    /// </remarks>
    MoveContainer = 5,

    /// <summary>Identical to <see cref="MoveContainer" /> — see its remark.</summary>
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
    /// <para><b>The counter is the establishment's own fund</b>, byte 7 of its
    /// <c>SUBREC_EVENT_STATE</c> — the same one a barding performance is paid out of. See
    /// <see cref="CapReward" /> for how that was established, and for the two wrong names it had
    /// before.</para>
    ///
    /// <para><b>*** SETTLED FROM THE DISASSEMBLY: A DRAW DOES NOTHING. ***</b> The decompiled
    /// win/loss chain ends with the SAME comparison twice (<c>else if (a &lt; b)</c> after
    /// <c>else if (a &lt; b)</c>), so its third arm cannot run as written — and that is NOT a
    /// decompilation artifact. At 0x410c8 the binary compares the two roll globals a third time and
    /// branches on <c>jl</c>, the same direction as the second arm's <c>jge</c> at 0x4109b; the
    /// only way to reach it is with the two EQUAL, so the jump is never taken and control falls to
    /// the return. On a draw the outcome global keeps whatever the last wager left in it, and
    /// neither the purse nor the fund moves. <c>DialogWager.Settle</c> reports that as "not
    /// settled" rather than as a third outcome value, because writing a 2 would invent a state the
    /// shipped game never reaches.</para>
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

    /// <summary>
    /// Copies (0, 20, 1) to (15, 60, 3) and (0, 30, 1) to (15, 64, 3).
    /// </summary>
    /// <remarks>
    /// Two of <see cref="MoveContainer" />'s copies, with the same rules: the sources keep their
    /// items and <see cref="ItemFlags.Equipped"/> is cleared on each copy.
    ///
    /// <para><b>Its sources are at y = 1, not y = 0</b> — a different pair of containers from
    /// <see cref="MoveContainer" />'s (0, 20, <b>0</b>), which is easy to lose when the summary
    /// gives only the x.</para>
    /// </remarks>
    RelocateContainers = 10,

    /// <summary>
    /// Raises a running stake/debt counter to at least <c>Field2</c> — <c>X = max(X, Field2)</c>.
    /// </summary>
    /// <remarks>
    /// <b>The old remark had this exactly backwards</b> ("min(global_reward_money, Field2), only
    /// lowers"). The body is <c>if (Field2 &gt; X) X = Field2;</c> — a MAX that only ever RAISES
    /// (EVTCOND.C case 11).
    ///
    /// <para><b>And it is not the reward money — it is the ESTABLISHMENT'S FUND</b>, byte 7 of the
    /// speaking actor's <c>SUBREC_EVENT_STATE</c> block. The town scene loads it from that actor,
    /// plays the dialog, writes it back clamped to 0xfa and zeroes its working copy
    /// (TOWNSCN.C:467-508), so it is per-establishment state that survives the conversation.</para>
    ///
    /// <para><b>*** IT IS THE BARDING FUND, AND THIS PORT ALREADY NAMED IT CORRECTLY. ***</b> An
    /// earlier note here called it "a running debt or tab with that character", flagged as
    /// interpretation — it was wrong, and the sign was backwards. TOWNSCN.C:260-293 reads the same
    /// byte as the money a tavern owes for a performance: it pays out <c>counter * 10</c> gold,
    /// gated on the party's best Barding (stat 0xb) against byte 6, halved for a decent showing and
    /// quartered for a poor one, then zeroes it. Byte 6 is the difficulty. Those are exactly
    /// <c>SaveGameContainerShopData.BardingDifficulty</c> and <c>BardingReward</c>, which
    /// <c>Scene.Barding</c> and <c>LocationScreen.RunBardingAsync</c> have read and spent all
    /// along. canassa's <c>bPopup_retry_counter</c> and <c>b_pad06</c> are the wrong names, and
    /// "ledger" was a third wrong one invented to replace them.</para>
    ///
    /// <para>So the coherent reading is a POT the house holds for the party: barding earns it,
    /// <see cref="GambleRoll" /> moves it either way, and this subtype puts a floor under it. A
    /// dialog CONDITION reads it as <c>&gt; 0</c> (GSTATE.C:110) — "is there anything waiting for
    /// you here".</para>
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

    /// <summary>Clears the container at (zone 3, 1308000, 1002400).</summary>
    /// <remarks>
    /// <c>itemCount = 0</c> then <c>actorspawn_destroy_and_persist</c>: the items go, the record is
    /// written back and the container stays where it is. Checked against the body — this summary
    /// was right.
    /// </remarks>
    EmptyTrapCacheContainer = 14,

    /// <summary>
    /// Raises one of the primary speaker's stats — the stat named by global 30015 — by two points.
    /// </summary>
    /// <remarks>
    /// <b>512 is 2.0, not 512.</b> <c>stat_combatant_modify(&amp;characters[nEvtArgActor0],
    /// lEvtArgValue, 0x200, 0)</c>: the delta is in 256ths, added to the slot's own fractional
    /// accumulator, and only the whole part carries into <c>base</c> (STAT.C:185). So this is two
    /// points of skill, awarded through the same machinery every other skill gain uses.
    ///
    /// <para><b>NOT IMPLEMENTED, and it is the one subtype that genuinely needs a system built.</b>
    /// The routine is more than an add: it applies the SKILL_SELECTED training multiplier
    /// (<c>delta += delta * trainRate / 0x34</c>), carries the fraction, clamps to the per-stat
    /// min/max tables, raises <c>max</c> to meet a base that passed it, and raises the
    /// SKILL_IMPROVED event plus the party-dirty flag on an increase. Two other places in this port
    /// already stand in for it with <c>Base = min(Base + 1, Max)</c> and say so; a third would be
    /// the wrong way to close this. It belongs with TASK-120.</para>
    ///
    /// <para>Mode 0 here means no proportional scaling — modes 1, 2 and 3 scale the delta by the
    /// current base, the headroom, and a per-stat ratio table respectively.</para>
    /// </remarks>
    BoostPrimarySpeakerAttribute512 = 15,

    /// <summary>
    /// OR Owyn's and Pug's knownSpells1..3 together; both end up with the union.
    /// </summary>
    /// <remarks>
    /// <b>The one summary in this enum that survived being checked against its body.</b> EVTCOND.C
    /// case 16 is exactly this, and the member name says what it does.
    ///
    /// <para>Two things worth keeping anyway: it addresses <c>characters[CHR_OWYN]</c> and
    /// <c>characters[CHR_PUG]</c> in the CHARACTER table, so it reaches whichever of them is not
    /// travelling; and it writes the union back to BOTH, so neither is the source and neither
    /// loses a spell.</para>
    /// </remarks>
    SyncOwynPugSpells = 16,
}
