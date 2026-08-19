namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using GameData.Resources.World;
using ResourceExtraction;
using Xunit;

public class InteractionProfileTableTests {
    [Fact]
    public void Corpse16_ResolvesToContainerBehaviorAndProfile() {
        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.Corpse, out string behavior, out InteractionProfile p));
        Assert.Equal("container", behavior);
        Assert.Equal(new InteractionRange(7000, 2500), p.Range);
        Assert.Contains(SaveGameContainerType.Corpse, p.ActionableContainerTypes);
        Assert.Contains(SaveGameContainerType.ScriptedLoot, p.ActionableContainerTypes);
        Assert.Equal(94, p.ExamineDialogId);
        Assert.Equal(78, p.ActionDialogId);
        Assert.Equal(154, p.NotActionableDialogId);
        Assert.True(p.OpensLoot);
        Assert.False(p.HasLock);
    }

    /// <summary>
    /// The traversal types still left out — a zone/level transition mechanic, not
    /// describe-or-loot, so a click on them must fall through to "no behavior" rather than
    /// silently borrowing the container one.
    /// </summary>
    /// <remarks>
    /// <b>Door was on this list and has come off it.</b> It is still not describe-or-loot; it now
    /// has a mechanic of its own (<c>DoorMechanics</c>) and a row that says so — see
    /// <see cref="ADoorIsMappedButToItsOwnBehaviourNotTheContainerOne"/>. The others stay here
    /// until they get the same treatment.
    /// </remarks>
    [Theory]
    [InlineData(WorldEntityType.Ladder)]
    [InlineData(WorldEntityType.Tunnel)]
    [InlineData(WorldEntityType.TunnelExit)]
    [InlineData(WorldEntityType.Pit)]
    public void TraversalTypes_AreNotMapped(WorldEntityType type) =>
        Assert.False(InteractionProfileTable.TryGet(type, out _, out _));

    /// <summary>
    /// A door IS mapped now — but to its own behaviour, and with an empty profile.
    /// </summary>
    /// <remarks>
    /// The distinction this asserts is the whole reason it was excluded before: borrowing
    /// "container" would show a door's dialogs while dropping the open/shut mechanic, which is the
    /// failure the old exclusion existed to prevent. A separate behaviour key keeps that guarantee
    /// while letting the door act.
    /// </remarks>
    [Fact]
    public void ADoorIsMappedButToItsOwnBehaviourNotTheContainerOne() {
        Assert.True(InteractionProfileTable.TryGet(
            WorldEntityType.Door, out string behavior, out InteractionProfile profile));
        Assert.Equal("door", behavior);
        Assert.NotEqual("container", behavior);
        Assert.Empty(profile.ActionableContainerTypes);
        Assert.False(profile.OpensLoot);
    }

    /// <summary>
    /// Grave, Catapult and RiftMachine fire trap encounters / GDS scenes and (for the grave)
    /// require a Shovel and a dig. Mapping them onto the plain container mechanism would show their
    /// dialogs while silently dropping the mechanic, which is worse than not responding — so their
    /// absence is asserted, not assumed.
    /// </summary>
    /// <remarks>
    /// <b>Building has left this list.</b> It went the way Door did: not onto the container
    /// mechanism, but onto a key of its own with an intentionally empty profile, so the mechanic
    /// lives in <c>FixedObjectClick</c> instead of being faked with dialogs.
    /// </remarks>
    [Theory]
    [InlineData(WorldEntityType.Grave)]
    [InlineData(WorldEntityType.Catapult)]
    [InlineData(WorldEntityType.RiftMachine)]
    public void ScriptedTypes_AreNotMapped(WorldEntityType type) =>
        Assert.False(InteractionProfileTable.TryGet(type, out _, out _));

    /// <summary>
    /// Building gets its own key and an empty profile — the same shape as the door.
    /// </summary>
    /// <remarks>
    /// <b>The empty profile is the assertion, not an oversight.</b> A building has no loot, no
    /// actionable container type and no container lock; it is a way IN, and its rules are
    /// <c>FixedObjectClick</c>. Mapping it to "container" would show a chest's dialogs over a town
    /// gate.
    ///
    /// <para>Range stays null because the original's reach test is a TILE comparison and not a
    /// radius — a radius here would let a gate be clicked from the next tile along.</para>
    /// </remarks>
    [Fact]
    public void Building10_HasItsOwnKeyAndAnEmptyProfile() {
        Assert.True(InteractionProfileTable.TryGet(
            WorldEntityType.Building, out string behavior, out InteractionProfile profile));
        Assert.Equal("building", behavior);
        Assert.NotEqual("container", behavior);
        Assert.Empty(profile.ActionableContainerTypes);
        Assert.False(profile.OpensLoot);
        Assert.False(profile.HasLock);
        Assert.Null(profile.Range);
    }

    [Fact]
    public void Container6_ResolvesToChestProfile() {
        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.Container,
            out string behavior, out InteractionProfile p));
        Assert.Equal("container", behavior);
        Assert.Contains(SaveGameContainerType.Chest, p.ActionableContainerTypes);
        Assert.Contains(SaveGameContainerType.ScriptedLoot, p.ActionableContainerTypes);
        Assert.True(p.OpensLoot);
        Assert.True(p.HasLock);
        Assert.Null(p.Range);
    }

    /// <summary>
    /// The dialog ids of every ported handler, against the <c>push</c> constants in the DOS
    /// routine named on each row. A table like this is only worth anything if the numbers are
    /// pinned: a transposed examine/action pair still produces a dialog, just the wrong one, and
    /// nothing else in the build would notice.
    /// </summary>
    [Theory]
    // type                              examine  action  notActionable  loot
    [InlineData(WorldEntityType.Bag,          93,    158,          154,  true)]  // handle_Bag @0x76905
    [InlineData(WorldEntityType.DeadAnimal,  172,    171,          154,  true)]  // handle_DeadAnimal @0x777e3
    [InlineData(WorldEntityType.Dirt,        155,     15,          154,  true)]  // handle_Dirt @0x7805e
    [InlineData(WorldEntityType.TreeStump,   187,    186,          154,  true)]  // handle_treeStump @0x787be
    [InlineData(WorldEntityType.Crystals,    179,    178,          154,  true)]  // handle_Crystals @0x781c4
    [InlineData(WorldEntityType.SiegeEngine, 184,    183,          154,  true)]  // handle_SiegeEngine @0x78513
    [InlineData(WorldEntityType.Bush,        162,    159,          154,  true)]  // handle_Bush @0x76ed7, byte 26
    [InlineData(WorldEntityType.BushPoison,  164,    161,          154,  true)]  // handle_Bush, byte 27
    [InlineData(WorldEntityType.BushHealing, 163,    160,          154,  true)]  // handle_Bush, byte 28
    [InlineData(WorldEntityType.WayMarker,    97,    154,          154,  false)] // handle_WayMarker @0x7860f
    [InlineData(WorldEntityType.Well,        189,    188,          188,  false)] // handle_Well @0x78b7e
    [InlineData(WorldEntityType.StoneSlab,   185,    154,          154,  false)] // handle_StoneSlab @0x786e5
    [InlineData(WorldEntityType.Pillar,      168,    154,          154,  false)] // handle_Pillar @0x776b0
    [InlineData(WorldEntityType.ScareCrow,   182,    181,          181,  false)] // handle_ScareCrow @0x7843a
    [InlineData(WorldEntityType.Ashes,       166,    165,          165,  false)] // handle_Ashes @0x77050
    [InlineData(WorldEntityType.RockPile,    176,    175,          175,  false)] // handle_RockPile @0x7816a
    [InlineData(WorldEntityType.Corn,        170,    169,          169,  false)] // handle_Corn @0x77789
    public void PortedHandlers_CarryTheirDosDialogIds(
        WorldEntityType type, int examine, int action, int notActionable, bool opensLoot) {
        Assert.True(InteractionProfileTable.TryGet(type, out string behavior, out InteractionProfile p));
        Assert.Equal("container", behavior);
        Assert.Equal(examine, p.ExamineDialogId);
        Assert.Equal(action, p.ActionDialogId);
        Assert.Equal(notActionable, p.NotActionableDialogId);
        Assert.Equal(opensLoot, p.OpensLoot);
        Assert.False(p.HasLock);
    }

    /// <summary>
    /// Only the corpse has a proximity gate. <c>handle_Corpse</c> is the one handler that opens
    /// with a GlobalKey depth test (@0x76a14-@0x76a35) and returns without a sound if the party is
    /// too far; every other handler plays the click the moment it is entered.
    /// </summary>
    [Theory]
    [InlineData(WorldEntityType.Bag)]
    [InlineData(WorldEntityType.Well)]
    [InlineData(WorldEntityType.Ashes)]
    [InlineData(WorldEntityType.Crystals)]
    public void OnlyTheCorpse_HasAProximityGate(WorldEntityType type) {
        Assert.True(InteractionProfileTable.TryGet(type, out _, out InteractionProfile p));
        Assert.Null(p.Range);
    }

    /// <summary>
    /// The bag is the only handler keyed on the runtime drop-bag container type
    /// (<c>containerType_2</c> at @0x76972); every other fixed-world object takes type 6. Getting
    /// this wrong makes a dropped bag un-openable while leaving every other type working.
    /// </summary>
    [Fact]
    public void TheBag_IsKeyedOnTheDropBagContainerType() {
        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.Bag, out _, out InteractionProfile bag));
        Assert.Equal(new[] { SaveGameContainerType.Bag }, bag.ActionableContainerTypes);

        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.TreeStump, out _, out InteractionProfile stump));
        Assert.Equal(new[] { SaveGameContainerType.FixedWorldItem }, stump.ActionableContainerTypes);
    }

    /// <summary>
    /// Dirt also accepts a hand-placed <see cref="SaveGameContainerType.ScriptedLoot"/> (the
    /// second type test at @0x780d2) — that is how a cache gets buried under a mound. Its
    /// neighbours in the table do not.
    /// </summary>
    [Fact]
    public void Dirt_AlsoAcceptsScriptedLoot() {
        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.Dirt, out _, out InteractionProfile dirt));
        Assert.Contains(SaveGameContainerType.ScriptedLoot, dirt.ActionableContainerTypes);
        Assert.Contains(SaveGameContainerType.FixedWorldItem, dirt.ActionableContainerTypes);

        Assert.True(InteractionProfileTable.TryGet(WorldEntityType.Crystals, out _, out InteractionProfile crystals));
        Assert.DoesNotContain(SaveGameContainerType.ScriptedLoot, crystals.ActionableContainerTypes);
    }

    /// <summary>
    /// The describe-only types must have NO actionable container type — that is what makes every
    /// container state resolve to the one left-click line, reproducing handlers that never look a
    /// container up at all. A stray type here would make them answer differently depending on
    /// what happens to be placed under them.
    /// </summary>
    [Theory]
    [InlineData(WorldEntityType.Ashes)]
    [InlineData(WorldEntityType.RockPile)]
    [InlineData(WorldEntityType.Corn)]
    public void DescribeOnlyTypes_HaveNoActionableContainerType(WorldEntityType type) {
        Assert.True(InteractionProfileTable.TryGet(type, out _, out InteractionProfile p));
        Assert.Empty(p.ActionableContainerTypes);
    }
}
