namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// What a location scene actually puts on screen — settled from the entry sequence, because the
/// obvious reading (a background image per scene) is wrong.
/// </summary>
public class GdsSceneRenderingTests {
    [Fact]
    public void ThePictureIsTheAnimationNotABackground() {
        Assert.True(GdsSceneRules.PictureComesFromTheAnimation);
    }

    [Fact]
    public void TheOnlySceneLoopScxIsTheDialogueFrame() {
        // Reading it as the backdrop would put the dialogue border behind every town gate.
        Assert.Equal("Dialog.scr", GdsSceneRules.DialogueFrameResource);
    }

    [Fact]
    public void AndItIsLoadedOncePerRunRatherThanPerSubScene() {
        Assert.True(GdsSceneRules.DialogueFrameLoadsOncePerRun);
    }

    [Fact]
    public void HotspotLabelsAreBakedPicturesNotText() {
        Assert.Equal("POINTERG.BMX", GdsSceneRules.CursorSetResource);
    }
}
