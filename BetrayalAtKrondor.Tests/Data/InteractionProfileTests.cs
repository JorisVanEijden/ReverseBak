namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using System.Collections.Generic;
using Xunit;

public class InteractionProfileTests {
    [Fact]
    public void DefaultsAreSafe() {
        var p = new InteractionProfile {
            ActionableContainerTypes = new[] { SaveGameContainerType.Corpse },
        };
        Assert.Null(p.Range);              // no proximity gate by default
        Assert.False(p.OpensLoot);
        Assert.False(p.HasLock);
        Assert.Single(p.ActionableContainerTypes);
    }

    [Fact]
    public void RangeCarriesBothThresholds() {
        var p = new InteractionProfile {
            ActionableContainerTypes = System.Array.Empty<SaveGameContainerType>(),
            Range = new InteractionRange(7000, 2500),
        };
        Assert.Equal(7000, p.Range!.Value.Overground);
        Assert.Equal(2500, p.Range!.Value.Underground);
    }
}
