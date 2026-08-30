namespace ResourceExtraction;

using GameData.Resources.Data;
using GameData.Resources.World;
using System.Collections.Generic;

/// <summary>
/// RE'd map from an entity's interactable-type byte (TableDatInfo.EntityType, DOS
/// <c>HandleEnvironmentInteraction</c> @0x76573 switch) to its semantic behavior key +
/// data-driven <see cref="InteractionProfile"/>. This is code knowledge (baked in the EXE switch),
/// surfaced as engine-independent data.
///
/// <para><b>Why every row below is <c>"container"</c>.</b> Seventeen of the DOS handlers are the
/// same routine with different constants: play the click sound, anchor the dialog on the picked
/// rect, then — on a right-click show the examine ddx; on a left-click look up the container at
/// the world item's position and show its own dialog if it has one, the handler's default action
/// ddx if not, and the not-actionable ddx if there is no container or it is the wrong type.
/// <see cref="InteractionDialogResolver"/> IS that routine, so those seventeen types need no code
/// at all — only the constants, which is what this table holds.</para>
///
/// <para><b>The three describe-only types</b> (Ashes, RockPile, Corn) never look at a container:
/// their handlers are a two-way branch on the mouse button. An empty
/// <see cref="InteractionProfile.ActionableContainerTypes"/> reproduces that exactly rather than
/// approximately — with no actionable type, EVERY container state resolves to
/// <see cref="InteractionProfile.NotActionableDialogId"/>, so the left-click answer is that one
/// dialog whatever is or is not at the location, which is the original's behaviour for all
/// inputs.</para>
///
/// <para><b>Deliberately absent</b>, because they are not this shape and would need real code:
/// Grave (12) fires a positioned trap encounter and needs a Shovel in the party and a dig
/// (@0x77df4); Catapult (36) and RiftMachine (9) are scripted props gated on encounter globals;
/// Pit (15) needs a Rope and a rope-swing sequence (<c>handle_Pit</c> @0x79c63, modelled in
/// <c>PitRopeCrossing</c>).</para>
///
/// <para><b>Correction, 2026-08-30: Pit (15) IS clickable.</b> This paragraph said it "is the
/// walk-into-it traversal and has no click at all", which would stop anyone giving it a row. It is
/// BOTH — the <c>m_pit</c> polygon is walkable and falling in is delivered by the movement loop
/// (that is <c>PitDescent</c>, already wired), while the pit OBJECT is <b>case 15 of the click
/// jump table</b> at <c>HandleEnvironmentInteraction_impl</c> @0x766ad, dispatching to
/// <c>handle_Pit</c> exactly as RockPile and the rest do. Two code paths on one entity type, which
/// is what made the single-sentence summary wrong rather than merely incomplete.</para> <b>Door (23), Building (10) and
/// the clickable traversal trio Tunnel (20) / TunnelExit (39) / Ladder (42) have since been
/// added</b> — none is describe-or-loot, but each has a behavior of its own
/// (<c>DoorMechanics</c>, <c>FixedObjectClick</c>, <c>TraversalClick</c>) and an intentionally
/// empty profile.</para>
/// </summary>
public static class InteractionProfileTable {
    // Every handler's "there is nothing here" ddx: "@0 shrugged. 'This must not be very
    // important,' he said as he turned to leave."
    private const int NotImportant = 154;

    // The container types a fixed world object's loot/dialog lives on. Most of the scenery
    // handlers accept only containerType_fixedWorldItem (6); the two that also take a
    // hand-placed ScriptedLoot (9) say so on their own row.
    private static readonly SaveGameContainerType[] Fixed = { SaveGameContainerType.FixedWorldItem };

    private static readonly SaveGameContainerType[] FixedOrScripted =
        { SaveGameContainerType.FixedWorldItem, SaveGameContainerType.ScriptedLoot };

    // No actionable type at all — see the describe-only note in the class remarks.
    private static readonly SaveGameContainerType[] None = System.Array.Empty<SaveGameContainerType>();

    private static readonly Dictionary<WorldEntityType, (string Behavior, InteractionProfile Profile)> Map = new() {
        // byte 23 = door (handle_Door @0x778df). *** NOT A CONTAINER, and the empty profile says so
        // rather than being an oversight. *** A door has no loot, no actionable container type and
        // no SaveGameContainerLockData; its lock is a bare difficulty byte on the placement record
        // (FixedObjectAccess.LockValue) and its identity a door variant, so every field the
        // container handlers read is genuinely absent here. What it needs instead is
        // DoorMechanics, which is why it gets its own behavior key.
        [WorldEntityType.Door] = ("door", new InteractionProfile {
            Range = null,
            ActionableContainerTypes = None,
            ExamineDialogId = 0,
            ActionDialogId = 0,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
            HasLock = false,
        }),
        // byte 10 = building or town gate (wcursor_click_fixedobj_full, WCURSOR.C:259). *** NOT A
        // CONTAINER EITHER, and like the door the empty profile is the point. *** A building is a
        // way IN: an unlocked one with a warp on its hotspot subrecord hands the party to a GDS
        // town scene. What it needs is FixedObjectClick, which is why it gets its own key.
        //
        // Range stays null because the original's reach test is not a distance at all — a
        // hotspot-bearing object is clickable only from the party's own TILE, and silently
        // otherwise (FixedObjectClick.IsWithinReach). A radius here would answer a different
        // question and let a building be clicked from the next tile along.
        //
        // HasLock is false for the same reason the door's is: a building's lock is a lookup key on
        // its params subrecord, not SaveGameContainerLockData. NotActionableDialogId is the shared
        // 154 = 0x9a, which happens to be exactly the "nothing happens" record the click itself
        // plays when the object has nothing to offer.
        [WorldEntityType.Building] = ("building", new InteractionProfile {
            Range = null,
            ActionableContainerTypes = None,
            ExamineDialogId = 0,
            ActionDialogId = 0,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
            HasLock = false,
        }),
        // bytes 20 / 39 / 42 = tunnel, tunnel exit and ladder
        // (wcursor_click_fixedobj_picklock, WCURSOR.C:999). *** THE THIRD EMPTY PROFILE, and the
        // same reasoning as the door and the building. *** These are the level-traversal mechanic:
        // the click runs a lock and then plays the object's own message, whose Teleport action is
        // what moves the party. No loot, no container type, and the lock is a lookup key on the
        // params subrecord rather than SaveGameContainerLockData — so every field a container
        // profile carries is absent. The rules are TraversalClick.
        //
        // Range is null because this handler has no reach test at all: unlike the building click it
        // never compares tiles, so a radius here would invent a restriction and make distant
        // ladders silently unclickable.
        [WorldEntityType.Tunnel] = ("traversal", new InteractionProfile {
            Range = null,
            ActionableContainerTypes = None,
            ExamineDialogId = 0,
            ActionDialogId = 0,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
            HasLock = false,
        }),
        [WorldEntityType.TunnelExit] = ("traversal", new InteractionProfile {
            Range = null,
            ActionableContainerTypes = None,
            ExamineDialogId = 0,
            ActionDialogId = 0,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
            HasLock = false,
        }),
        [WorldEntityType.Ladder] = ("traversal", new InteractionProfile {
            Range = null,
            ActionableContainerTypes = None,
            ExamineDialogId = 0,
            ActionDialogId = 0,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
            HasLock = false,
        }),
        // byte 15 = pit (handle_Pit @0x79c63). Its profile is EMPTY ON PURPOSE, and that is what
        // the row says: every field here describes describe-or-loot, and a pit is neither. No
        // examine line, no message to play, no container, no lock — a click offers a rope swing and
        // every rule for that is PitRopeCrossing.
        //
        // Range is null for the reason the traversal rows' is: handle_Pit gates on the party's
        // position against the pit's own axis band (PitRopeCrossing.IsLinedUp), not on a radius, so
        // a range here would stack a second and wrong gate on top of the real one.
        //
        // NotActionableDialogId is NotImportant rather than a "you have no rope" line, deliberately:
        // the original checks the rope count BEFORE it looks at the pit and takes a path that says
        // nothing. Explaining the missing rope would be more helpful than the game and less
        // faithful. The 0x114 rope message belongs to running OUT mid-crossing, a different moment.
        [WorldEntityType.Pit] = ("pit", new InteractionProfile {
            Range = null,
            ActionableContainerTypes = None,
            ExamineDialogId = 0,
            ActionDialogId = 0,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
            HasLock = false,
        }),
        // byte 16 = corpse (handle_Corpse @0x76a0a). The only handler with a proximity gate.
        [WorldEntityType.Corpse] = ("container", new InteractionProfile {
            Range = new InteractionRange(7000, 2500),
            ActionableContainerTypes = new[] { SaveGameContainerType.Corpse, SaveGameContainerType.ScriptedLoot },
            ExamineDialogId = 94,
            ActionDialogId = 78,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
            HasLock = false,
        }),
        // byte 6 = container/chest (HandleEnvironmentInteraction @0x76573 case 6). Its dialogs are
        // lock-state-driven in the handler, not by the resolver, hence the zeros.
        [WorldEntityType.Container] = ("container", new InteractionProfile {
            Range = null,
            ActionableContainerTypes = new[] { SaveGameContainerType.Chest, SaveGameContainerType.ScriptedLoot },
            ExamineDialogId = 0,
            ActionDialogId = 0,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
            HasLock = true,
        }),

        // --- loots ---------------------------------------------------------------------------

        // byte 41 = bag (handle_Bag @0x76905). The ONLY handler keyed on containerType_2 — the
        // runtime drop-bag — rather than on the fixed-world-item type.
        [WorldEntityType.Bag] = ("container", new InteractionProfile {
            ActionableContainerTypes = new[] { SaveGameContainerType.Bag },
            ExamineDialogId = 93,
            ActionDialogId = 158,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),
        // byte 35 = dead animal in a hunter's trap (handle_DeadAnimal @0x777e3).
        [WorldEntityType.DeadAnimal] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 172,
            ActionDialogId = 171,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),
        // byte 17 = mound of dirt (handle_Dirt @0x7805e). Takes a ScriptedLoot as well as a fixed
        // item (@0x780d2), which is how a hand-placed cache is buried under one.
        [WorldEntityType.Dirt] = ("container", new InteractionProfile {
            ActionableContainerTypes = FixedOrScripted,
            ExamineDialogId = 155,
            ActionDialogId = 15,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),
        // byte 30 = hollow tree stump (handle_treeStump @0x787be).
        [WorldEntityType.TreeStump] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 187,
            ActionDialogId = 186,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),
        // byte 24 = crystal formation (handle_Crystals @0x781c4).
        [WorldEntityType.Crystals] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 179,
            ActionDialogId = 178,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),
        // byte 33 = siege engine (handle_SiegeEngine @0x78513).
        [WorldEntityType.SiegeEngine] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 184,
            ActionDialogId = 183,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),

        // Bushes (handle_Bush @0x76ed7): one handler, three subtypes, three pairs of dialogs.
        // The handler re-reads the world item's own type byte and switches on it (@0x76fae for
        // the action ddx, @0x7700f for the examine ddx), so each byte is its own row.
        [WorldEntityType.Bush] = ("container", new InteractionProfile {         // 26: edible berries
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 162,
            ActionDialogId = 159,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),
        [WorldEntityType.BushPoison] = ("container", new InteractionProfile {   // 27: "taste a bit funny"
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 164,
            ActionDialogId = 161,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),
        [WorldEntityType.BushHealing] = ("container", new InteractionProfile {  // 28: restorative berries
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 163,
            ActionDialogId = 160,
            NotActionableDialogId = NotImportant,
            OpensLoot = true,
        }),

        // --- describes, container-backed ------------------------------------------------------
        // These four resolve a container so a hand-placed dialog can override the default, but
        // never open it: no handler of theirs calls the loot entry point.

        // byte 13 = way marker / signpost (handle_WayMarker @0x7860f). It has no default action
        // ddx of its own — with no per-container dialog it falls through to the not-important
        // one (@0x786a4), which is why both ids are 154.
        [WorldEntityType.WayMarker] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 97,
            ActionDialogId = NotImportant,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
        }),
        // byte 31 = well (handle_Well @0x78b7e). Unusually, its no-container and wrong-type paths
        // land on the DRINK dialog (@0x78be6/@0x78bf0 both jump to `useWell`), not on the
        // not-important one — so a well always works whether or not anything is placed under it.
        [WorldEntityType.Well] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 189,
            ActionDialogId = 188,
            NotActionableDialogId = 188,
            OpensLoot = false,
        }),
        // byte 29 = stone slab (handle_StoneSlab @0x786e5). Like the way marker: no default of
        // its own, so a slab with nothing placed under it is "not very important".
        [WorldEntityType.StoneSlab] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 185,
            ActionDialogId = NotImportant,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
        }),
        // byte 37 = alien pillar / column (handle_Pillar @0x776b0). Same shape as the slab.
        [WorldEntityType.Pillar] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 168,
            ActionDialogId = NotImportant,
            NotActionableDialogId = NotImportant,
            OpensLoot = false,
        }),
        // byte 34 = scarecrow (handle_ScareCrow @0x7843a). Like the well, every path that is not
        // a per-container dialog converges on one line — "it wouldn't budge" (@0x784cf).
        [WorldEntityType.ScareCrow] = ("container", new InteractionProfile {
            ActionableContainerTypes = Fixed,
            ExamineDialogId = 182,
            ActionDialogId = 181,
            NotActionableDialogId = 181,
            OpensLoot = false,
        }),

        // --- describes, no container at all ----------------------------------------------------
        // See the class remarks for why an empty actionable list is exact here and not a stand-in.

        // byte 19 = cold ashes of a campfire (handle_Ashes @0x77050).
        [WorldEntityType.Ashes] = ("container", new InteractionProfile {
            ActionableContainerTypes = None,
            ExamineDialogId = 166,
            ActionDialogId = 165,
            NotActionableDialogId = 165,
            OpensLoot = false,
        }),
        // byte 25 = pile of rocks (handle_RockPile @0x7816a).
        [WorldEntityType.RockPile] = ("container", new InteractionProfile {
            ActionableContainerTypes = None,
            ExamineDialogId = 176,
            ActionDialogId = 175,
            NotActionableDialogId = 175,
            OpensLoot = false,
        }),
        // byte 18 = corn (handle_Corn @0x77789).
        [WorldEntityType.Corn] = ("container", new InteractionProfile {
            ActionableContainerTypes = None,
            ExamineDialogId = 170,
            ActionDialogId = 169,
            NotActionableDialogId = 169,
            OpensLoot = false,
        }),
    };

    public static bool TryGet(WorldEntityType entityType, out string behavior, out InteractionProfile profile) {
        if (Map.TryGetValue(entityType, out var e)) {
            behavior = e.Behavior;
            profile = e.Profile;
            return true;
        }
        behavior = null!;
        profile = null!;
        return false;
    }
}
