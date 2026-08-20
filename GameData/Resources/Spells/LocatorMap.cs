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

    /// <summary>
    /// <b>A marker is a filled dot, not a sprite</b> — radius 2, drawn in pen
    /// <see cref="MarkerPen"/>.
    /// </summary>
    /// <remarks>
    /// There is no marker artwork anywhere in the archive to go looking for; the pass draws a
    /// circle straight into the frame buffer.
    /// </remarks>
    public const int MarkerRadius = 2;

    /// <summary>The pen a marker is drawn in: 111, a saturated red.</summary>
    /// <remarks>
    /// Like the overhead map's own party arrow, this pen holds the same RGB (215, 0, 0) in the UI
    /// palette and in all twelve zone palettes, so the dot is the same red whatever is installed and
    /// a port need not decide which palette the locator runs under.
    /// </remarks>
    public const int MarkerPen = 111;

    /// <summary>
    /// A dot within its own radius of the inset's edge is still drawn, and clipped.
    /// </summary>
    /// <remarks>
    /// The bounds test is widened by <see cref="MarkerRadius"/> on all four sides before the circle
    /// is drawn, so a thing just off the edge shows as a half dot rather than vanishing. Testing the
    /// centre alone would pop markers out a full radius early.
    /// </remarks>
    public const int MarkerClipSlack = MarkerRadius;

    /// <summary>
    /// <b>Use the map camera's own projection; do not reimplement this one.</b>
    /// </summary>
    /// <remarks>
    /// The original has no 3D pipeline to lean on for these dots, so it projects each one by hand:
    /// offset from the camera, scaled by <c>(1 &lt;&lt; zoom) / cameraHeight</c>, rotated by MINUS
    /// the camera yaw, added to the viewport centre with the Y axis flipped. That is the same
    /// mapping a top-down camera already performs — the scale is inversely proportional to height in
    /// both — so a port projects the world position with its own camera and gets the dots to land on
    /// the terrain they belong to for free. Reimplementing the fixed-point version would only give
    /// two projections to keep in step.
    ///
    /// <para>The yaw it rotates by is the one <see cref="MarkersDrawnWithYaw"/> chooses, which is
    /// why the pass brackets itself with a save and restore of the camera's yaw.</para>
    /// </remarks>
    public static bool MarkersUseTheCameraProjection => true;

    /// <summary>
    /// <b>Only two of the three searches look at fixed objects at all.</b>
    /// </summary>
    /// <remarks>
    /// The valuables pass runs two scans — the per-zone actor lists and the visible-entry pool — and
    /// stops. Food and magic run a third over the fixed-object list. Verified in IDA:
    /// <c>cmap_markValuablesNearby</c> (0x44918) makes exactly two calls where its two siblings make
    /// three, which is the whole of the seven-byte difference in their sizes.
    ///
    /// <para>So Eyes of Ishap cannot mark a fixed object, however valuable. A port that runs one
    /// uniform scan for all three spells marks things the original never does.</para>
    /// </remarks>
    public static bool ScansFixedObjects(FieldSpells.LocatorTarget target) =>
        target == FieldSpells.LocatorTarget.Food || target == FieldSpells.LocatorTarget.Magic;

    /// <summary>Whether a fixed object gets a marker.</summary>
    /// <remarks>
    /// <b>The fixed-object scan is a different rule, not the same rule over a third list.</b> It
    /// asks NO question about the entity type and does NOT subtract the object's extent — it is a
    /// plain centre-to-centre range test, and then the contents check. Both list scans do the
    /// opposite on both counts (see <see cref="Marks"/>).
    /// </remarks>
    /// <param name="target">Which of the three searches is running.</param>
    /// <param name="groundDistance">Distance across the ground from the party to the object.</param>
    /// <param name="holdsFood">Whether it holds food (asked by the food search).</param>
    /// <param name="holdsMagic">Whether it holds magic (asked by the magic search).</param>
    public static bool MarksFixedObject(FieldSpells.LocatorTarget target, long groundDistance,
        bool holdsFood, bool holdsMagic) {
        if (!ScansFixedObjects(target) || groundDistance >= MarkerRange) {
            return false;
        }

        return target == FieldSpells.LocatorTarget.Food ? holdsFood : holdsMagic;
    }

    /// <summary>Whether this thing gets a marker.</summary>
    /// <remarks>
    /// The rule for the two LIST scans — the per-zone actor lists and the visible-entry pool, which
    /// share one type set within a spell. Fixed objects are scanned separately and by a different
    /// rule; see <see cref="MarksFixedObject"/>.
    /// </remarks>
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
    /// What "holds food" means: rations, in any condition.
    /// </summary>
    /// <remarks>
    /// The food search opens the fixed-object container standing at the item's position and looks
    /// for any of three object ids — 72 Rations, 73 Rations (Poisoned), 74 Rations (Spoiled). The
    /// spell does not care whether the food is any good, so a cache of spoiled rations still lights
    /// up. Nothing else in the game counts, so this is not a "food category" to be widened later.
    /// </remarks>
    public static readonly int[] FoodItemIds = { 72, 73, 74 };

    /// <summary>
    /// What "holds magic" means: an explicit list of twenty-one object ids.
    /// </summary>
    /// <remarks>
    /// The three magical staves and the chapter artefacts, plus Enchanted Quarrels (43), Ring of the
    /// Golden Way (88), Weedwalkers (90), Restoratives (119) and a Magical Scroll (133). A list, not
    /// a flag on the object record — there is no "is magical" bit for this to read.
    ///
    /// <para><b>The run 1..17 deliberately skips 3, and 3 is the Wooden Staff.</b> 1 Crystal Staff,
    /// 2 Lightning Staff and 4 Staff of Macros are magical; the plain wooden one is not. A port that
    /// tidies the list into a range marks every wooden staff in the world.</para>
    /// </remarks>
    public static readonly int[] MagicItemIds = {
        1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 43, 88, 90, 119, 133,
    };

    /// <summary>Whether a container's contents satisfy a search that asks about contents.</summary>
    /// <remarks>
    /// One item is enough; the pass stops caring once anything matches. Valuables never reaches here
    /// (<see cref="ChecksContents"/>), so an unrecognised target holds nothing.
    /// </remarks>
    /// <param name="target">Which of the three searches is running.</param>
    /// <param name="itemIds">The object ids the container holds.</param>
    public static bool ContentsSatisfy(FieldSpells.LocatorTarget target,
        System.Collections.Generic.IEnumerable<int> itemIds) {
        if (itemIds == null || !ChecksContents(target)) {
            return false;
        }
        int[] wanted = target == FieldSpells.LocatorTarget.Food ? FoodItemIds : MagicItemIds;
        foreach (int id in itemIds) {
            if (Array.IndexOf(wanted, id) >= 0) {
                return true;
            }
        }

        return false;
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
