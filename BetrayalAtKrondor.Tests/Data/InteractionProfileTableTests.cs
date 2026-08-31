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
    /// Pit is mapped, and its profile is empty.
    /// </summary>
    /// <remarks>
    /// <b>THIS TEST USED TO ASSERT THE OPPOSITE, and the reason it gave was wrong.</b> It read:
    /// "Pit stays, and not because nobody has got to it. It is the one traversal you never click —
    /// you walk onto it — so it has no click handler to key." Two code paths share the type: a
    /// <c>m_pit</c> POLYGON is walkable and dropping in is the movement loop's, while the pit
    /// OBJECT is case 15 of the click dispatch and swings the party across on a rope
    /// (<c>handle_Pit</c> @0x79c63, rules in <c>PitRopeCrossing</c>).
    ///
    /// <para><b>And the "empty profile" this test asserted for a day was wrong too.</b> It pinned
    /// <c>ExamineDialogId = 0</c> and said asserting the emptiness stops a later pass "filling it in
    /// with an examine line the original does not have — including a helpful 'you have no rope',
    /// which the original pointedly does not say". The disassembly says otherwise on both counts: a
    /// secondary click shows dialog 177, and a missing rope shows 198. Two wrong claims defended by
    /// one confident test.</para>
    ///
    /// <para>What IS empty stays asserted, because those parts are real: no container, no lock, no
    /// loot, no range — the reach gate is the pit's own axis band, not a radius.</para>
    /// </remarks>
    [Fact]
    public void Pit_IsMappedAndCarriesOnlyItsExamineLine() {
        Assert.True(InteractionProfileTable.TryGet(
            WorldEntityType.Pit, out string behavior, out InteractionProfile profile));
        Assert.Equal("pit", behavior);
        Assert.Equal(GameData.Resources.World.PitRopeCrossing.ExamineDialog,
            profile.ExamineDialogId);
        Assert.Null(profile.Range);
        Assert.Equal(0, profile.ActionDialogId);
        Assert.False(profile.OpensLoot);
        Assert.False(profile.HasLock);
    }

    /// <summary>
    /// The clickable traversal trio share one behaviour key, with empty profiles.
    /// </summary>
    /// <remarks>
    /// <b>One key for three types is the point:</b> tunnel, tunnel exit and ladder all reach the
    /// same handler in the original, so three keys would be three copies of one mechanic. The
    /// profile is empty for the door's reasons — no loot, no container type, and a lock that lives
    /// on the params subrecord rather than in container lock data.
    ///
    /// <para>Range is null because this handler has NO reach test at all, unlike the building's
    /// tile guard — a radius here would invent a restriction the original does not have.</para>
    /// </remarks>
    [Theory]
    [InlineData(WorldEntityType.Ladder)]
    [InlineData(WorldEntityType.Tunnel)]
    [InlineData(WorldEntityType.TunnelExit)]
    public void TheTraversalTrioSharesOneBehaviourWithAnEmptyProfile(WorldEntityType type) {
        Assert.True(InteractionProfileTable.TryGet(type, out string behavior,
            out InteractionProfile profile));
        Assert.Equal("traversal", behavior);
        Assert.NotEqual("container", behavior);
        Assert.Empty(profile.ActionableContainerTypes);
        Assert.False(profile.OpensLoot);
        Assert.Null(profile.Range);
    }

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
    /// <b>THE "NOT MAPPED" LIST IS EMPTY, AND THAT IS THE RESULT.</b> It once held Building, Grave,
    /// Catapult and RiftMachine — every one of them "would need real code". Every one of them now
    /// has real code and a key of its own, so this asserts the list is closed rather than naming a
    /// member of it.
    /// </summary>
    /// <remarks>
    /// Deliberately not deleted with the last entry. The list existing is what stopped anyone
    /// mapping a scripted prop onto the plain container mechanism as a shortcut — "shows the dialogs
    /// while dropping the mechanic" — and a new type arriving unhandled should re-open it rather
    /// than quietly inherit "container".
    ///
    /// <para>The only types with no row are now <c>Ground</c>, <c>Road</c> and <c>Water</c>, which
    /// are terrain rather than objects.</para>
    /// </remarks>
    [Fact]
    public void EveryOBJECTTypeIsMapped_TheScriptedPropsIncluded() {
        foreach (WorldEntityType type in new[] {
                     WorldEntityType.Building, WorldEntityType.Grave,
                     WorldEntityType.Catapult, WorldEntityType.RiftMachine }) {
            Assert.True(InteractionProfileTable.TryGet(type, out string behavior, out _),
                $"{type} was on the 'needs real code' list and should now have a behavior key");
            Assert.NotEqual("container", behavior);
        }
    }

    /// <summary>
    /// The rift machine gets its own key and an empty profile — the sixth of that shape, and the
    /// last of the three this table once called deliberately absent.
    /// </summary>
    /// <remarks>
    /// Gated on an encounter global, it plays a sound and scrambles the WHOLE SCENE for ten frames.
    /// The object itself never animates, so there is nothing a profile could describe.
    /// </remarks>
    [Fact]
    public void RiftMachine9_HasItsOwnKeyAndAnEmptyProfile() {
        Assert.True(InteractionProfileTable.TryGet(
            WorldEntityType.RiftMachine, out string behavior, out InteractionProfile profile));
        Assert.Equal("rift", behavior);
        Assert.Empty(profile.ActionableContainerTypes);
        Assert.False(profile.OpensLoot);
        Assert.False(profile.HasLock);
    }

    /// <summary>
    /// The catapult gets its own key and an empty profile — the fifth of that shape, and the second
    /// to leave the list above in one day.
    /// </summary>
    /// <remarks>
    /// A scripted prop is not describe-or-loot: gated on an encounter global, it steps its mesh
    /// through a four-frame sequence and then plays its sound. <c>CatapultUse</c> carries that.
    /// </remarks>
    [Fact]
    public void Catapult36_HasItsOwnKeyAndAnEmptyProfile() {
        Assert.True(InteractionProfileTable.TryGet(
            WorldEntityType.Catapult, out string behavior, out InteractionProfile profile));
        Assert.Equal("catapult", behavior);
        Assert.NotEqual("container", behavior);
        Assert.Empty(profile.ActionableContainerTypes);
        Assert.False(profile.OpensLoot);
        Assert.False(profile.HasLock);
    }

    /// <summary>
    /// The grave gets its own key and an empty profile — the fourth of that shape.
    /// </summary>
    /// <remarks>
    /// <b>A dig is not expressible as a profile.</b> It wants a Shovel in the party, spends it, may
    /// spring a positioned trap encounter first, and only then opens the container or reports a body
    /// or an empty coffin. The rules are <c>GraveDigging</c>; this row exists for the behavior name.
    ///
    /// <para><c>ExamineDialogId</c> stays 0 rather than 173 <b>because one field cannot say what a
    /// non-diggable grave does</b>: a primary click shows the grave's OWN dialog (@0x77f1f) and only
    /// a secondary reads the tombstone.</para>
    /// </remarks>
    [Fact]
    public void Grave12_HasItsOwnKeyAndAnEmptyProfile() {
        Assert.True(InteractionProfileTable.TryGet(
            WorldEntityType.Grave, out string behavior, out InteractionProfile profile));
        Assert.Equal("grave", behavior);
        Assert.NotEqual("container", behavior);
        Assert.Empty(profile.ActionableContainerTypes);
        Assert.False(profile.OpensLoot);
        Assert.False(profile.HasLock);
        Assert.Equal(0, profile.ExamineDialogId);
    }

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
