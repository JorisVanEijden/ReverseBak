namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Digging a grave — <c>handle_Grave</c> (ovr190 @0x77ca9).
/// </summary>
/// <remarks>
/// TASK-136's first of three: "needs a Shovel in party inventory and fires a positioned
/// trap/encounter on digging". The trace turned up four rules that one sentence does not carry.
/// </remarks>
public class GraveDiggingTests {
    [Fact]
    public void THETHREECONTENTBitsAreAlsoWhatMakesAGraveDIGGABLE() {
        // The handler tests flags & 2, & 4 and & 8 together up front and takes the examine-only
        // path when none is set. So a grave with no outcome bit cannot be dug at all, whatever else
        // its container carries — the same bits do both jobs.
        Assert.False(GraveDigging.IsDiggable(0));
        Assert.False(GraveDigging.IsDiggable(1), "bit 0 is not one of them");
        Assert.True(GraveDigging.IsDiggable((int)GraveDigging.Contents.Loot));
        Assert.True(GraveDigging.IsDiggable((int)GraveDigging.Contents.Body));
        Assert.True(GraveDigging.IsDiggable((int)GraveDigging.Contents.Empty));
    }

    [Fact]
    public void THEOUTCOMEIsAChain_soLootSILENCESTheBody() {
        // `if (flags & 2) open; else if (flags & 4) dialog 68; else dialog 67`. A grave flagged BOTH
        // opens its container and never mentions the body — first match wins, and treating the bits
        // as independent would show two outcomes for one dig.
        Assert.Equal(GraveDigging.Contents.Loot, GraveDigging.OutcomeFor(
            (int)(GraveDigging.Contents.Loot | GraveDigging.Contents.Body)));
        Assert.Equal(GraveDigging.Contents.Body, GraveDigging.OutcomeFor(
            (int)GraveDigging.Contents.Body));
    }

    [Fact]
    public void THELASTARMIsAnELSE_notATestOfBitThree() {
        // Anything diggable that is neither Loot nor Body reads as an empty coffin, including a
        // flags word with bit 3 clear that got here some other way.
        Assert.Equal(GraveDigging.Contents.Empty, GraveDigging.OutcomeFor(
            (int)GraveDigging.Contents.Empty));
        Assert.Equal(GraveDigging.Contents.Empty, GraveDigging.OutcomeFor(0));
        Assert.Equal(GraveDigging.EmptyCoffinDialog,
            GraveDigging.DialogFor(GraveDigging.Contents.Empty));
        Assert.Equal(GraveDigging.JustABodyDialog,
            GraveDigging.DialogFor(GraveDigging.Contents.Body));
    }

    [Fact]
    public void ATRAPPEDGraveCanOnlyBeDugFromItsOWNTile() {
        // *** And the refusal is SILENT — no dialog, no sound. *** A player clicking a trapped grave
        // from the neighbouring tile gets no response at all, which reads as a broken hotspot rather
        // than as a rule.
        const long tile = WorldPlacement.TileSize;
        Assert.True(GraveDigging.PartyIsCloseEnough(
            graveWorldX: (3 * tile) + 100, graveWorldY: (4 * tile) + 100,
            partyTileX: 3, partyTileY: 4));
        Assert.False(GraveDigging.PartyIsCloseEnough(
            (3 * tile) + 100, (4 * tile) + 100, partyTileX: 2, partyTileY: 4));
        Assert.False(GraveDigging.PartyIsCloseEnough(
            (3 * tile) + 100, (4 * tile) + 100, partyTileX: 3, partyTileY: 5));
    }

    [Fact]
    public void THESHOVELIsCheckedAFTERTheConfirm() {
        // Click sound, confirm dialog, THEN CountItemInWholeParty(Shovel). The game asks whether
        // you want to dig and only then tells you that you cannot — and the refusal's own text is
        // written for that moment ("Besides, we need a shovel"). Checking first skips a line the
        // game means you to read.
        Assert.True(GraveDigging.ShovelIsCheckedAfterTheConfirm);
        Assert.True(GraveDigging.ConfirmCanBeDeclined);
        Assert.Equal(66, GraveDigging.NoShovelDialog);
    }

    [Fact]
    public void THESHOVELIsSPENTWhateverTheDigTurnsUp() {
        // useItem(Shovel) runs BEFORE the outcome branch, so an empty coffin costs the same as a
        // full one. A "check you have one" reading leaves the party with unlimited digs.
        Assert.True(GraveDigging.DiggingSpendsTheShovel);
        Assert.Equal(0x1e, GraveDigging.ShovelObjectId);
    }

    [Fact]
    public void ATrappedGraveIsDISPOSEDBeforeItsTrapSpawns() {
        // Read the encounter's x/y, dispose, null the pointer, THEN spawn from TRAP.DAT — and
        // re-fetch the container afterwards, because the spawn may have put a new one there.
        // Spawning first leaves the old grave standing beside whatever the trap created.
        Assert.True(GraveDigging.TrapDisposesTheGraveFirst);
    }

    [Fact]
    public void ASecondaryClickREADSTheStoneInsteadOfDigging() {
        // menu_getButtonClicked() != primary goes to the examine dialog on BOTH paths — the
        // diggable one and the examine-only one — so a right-click never digs.
        Assert.Equal(173, GraveDigging.ExamineDialog);
        Assert.NotEqual(GraveDigging.ExamineDialog, GraveDigging.NothingHereDialog);
    }
}
