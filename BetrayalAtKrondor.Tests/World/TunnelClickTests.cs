namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The tunnel click, which is <b>not</b> the ladder click.
/// </summary>
/// <remarks>
/// These pin the two things that were wrong: which handler kinds 20 and 39 belong to, and that a
/// tunnel's traversal is a hotspot dispatch rather than its message. The consequence of getting
/// either wrong is that the Mac Mordain Cadal has no exit — measured, not hypothesised.
/// </remarks>
public class TunnelClickTests {
    [Fact]
    public void AHotspotBeatsAMessageWhenTheObjectCarriesBoth() {
        // The original consults the message only to decide whether there is anything to do at all,
        // then enters the dispatch block regardless. An object with both would otherwise speak its
        // line and stay put — which is exactly the "nothing happens" the mine's stairs gave.
        Assert.Equal(TunnelClick.Outcome.DispatchHotspot,
            TunnelClick.For(isPrimary: true, inReach: true, hasFixedObject: true, hasHotspot: true,
                interactDialogId: 2300004));
    }

    [Fact]
    public void WithNoHotspotTheMessageIsTheFallback_AndWithNeitherItIsNothingToDo() {
        Assert.Equal(TunnelClick.Outcome.PlayMessage,
            TunnelClick.For(isPrimary: true, inReach: true, hasFixedObject: true, hasHotspot: false,
                interactDialogId: 2300004));
        Assert.Equal(TunnelClick.Outcome.NothingToDo,
            TunnelClick.For(isPrimary: true, inReach: true, hasFixedObject: true, hasHotspot: false,
                interactDialogId: 0));
    }

    [Fact]
    public void OutOfReachBeatsEverything_IncludingTheDescribeLine() {
        // The reach test is the first statement in the routine, before the click sound. A secondary
        // click from outside the tile says nothing either.
        Assert.Equal(TunnelClick.Outcome.OutOfReach,
            TunnelClick.For(isPrimary: true, inReach: false, hasFixedObject: true, hasHotspot: true,
                interactDialogId: 0));
        Assert.Equal(TunnelClick.Outcome.OutOfReach,
            TunnelClick.For(isPrimary: false, inReach: false, hasFixedObject: true,
                hasHotspot: true, interactDialogId: 0));
    }

    [Fact]
    public void ReachIsTheMapTile_NotARadius() {
        Assert.True(TunnelClick.IsWithinReach(11, 10, 11, 10));
        Assert.False(TunnelClick.IsWithinReach(12, 10, 11, 10));
        Assert.False(TunnelClick.IsWithinReach(11, 11, 11, 10));
    }

    [Fact]
    public void TheThreeFixedObjectHandlersDoNotShareADescribeLine() {
        // A building answers 0x60, a ladder 0xae and a tunnel 0x62. Sharing one record between two
        // of them makes one of the two say the wrong thing, which is invisible until someone reads
        // it on screen.
        Assert.NotEqual(TraversalClick.DescribeDialog, TunnelClick.DescribeDialog);
        Assert.Equal(0x62, TunnelClick.DescribeDialog);
    }

    [Fact]
    public void TheLadderRefusesWhenItHasNoLockSubrecord() {
        // Distinct from LockFlowAlwaysRuns, which is about the VALUE. A missing SUBREC_PARAMS is
        // "nothing happens"; a present one holding zero runs the whole flow.
        Assert.False(TraversalClick.HasLockToRun(hasLockSubrecord: false));
        Assert.True(TraversalClick.HasLockToRun(hasLockSubrecord: true));
        Assert.True(TraversalClick.LockFlowAlwaysRuns);
    }
}
