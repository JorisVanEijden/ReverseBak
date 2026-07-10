namespace BetrayalAtKrondor.Tests.Object;
using GameData.Resources.Object;
using Xunit;

public class ItemIconResolverTests {
    [Fact] public void UsesIconWhenNonZero_Invshp1() =>
        Assert.Equal("INVSHP1.BMX#5", ItemIconResolver.ResolveBmxSubResource(new ObjectInfo("x"){ Number=80, Icon=5 }));

    [Fact] public void FallsBackToNumberWhenIconZero() =>
        Assert.Equal("INVSHP1.BMX#80", ItemIconResolver.ResolveBmxSubResource(new ObjectInfo("x"){ Number=80, Icon=0 }));

    [Fact] public void Invshp2WhenIndexGte120() =>
        Assert.Equal("INVSHP2.BMX#5", ItemIconResolver.ResolveBmxSubResource(new ObjectInfo("x"){ Number=125, Icon=0 }));
}
