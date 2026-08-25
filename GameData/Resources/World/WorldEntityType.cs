namespace GameData.Resources.World;

/// <summary>
/// The world-item interactable-class byte (TableDatInfo.EntityType, DOS
/// HandleEnvironmentInteraction @0x76573 switch; case N = byte N). Values 0–5,7,8,11,14,21,22,32,
/// 38,40 are terrain / decorative (no handler). 20/39 are Tunnel/TunnelExit.
///
/// <para><b>TYPES 0, 1 AND 3 ARE THE TERRAIN PAINT STACK — the three that have never been named,
/// and the only three in the game that carry a <c>DrawPriority</c>.</b> Across all twelve zone
/// tables the priority is non-zero for exactly those types (0 at 8, 1 at 7, 3 at 6) and zero for
/// every other type; everything else is drawn by the ordinary far-to-near sort. The
/// <c>g######</c> / <c>t######</c> / <c>r######</c> name families share a zone+tile suffix — 43 of
/// the 44 <c>g</c> tiles have a <c>t</c> twin — so they are the same tile painted at three layers
/// rather than three unrelated kinds. Pinned by <c>TerrainPaintStackTests</c>.</para>
///
/// <para>This is not decoration: <c>ProximityWorld.SortLikeTheRenderer</c> splits collision
/// candidates on <c>DrawPriority != 0</c> to reproduce the renderer's "proud geometry first"
/// ordering, so <b>collision already depends on exactly this set</b>.</para>
///
/// <para><b>NAMED 2026-08-25, from the original rather than from the letters.</b> Three
/// independent sources agree:</para>
/// <list type="bullet">
///   <item><b>Type 1 is <see cref="Road"/>, straight from the original's own travel gate.</b>
///     <c>worldmove_prox_query_at_cell</c> (WORLDMOV.C:524) reads
///     <c>ts_get_shape(...)-&gt;kind</c> — this byte — and returns true for <b>1 or 2 only</b>.
///     Type 2 is already named <c>bridge*</c> in the data, so 1 is the road it pairs with. Its
///     faces confirm it: 2151 of them use pen 1 (Road) and 619 pen 2 (Path), and almost nothing
///     else.</item>
///   <item><b>Type 3 is <see cref="Water"/>.</b> Its faces are pen 3 (River) 422 to 82, it is the
///     one paint layer that is NOT walkable, and its single human-named member is literally
///     <c>water</c>.</item>
///   <item><b>Type 0 is <see cref="Ground"/>.</b> Nine entries are named <c>ground</c>, its faces
///     are pens 0 and 5 (Ground and Dirt) plus the two shade ramps, and it is the bottom layer.</item>
/// </list>
///
/// <para><b>The type is not the layer — the PRIORITY is.</b> Six type-0 entries, all named
/// <c>field</c> (one per zone Z01-Z06), sit at priority 7 alongside type 1 rather than at 8 with the
/// rest of type 0. So the mapping is not a bijection, and a port that inferred the paint order from
/// the type would put every field on the wrong layer.</para>
///
/// <para>26–28 all route to <c>handle_Bush</c> (@0x76ed7) but are three DIFFERENT bushes: the
/// handler re-reads the world item's own subtype byte and picks a different pair of dialogs for
/// each (@0x76fae examine / @0x7700f describe). The DOS enum names them
/// <c>interactable_bush_food</c> / <c>_poison</c> / <c>_healing</c> = 26 / 27 / 28, which is why
/// they are three members here and not one.</para>
/// </summary>
public enum WorldEntityType : byte {
    Ground      = 0,   Road        = 1,   Water     = 3,
    Container   = 6,   RiftMachine = 9,   Building  = 10,  Grave      = 12,
    WayMarker   = 13,  Pit         = 15,  Corpse    = 16,  Dirt       = 17,
    Corn        = 18,  Ashes       = 19,  Tunnel    = 20,  Door       = 23,
    Crystals    = 24,  RockPile    = 25,  Bush      = 26,  BushPoison = 27,
    BushHealing = 28,  StoneSlab   = 29,  TreeStump = 30,  Well       = 31,
    SiegeEngine = 33,  ScareCrow   = 34,  DeadAnimal = 35, Catapult   = 36,
    Pillar      = 37,  TunnelExit  = 39,  Bag       = 41,  Ladder     = 42,
}
