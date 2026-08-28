namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// When a step-size change triggers the roaming-encounter reset —
/// <c>worldmove_rgn_chap_trans_apply</c> (WORLDMOV.C:65).
/// </summary>
public class StepSizeChangeTests {
    [Fact]
    public void ANINCREASEResetsTheRoamers() =>
        Assert.True(StepSizeChange.ResetsRoamers(1600, 2400));

    [Fact]
    public void ADECREASEDoesNot() =>
        Assert.False(StepSizeChange.ResetsRoamers(2400, 1600));

    [Fact]
    public void NOCHANGEDoesNot() =>
        Assert.False(StepSizeChange.ResetsRoamers(1600, 1600));

    [Fact]
    public void THEBASELINEMovesEvenWhenNothingFired() {
        // *** THE ASYMMETRY, AND THE ONLY REASON NewBaseline IS A FUNCTION. *** The original assigns
        // nLastSeenStepSpeed OUTSIDE the increase check. Storing it only when the sweep ran would
        // leave the baseline high after a decrease, and the next increase back to a previously-seen
        // value would then be judged "not an increase" and silently skip the reset.
        Assert.Equal(1600, StepSizeChange.NewBaseline(2400, 1600));

        // Lower, then back up: the second move IS an increase against the re-baselined value.
        int baseline = StepSizeChange.NewBaseline(2400, 1600);
        Assert.True(StepSizeChange.ResetsRoamers(baseline, 2400));
    }

    [Fact]
    public void THEGRIDSTRIDEArmFollowsTheSameRule() {
        // Same shape, different action (worldmove_plr_hdg_align_grid). Named separately so a reader
        // is not left deducing that the two arms agree.
        Assert.True(StepSizeChange.AlignsHeading(1024, 2048));
        Assert.False(StepSizeChange.AlignsHeading(2048, 1024));
    }
}
