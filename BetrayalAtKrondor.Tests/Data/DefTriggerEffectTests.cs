namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.GameState;

using ResourceExtraction.Extractors.GameState;

using Xunit;

public class DefTriggerEffectTests {
    [Fact]
    public void EnabSetsFlag() {
        var e = Assert.IsType<SetFlagEffect>(DefEffect.ForKey(7042, set: true));
        Assert.Equal(7042, e.Flag);
        Assert.True(e.Set);
    }

    [Fact]
    public void DisaClearsFlag() {
        var e = Assert.IsType<SetFlagEffect>(DefEffect.ForKey(7042, set: false));
        Assert.False(e.Set);
    }

    [Fact]
    public void ZeroKeyIsNoEffect() {
        Assert.Null(DefEffect.ForKey(0, set: true));
    }
}
