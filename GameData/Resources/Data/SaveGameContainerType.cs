namespace GameData.Resources.Data;

/// <summary>
/// Container-type byte (IDA <c>containerType</c>), ground truth from
/// generated/STARTUP.savegame.json + handler usage. The list mixes actor/template
/// inventories and placed world objects; only 4/5/6/9 reach HandleEnvironmentInteraction.
///
/// <para>The byte IS the DOS actor's <c>bResidence</c> (ACTOR.H): 1 = RES_PARTY_SLOT,
/// 4 = RES_CONTAINER, 5 = RES_BODY, 6 = RES_FIXED_OBJECT, 7 = RES_COMBAT,
/// 8 = RES_PICKLOCK_BUFFER. Type 8 was previously guessed to be a "cheat chest"; it is in
/// fact the party's shared keys inventory — <c>boot_party_state_load_from_temp</c>
/// (BOOT.C:132) binds <c>g_gameState.shared_inventory</c> to the zone-0 container at
/// (x=7, y=0), and PICKLOCK.C copies that same actor into its work buffer, which is where
/// the residence name comes from.</para>
/// </summary>
public enum SaveGameContainerType : byte {
    TemplatePool   = 0,   // Zone 255 prototype pools (not in world)
    Inventory      = 1,   // party character inventories
    Bag            = 2,   // runtime drop-bag (created live)
    Chest          = 4,   // ambient chests (mostly locked)
    Corpse         = 5,   // ambient corpses
    FixedWorldItem = 6,   // fixed objects: shops, signs, wells
    NpcInventory   = 7,   // Zone 100 NPC/monster inventories
    SharedKeys     = 8,   // the party's ONE shared keys inventory (DOS RES_PICKLOCK_BUFFER)
    ScriptedLoot   = 9,   // hand-placed special loot under any entity
}
