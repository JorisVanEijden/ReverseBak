namespace GameData.Resources.Data;

/// <summary>
/// Container-type byte (IDA <c>containerType</c>), ground truth from
/// generated/STARTUP.savegame.json + handler usage. The list mixes actor/template
/// inventories and placed world objects; only 4/5/6/9 reach HandleEnvironmentInteraction.
///
/// <para>The byte IS the DOS actor's <c>bResidence</c> (ACTOR.H): 0 = RES_FREE,
/// 1 = RES_PARTY_SLOT, 2 = RES_ENCOUNTER, 4 = RES_CONTAINER, 5 = RES_BODY,
/// 6 = RES_FIXED_OBJECT, 7 = RES_COMBAT, 8 = RES_PICKLOCK_BUFFER, 9 = RES_SELF_SPAWN.
/// Type 8 was previously guessed to be a "cheat chest"; it is in
/// fact the party's shared keys inventory — <c>boot_party_state_load_from_temp</c>
/// (BOOT.C:132) binds <c>g_gameState.shared_inventory</c> to the zone-0 container at
/// (x=7, y=0), and PICKLOCK.C copies that same actor into its work buffer, which is where
/// the residence name comes from.</para>
/// </summary>
public enum SaveGameContainerType : byte {
    /// <summary>RES_FREE — an unused record. Every travel zone ships 6-8 of these parked at
    /// zone 255 / (0,0) with capacity 20: they are the pool a discarded item claims, not
    /// "prototypes". <c>actorspawn_enc_location</c> (ACTSPAWN.C:170) takes the first one it
    /// finds and stamps it into a <see cref="Bag"/> at the party's feet; emptying that bag
    /// returns the record here (<c>actorspawn_destroy_and_persist</c>).</summary>
    Free           = 0,
    Inventory      = 1,   // party character inventories
    /// <summary>RES_ENCOUNTER — a bag on the ground, claimed from the <see cref="Free"/> pool
    /// when the party discards an item (TBL entity 166 "bag", world_item_id 0xA6).</summary>
    Bag            = 2,
    Chest          = 4,   // ambient chests (mostly locked)
    Corpse         = 5,   // ambient corpses
    FixedWorldItem = 6,   // fixed objects: shops, signs, wells
    NpcInventory   = 7,   // Zone 100 NPC/monster inventories
    SharedKeys     = 8,   // the party's ONE shared keys inventory (DOS RES_PICKLOCK_BUFFER)
    /// <summary>RES_SELF_SPAWN — hand-placed special loot under any entity. The residence name
    /// comes from <c>actorspawn_for_location_by_time</c>, which spawns a record into the visible
    /// pool when it carries this residence (or the <see cref="SaveGameContainerDataType.SelfSpawn"/>
    /// flag); the domain name is kept because that is what the placements are used for.</summary>
    ScriptedLoot   = 9,
}
