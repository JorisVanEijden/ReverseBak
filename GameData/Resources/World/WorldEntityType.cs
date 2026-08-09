namespace GameData.Resources.World;

/// <summary>
/// The world-item interactable-class byte (TableDatInfo.EntityType, DOS
/// HandleEnvironmentInteraction @0x76573 switch; case N = byte N). Values 0–5,7,8,11,14,21,22,32,
/// 38,40 are terrain / decorative (no handler). 20/39 are Tunnel/TunnelExit.
///
/// <para>26–28 all route to <c>handle_Bush</c> (@0x76ed7) but are three DIFFERENT bushes: the
/// handler re-reads the world item's own subtype byte and picks a different pair of dialogs for
/// each (@0x76fae examine / @0x7700f describe). The DOS enum names them
/// <c>interactable_bush_food</c> / <c>_poison</c> / <c>_healing</c> = 26 / 27 / 28, which is why
/// they are three members here and not one.</para>
/// </summary>
public enum WorldEntityType : byte {
    Container   = 6,   RiftMachine = 9,   Building  = 10,  Grave      = 12,
    WayMarker   = 13,  Pit         = 15,  Corpse    = 16,  Dirt       = 17,
    Corn        = 18,  Ashes       = 19,  Tunnel    = 20,  Door       = 23,
    Crystals    = 24,  RockPile    = 25,  Bush      = 26,  BushPoison = 27,
    BushHealing = 28,  StoneSlab   = 29,  TreeStump = 30,  Well       = 31,
    SiegeEngine = 33,  ScareCrow   = 34,  DeadAnimal = 35, Catapult   = 36,
    Pillar      = 37,  TunnelExit  = 39,  Bag       = 41,  Ladder     = 42,
}
