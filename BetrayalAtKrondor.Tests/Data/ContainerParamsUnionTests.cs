namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using Xunit;

/// <summary>
/// <c>SUBREC_PARAMS</c> is a three-way union (ACTOR.H:200), and our model names only one arm.
/// </summary>
public class ContainerParamsUnionTests {
    [Fact]
    public void TheProximityReadingIsTheSameBytesUnderDifferentNames() {
        // bFlags / bIntensity / bHundred_flag / pad — byte for byte with the lock arm.
        var subrecord = new SaveGameContainerLockData(
            flags: 0x04, difficulty: 7, puzzleChest: 1, trapDamage: 0);

        Assert.Equal(7, subrecord.ProximityIntensity);
        Assert.Equal(subrecord.Difficulty, subrecord.ProximityIntensity);
        Assert.True(subrecord.ProximityFlagBit2);
        Assert.True(subrecord.ProximityHundredFlag);
    }

    [Fact]
    public void OnlyBitTwoOfTheFlagsByteDrivesTheDivisor() {
        // The /0x32 divisor is one bit, not "flags are non-zero" — an actor with other flags set
        // must not pick it up.
        Assert.False(new SaveGameContainerLockData(0x03, 0, 0, 0).ProximityFlagBit2);
        Assert.True(new SaveGameContainerLockData(0x05, 0, 0, 0).ProximityFlagBit2);
    }

    [Fact]
    public void TheHundredFlagIsAnyNonZeroByte() {
        Assert.False(new SaveGameContainerLockData(0, 0, 0, 0).ProximityHundredFlag);
        Assert.True(new SaveGameContainerLockData(0, 0, 2, 0).ProximityHundredFlag);
    }

    [Fact]
    public void TheProtectedBitIsTheONEFlagServingBOTHProtections() {
        // 0x40 exempts an actor from the stash sweep AND sorts its bag last for recycling. The two
        // models must agree on the bit or one of the protections quietly stops applying.
        Assert.Equal((int)SaveGameContainerDataType.HoldsProtectedItem,
            GameData.Resources.World.StashExposure.ProtectedFlag);
    }

    [Fact]
    public void AnEventStateSubrecordIsWHATTheShopFlagMARKS() {
        // SUBREC_EVENT_STATE is flag 0x04, which our enum calls Shop. The stash sweep's
        // "isEventState" test is the presence of THAT subrecord, so a caller looking for a
        // separate event-state field would find none and conclude the data is unparsed.
        Assert.Equal(0x04, (int)SaveGameContainerDataType.Shop);
    }
}
