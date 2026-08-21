namespace BetrayalAtKrondor.Tests.Audio;

using GameData.Resources.Audio;
using Xunit;

/// <summary>
/// The world's ambient sound effects — when one comes, and which.
/// </summary>
public class AmbientSoundTests {
    [Fact]
    public void ADungeonIsQUIETERThanTheOpenAir_NotJustDifferent() {
        // 180 against 110. Sharing one rate is the obvious simplification and it loses the contrast
        // that makes a dungeon feel like one.
        Assert.True(AmbientSound.OneIn(underground: true) > AmbientSound.OneIn(underground: false));
        Assert.Equal(110, AmbientSound.OneIn(underground: false));
        Assert.Equal(180, AmbientSound.OneIn(underground: true));
    }

    [Fact]
    public void ATickSoundsONLYOnAZeroRoll() {
        // The die is rolled every pass and almost always does nothing; there is no timer and no
        // minimum gap. Treating it as "true most of the time" turns ambience into a drone.
        Assert.True(AmbientSound.Fires(0));
        Assert.False(AmbientSound.Fires(1));
        Assert.False(AmbientSound.Fires(109));
    }

    [Fact]
    public void CHAPTEREIGHTAndZONESIXAreSilentOutdoors() {
        Assert.True(AmbientSound.IsSilent(underground: false, chapter: 8, zone: 3));
        Assert.True(AmbientSound.IsSilent(underground: false, chapter: 2, zone: 6));
        Assert.False(AmbientSound.IsSilent(underground: false, chapter: 2, zone: 3));
    }

    [Fact]
    public void UNDERGROUNDIsExemptFromBothExclusions() {
        // *** The mode check comes FIRST. *** A dungeon in chapter 8, or under zone 6, still drips —
        // the exclusions live only in the outdoor branch. Hoisting them above the mode test is the
        // natural tidy-up and it silences dungeons that should not be silent.
        Assert.False(AmbientSound.IsSilent(underground: true, chapter: 8, zone: 6));
    }

    [Fact]
    public void BeforeTheStoryFlagTheWorldHasTwoSounds_OneOfThemRare() {
        Assert.Equal(AmbientSound.RarePreFlagSfx,
            AmbientSound.PickAboveground(zone: 3, moodFlagSet: false, percentRoll: 0, pairRoll: 0, rangeRoll: 52));
        Assert.Equal(AmbientSound.RarePreFlagSfx,
            AmbientSound.PickAboveground(zone: 3, moodFlagSet: false, percentRoll: 5, pairRoll: 0, rangeRoll: 52));
        Assert.Equal(AmbientSound.CommonPreFlagSfx,
            AmbientSound.PickAboveground(zone: 3, moodFlagSet: false, percentRoll: 6, pairRoll: 0, rangeRoll: 52));
    }

    [Fact]
    public void ThePercentageComparisonIsINCLUSIVE() {
        // Six outcomes in a hundred, not five — the same inclusive form the hotspot and scouting
        // rolls use. Worth pinning because "<= 5" reads as 5%.
        var rare = 0;
        for (var roll = 0; roll < 100; roll++) {
            if (AmbientSound.PickAboveground(3, false, roll, 0, 52) == AmbientSound.RarePreFlagSfx) {
                rare++;
            }
        }
        Assert.Equal(6, rare);
    }

    [Fact]
    public void AFTERTheFlagTheWorldSoundsDifferent() {
        // *** The ambient palette is story-dependent, and the change is invisible in the audio data
        // because it lives in a flag test. *** A port that hardcodes one set has the world sounding
        // the same all game.
        int before = AmbientSound.PickAboveground(3, moodFlagSet: false, percentRoll: 50, pairRoll: 0, rangeRoll: 53);
        int after = AmbientSound.PickAboveground(3, moodFlagSet: true, percentRoll: 50, pairRoll: 0, rangeRoll: 53);

        Assert.NotEqual(before, after);
        Assert.Equal(53, after);
    }

    [Fact]
    public void TheDistinctZoneGetsItsOwnMixOnlyAfterTheFlag() {
        // Zone 2 is ordinary until the flag, and only then splits off.
        Assert.Equal(
            AmbientSound.PickAboveground(3, moodFlagSet: false, percentRoll: 50, pairRoll: 0, rangeRoll: 53),
            AmbientSound.PickAboveground(AmbientSound.DistinctZone, moodFlagSet: false, percentRoll: 50, pairRoll: 0, rangeRoll: 53));

        Assert.Equal(AmbientSound.DistinctZoneSfx,
            AmbientSound.PickAboveground(AmbientSound.DistinctZone, true, percentRoll: 50, pairRoll: 0, rangeRoll: 53));
    }

    [Fact]
    public void TheDistinctZonesOtherHalfIsATwoSoundPair() {
        Assert.Equal(AmbientSound.DistinctZonePairBase,
            AmbientSound.PickAboveground(AmbientSound.DistinctZone, true, percentRoll: 51, pairRoll: 0, rangeRoll: 53));
        Assert.Equal(AmbientSound.DistinctZonePairBase + 1,
            AmbientSound.PickAboveground(AmbientSound.DistinctZone, true, percentRoll: 99, pairRoll: 1, rangeRoll: 53));
    }

    [Fact]
    public void EveryPostFlagSoundIsInsideTheDeclaredRange() {
        for (int roll = AmbientSound.PostFlagFirstSfx; roll <= AmbientSound.PostFlagLastSfx; roll++) {
            int picked = AmbientSound.PickAboveground(3, true, percentRoll: 50, pairRoll: 0, rangeRoll: roll);
            Assert.InRange(picked, AmbientSound.PostFlagFirstSfx, AmbientSound.PostFlagLastSfx);
        }
    }

    [Fact]
    public void ADungeonPlaysOneSoundAndItIsNotAnOutdoorOne() {
        Assert.Equal(3, AmbientSound.UndergroundSfx);
        Assert.NotEqual(AmbientSound.CommonPreFlagSfx, AmbientSound.UndergroundSfx);
    }

    [Fact]
    public void IntensityReachesHigherOutdoorsThanUnderground() {
        (int outMin, int outMax) = AmbientSound.IntensityRange(underground: false);
        (int inMin, int inMax) = AmbientSound.IntensityRange(underground: true);

        Assert.Equal(outMin, inMin);
        Assert.True(outMax > inMax);
        Assert.Equal(63, outMax);
        Assert.Equal(59, inMax);
    }
}
