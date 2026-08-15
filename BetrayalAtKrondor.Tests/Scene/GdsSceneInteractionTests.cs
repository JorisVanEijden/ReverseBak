namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

public class GdsSceneInteractionTests {
    [Fact]
    public void RightClickIsExamineAndLeftClickActs() {
        Assert.Equal(GdsSceneInteraction.Click.Examine, GdsSceneInteraction.ClickFor(128, true));
        Assert.Equal(GdsSceneInteraction.Click.Act, GdsSceneInteraction.ClickFor(128, false));
    }

    [Fact]
    public void AnIdBelowTheHotspotBaseIsNotAHotspotOnEitherButton() {
        Assert.Equal(GdsSceneInteraction.Click.NotAHotspot, GdsSceneInteraction.ClickFor(1, false));
        Assert.Equal(GdsSceneInteraction.Click.NotAHotspot, GdsSceneInteraction.ClickFor(1, true));
        Assert.Equal(-1, GdsSceneInteraction.HotspotIndexFor(1));
    }

    [Fact]
    public void TheActionIdMapsBackToTheHotspotIndex() {
        Assert.Equal(0, GdsSceneInteraction.HotspotIndexFor(128));
        Assert.Equal(10, GdsSceneInteraction.HotspotIndexFor(138));
    }

    [Fact]
    public void AHotspotWithNoExamineDialogIsSilentOnRightClick() {
        Assert.False(GdsSceneInteraction.HasExamine(new GdsHotspot { ExamineDialogId = 0 }));
        Assert.True(GdsSceneInteraction.HasExamine(new GdsHotspot { ExamineDialogId = 42 }));
    }

    [Fact]
    public void ABranchingOrTypeSixDialogOpensTheWindowAndPlainTextDoesNot() {
        Assert.Equal(GdsSceneInteraction.ExamineStyle.DialogWindow,
            GdsSceneInteraction.ExamineStyleFor(6, 0));
        Assert.Equal(GdsSceneInteraction.ExamineStyle.DialogWindow,
            GdsSceneInteraction.ExamineStyleFor(1, 3));
        Assert.Equal(GdsSceneInteraction.ExamineStyle.InScene,
            GdsSceneInteraction.ExamineStyleFor(1, 0));
    }

    [Fact]
    public void OnlyTheInSceneExamineInvalidatesThePalette() {
        Assert.True(GdsSceneInteraction.ExamineInvalidatesPalette(GdsSceneInteraction.ExamineStyle.InScene));
        Assert.False(GdsSceneInteraction.ExamineInvalidatesPalette(GdsSceneInteraction.ExamineStyle.DialogWindow));
    }

    [Fact]
    public void ActionThirteenSkipsTheHotspotsOwnDialog() {
        var shopServices = new GdsHotspot { ActionDialogId = 99, ActionCode = 13 };
        var anythingElse = new GdsHotspot { ActionDialogId = 99, ActionCode = 7 };

        Assert.False(GdsSceneInteraction.ShowsActionDialogFirst(shopServices));
        Assert.True(GdsSceneInteraction.ShowsActionDialogFirst(anythingElse));
    }

    [Fact]
    public void AHotspotWithNoActionDialogDispatchesStraightAway() {
        Assert.False(GdsSceneInteraction.ShowsActionDialogFirst(
            new GdsHotspot { ActionDialogId = 0, ActionCode = 7 }));
    }

    [Fact]
    public void TheActionCodeIsSigned() {
        Assert.Equal(7, GdsSceneInteraction.NormalizeActionCode(7));
        Assert.Equal(-1, GdsSceneInteraction.NormalizeActionCode(0xFF));
        Assert.Equal(127, GdsSceneInteraction.NormalizeActionCode(127));
        Assert.Equal(-128, GdsSceneInteraction.NormalizeActionCode(128));
    }

    [Fact]
    public void ADescriptionWithoutAnActionBlockIsAllText() {
        var (actions, text) = GdsSceneInteraction.SplitExamineText("To Hakha's Cajunlo");

        Assert.Null(actions);
        Assert.Equal("To Hakha's Cajunlo", text);
    }

    [Fact]
    public void ALeadingHashBlockIsScriptAndOnlyTheRestIsShown() {
        var (actions, text) = GdsSceneInteraction.SplitExamineText("#SETFLAG 12#You see a door.");

        Assert.Equal("SETFLAG 12", actions);
        Assert.Equal("You see a door.", text);
    }

    [Fact]
    public void AnUnterminatedBlockLeavesNothingToDisplay() {
        // The original's scan runs to the NUL, so everything was the block.
        var (actions, text) = GdsSceneInteraction.SplitExamineText("#SETFLAG 12");

        Assert.Equal("SETFLAG 12", actions);
        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void AHashLaterInTheTextIsNotAnActionBlock() {
        var (actions, text) = GdsSceneInteraction.SplitExamineText("Room #3 is locked.");

        Assert.Null(actions);
        Assert.Equal("Room #3 is locked.", text);
    }

    [Fact]
    public void MouseDriftIsSummedNotRadial() {
        // 6 + 4 = 10 still holds; one more dismisses.
        Assert.True(GdsSceneInteraction.ExamineSurvivesMouseMove(6, 4));
        Assert.False(GdsSceneInteraction.ExamineSurvivesMouseMove(7, 4));
        // Direction does not matter.
        Assert.True(GdsSceneInteraction.ExamineSurvivesMouseMove(-6, -4));
        Assert.False(GdsSceneInteraction.ExamineSurvivesMouseMove(-11, 0));
    }

    [Fact]
    public void TheDescriptionEndsOnInputOrOnMovement() {
        Assert.True(GdsSceneInteraction.ExamineEndsOn(dialogInput: true, 0, 0));
        Assert.True(GdsSceneInteraction.ExamineEndsOn(dialogInput: false, 20, 0));
        Assert.False(GdsSceneInteraction.ExamineEndsOn(dialogInput: false, 1, 1));
    }
}
