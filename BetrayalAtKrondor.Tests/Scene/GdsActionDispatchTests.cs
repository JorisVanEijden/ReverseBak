namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

public class GdsActionDispatchTests {
    [Theory]
    [InlineData(2, GdsActionDispatch.ActionKind.DialogOnly)]
    [InlineData(3, GdsActionDispatch.ActionKind.SubScene)]
    [InlineData(4, GdsActionDispatch.ActionKind.SubScene)]
    [InlineData(5, GdsActionDispatch.ActionKind.Container)]
    [InlineData(6, GdsActionDispatch.ActionKind.Container)]
    [InlineData(7, GdsActionDispatch.ActionKind.Inn)]
    [InlineData(8, GdsActionDispatch.ActionKind.Container)]
    [InlineData(9, GdsActionDispatch.ActionKind.Barding)]
    [InlineData(11, GdsActionDispatch.ActionKind.Teleport)]
    [InlineData(13, GdsActionDispatch.ActionKind.ShopServices)]
    [InlineData(15, GdsActionDispatch.ActionKind.EndChapter)]
    [InlineData(16, GdsActionDispatch.ActionKind.ShopScreen)]
    public void EveryCodeTheShippedScenesUseIsHandled(int code, GdsActionDispatch.ActionKind expected) {
        Assert.Equal(expected, GdsActionDispatch.KindOf(code));
    }

    [Fact]
    public void CodeTenIsUnhandledBecauseTheOriginalDiscardsItsTest() {
        Assert.Equal(GdsActionDispatch.ActionKind.Unhandled, GdsActionDispatch.KindOf(10));
        Assert.True(GdsActionDispatch.ActionCode10IsDead);
    }

    [Fact]
    public void FailedBardingBecomesASubSceneTransition() {
        Assert.Equal(9, GdsActionDispatch.ActionAfterBarding(bardingSucceeded: true));
        Assert.Equal(3, GdsActionDispatch.ActionAfterBarding(bardingSucceeded: false));
        // And 3 is a transition, so failure moves the party rather than doing nothing.
        Assert.Equal(GdsActionDispatch.ActionKind.SubScene,
            GdsActionDispatch.KindOf(GdsActionDispatch.ActionAfterBarding(false)));
    }

    [Fact]
    public void TheTwoTransitionCodesReadTheLetterFromDifferentPlaces() {
        Assert.Equal(4, GdsActionDispatch.TransitionLetter(3, sceneNextLetter: 4, hotspotNextLetter: 9));
        Assert.Equal(9, GdsActionDispatch.TransitionLetter(4, sceneNextLetter: 4, hotspotNextLetter: 9));
    }

    [Fact]
    public void ATransitionWithNoDestinationLeavesTheLocation() {
        Assert.True(GdsActionDispatch.TransitionLeavesTheLocation(0));
        Assert.True(GdsActionDispatch.TransitionLeavesTheLocation(-1));
        Assert.False(GdsActionDispatch.TransitionLeavesTheLocation(1));
    }

    [Fact]
    public void TheVisitCountSaturatesRatherThanWrapping() {
        Assert.Equal(1, GdsActionDispatch.NextVisitCount(0));
        Assert.Equal(100, GdsActionDispatch.NextVisitCount(99));
        Assert.Equal(100, GdsActionDispatch.NextVisitCount(100));
        Assert.Equal(200, GdsActionDispatch.NextVisitCount(200));
    }

    [Fact]
    public void OnlyOneInnHasAScriptedRate() {
        Assert.Null(GdsActionDispatch.ScriptedInnRate(62, 4, flagSet: false));
        Assert.Null(GdsActionDispatch.ScriptedInnRate(61, 5, flagSet: false));
        Assert.Equal(72, GdsActionDispatch.ScriptedInnRate(62, 5, flagSet: false));
        Assert.Equal(10, GdsActionDispatch.ScriptedInnRate(62, 5, flagSet: true));
    }

    [Fact]
    public void TheShopServiceMenuLoopsUntilItReturnsThree() {
        Assert.True(GdsActionDispatch.ShopServicesContinues(1));
        Assert.True(GdsActionDispatch.ShopServicesContinues(2));
        Assert.False(GdsActionDispatch.ShopServicesContinues(3));
    }

    [Fact]
    public void OnlyAFullScreenActionFadesBeforeTheLocationComesBack() {
        Assert.True(GdsActionDispatch.RedrawsTheLocationAfterwards(staysInTheScene: true));
        Assert.False(GdsActionDispatch.RedrawsTheLocationAfterwards(staysInTheScene: false));
        Assert.True(GdsActionDispatch.FadesBeforeRedraw(showedAFullScreen: true));
        Assert.False(GdsActionDispatch.FadesBeforeRedraw(showedAFullScreen: false));
    }
}
