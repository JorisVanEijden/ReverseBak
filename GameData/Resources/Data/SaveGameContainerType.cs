namespace GameData.Resources.Data;

/// <summary>
/// Container-type byte (IDA <c>containerType</c>), ground truth from
/// generated/STARTUP.savegame.json + handler usage. The list mixes actor/template
/// inventories and placed world objects; only 4/5/6/9 reach HandleEnvironmentInteraction.
/// </summary>
public enum SaveGameContainerType : byte {
    TemplatePool   = 0,   // Zone 255 prototype pools (not in world)
    Inventory      = 1,   // party character inventories
    Bag            = 2,   // runtime drop-bag (created live)
    Chest          = 4,   // ambient chests (mostly locked)
    Corpse         = 5,   // ambient corpses
    FixedWorldItem = 6,   // fixed objects: shops, signs, wells
    NpcInventory   = 7,   // Zone 100 NPC/monster inventories
    CheatChest     = 8,   // debug/cheat chest
    ScriptedLoot   = 9,   // hand-placed special loot under any entity
}
