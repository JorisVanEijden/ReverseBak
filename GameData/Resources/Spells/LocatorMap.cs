namespace GameData.Resources.Spells;

using GameData.Resources.World;
using System;

/// <summary>
/// What the three locator spells actually mark — the <c>REQ_CMAP</c> marker passes reached from
/// <c>CastLocatorSpell</c> (IDA 0x6d062).
/// </summary>
/// <remarks>
/// <b>The display is the overhead map, borrowed.</b> The spell sets the same map-mode flag the
/// overhead map screen sets, lifts the camera to the zone's <b>maximum</b> zoom, shrinks the world
/// viewport to an inset and overlays REQ_CMAP — then puts the viewport and the camera back. So the
/// port is a camera and clip-rect change over the live world plus a marker pass, not a screen with
/// its own art. See <see cref="FieldSpells.LocatorViewport"/> and
/// <see cref="World.LocalMapScreen"/>.
///
/// <para><b>Which spell searches for what was recorded in IDA as an unverified guess. It is not a
/// guess any more.</b> Each pass gates on the world item's entity-type byte, then on range, and two
/// of the three then ask the item what it holds: <c>HasFood</c> for The Unseen (0x44c2e) and
/// <c>HasMagic</c> for Nacre Cicatrix (0x44f2c). The type sets say the same thing — the food pass is
/// the only one that takes the three bushes, dead animals and corpses; the magic pass is the only
/// one that takes rift machines and crystals.</para>
/// </remarks>
public static class LocatorMap {
    /// <summary>The REQ overlaid on the inset.</summary>
    public const string Layout = "REQ_CMAP.DAT";

    /// <summary>
    /// How near a thing must be to be marked: <b>one tile</b> (64000 world units).
    /// </summary>
    /// <remarks>
    /// Measured across the ground (<c>approxDistanceXY</c>) and reduced by the object's own extent,
    /// so a big thing is found from further out than a small one at the same centre distance. All
    /// three passes use the same figure.
    /// </remarks>
    public const int MarkerRange = 64000;

    /// <summary>
    /// <b>The camera goes to the zone's maximum map height, not to the remembered zoom.</b>
    /// </summary>
    /// <remarks>
    /// <c>CastLocatorSpell</c> overwrites the height with <c>mapMaxZ</c> after the map-mode snap, so
    /// the locator always shows the widest view whatever the player left the overhead map at — and
    /// the player's own zoom survives, because the spell saves and restores it.
    /// </remarks>
    public static bool OpensAtMaximumZoom => true;

    /// <summary>The kinds each search marks, by entity type.</summary>
    /// <remarks>
    /// Read off the three jump tables. Every set contains the plain containers and the fixed
    /// furniture that can hold things; the differences are the point.
    /// </remarks>
    public static WorldEntityType[] MarkedTypesFor(FieldSpells.LocatorTarget target) {
        switch (target) {
            // No graves, no corpses, no bushes — and the only set with a catapult.
            case FieldSpells.LocatorTarget.Valuables:
                return new[] {
                    WorldEntityType.Container, WorldEntityType.Building, WorldEntityType.Dirt,
                    WorldEntityType.StoneSlab, WorldEntityType.TreeStump, WorldEntityType.Well,
                    WorldEntityType.SiegeEngine, WorldEntityType.Catapult, WorldEntityType.Bag,
                };

            // The three bushes, dead animals, corpses and graves — the things food comes from.
            case FieldSpells.LocatorTarget.Food:
                return new[] {
                    WorldEntityType.Container, WorldEntityType.Building, WorldEntityType.Grave,
                    WorldEntityType.Corpse, WorldEntityType.Dirt, WorldEntityType.Bush,
                    WorldEntityType.BushPoison, WorldEntityType.BushHealing, WorldEntityType.TreeStump,
                    WorldEntityType.Well, WorldEntityType.SiegeEngine, WorldEntityType.DeadAnimal,
                    WorldEntityType.Bag,
                };

            // The widest set, and the only one that takes rift machines, crystals and pillars.
            case FieldSpells.LocatorTarget.Magic:
                return new[] {
                    WorldEntityType.Container, WorldEntityType.RiftMachine, WorldEntityType.Building,
                    WorldEntityType.Grave, WorldEntityType.Corpse, WorldEntityType.Dirt,
                    WorldEntityType.Crystals, WorldEntityType.Bush, WorldEntityType.BushPoison,
                    WorldEntityType.BushHealing, WorldEntityType.StoneSlab, WorldEntityType.TreeStump,
                    WorldEntityType.Well, WorldEntityType.SiegeEngine, WorldEntityType.DeadAnimal,
                    WorldEntityType.Pillar, WorldEntityType.Bag,
                };

            default:
                return Array.Empty<WorldEntityType>();
        }
    }

    /// <summary>Whether a search asks the object what it holds, or takes it on kind alone.</summary>
    /// <remarks>
    /// <b>The valuables search asks nothing.</b> It marks every chest, building, dig, slab, stump,
    /// well, siege engine, catapult and bag in range whether or not there is anything in it — so it
    /// finds places worth opening rather than confirmed treasure. The other two do ask: an object
    /// only carries a food or magic marker if it actually holds such a thing.
    /// </remarks>
    public static bool ChecksContents(FieldSpells.LocatorTarget target) =>
        target == FieldSpells.LocatorTarget.Food || target == FieldSpells.LocatorTarget.Magic;

    /// <summary>
    /// Kinds the magic search marks <b>without</b> asking what they hold, because they are the magic.
    /// </summary>
    /// <remarks>
    /// A rift machine, a stone slab and a pillar are tested for by type immediately before the
    /// <c>HasMagic</c> call and skip it (0x44f13-0x44f26). Nothing equivalent exists in the other two
    /// passes.
    /// </remarks>
    public static bool IsMagicalInItself(WorldEntityType type) =>
        type == WorldEntityType.RiftMachine
        || type == WorldEntityType.StoneSlab
        || type == WorldEntityType.Pillar;

    /// <summary>Whether this thing gets a marker.</summary>
    /// <param name="target">Which of the three searches is running.</param>
    /// <param name="type">The world item's entity type.</param>
    /// <param name="groundDistance">Distance across the ground from the party to the item.</param>
    /// <param name="extent">The item type's own extent, subtracted from the distance.</param>
    /// <param name="holdsFood">Whether the item holds food (only asked of the food search).</param>
    /// <param name="holdsMagic">Whether the item holds magic (only asked of the magic search).</param>
    public static bool Marks(FieldSpells.LocatorTarget target, WorldEntityType type,
        long groundDistance, long extent, bool holdsFood, bool holdsMagic) {
        if (Array.IndexOf(MarkedTypesFor(target), type) < 0) {
            return false;
        }

        if (groundDistance - extent >= MarkerRange) {
            return false;
        }

        switch (target) {
            case FieldSpells.LocatorTarget.Food:
                return holdsFood;
            case FieldSpells.LocatorTarget.Magic:
                return IsMagicalInItself(type) || holdsMagic;
            default:
                return true;
        }
    }

    /// <summary>
    /// <b>The markers follow the map's rotation, not the compass.</b>
    /// </summary>
    /// <remarks>
    /// Each pass saves the camera's yaw and zeroes it for the duration when the north-up option is
    /// set, exactly as the overhead map's own render does — so the markers and the terrain under
    /// them always agree about which way is up.
    /// </remarks>
    public static int MarkersDrawnWithYaw(int partyYaw, bool northUp) => northUp ? 0 : partyYaw;
}
