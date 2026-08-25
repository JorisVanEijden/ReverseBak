namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Picking one of a DEF_COMB record's four landings.
/// </summary>
public class DefCombLandingTests {
    private static DefCombEntry Record() => new DefCombEntry {
        LandingDir1 = new LandingPosition { FineX = 1, FineY = 11, RotationZ = 100 },
        LandingDir2 = new LandingPosition { FineX = 2, FineY = 22, RotationZ = 200 },
        LandingDir4 = new LandingPosition { FineX = 4, FineY = 44, RotationZ = 400 },
        LandingDir8 = new LandingPosition { FineX = 8, FineY = 88, RotationZ = 800 },
    };

    [Fact]
    public void EachDirectionReachesItsOwnEntry() {
        DefCombEntry r = Record();

        Assert.Equal(1, r.LandingFor(EncounterAftermath.Landing.Direction1).FineX);
        Assert.Equal(2, r.LandingFor(EncounterAftermath.Landing.Direction2).FineX);
        Assert.Equal(4, r.LandingFor(EncounterAftermath.Landing.Direction4).FineX);
        Assert.Equal(8, r.LandingFor(EncounterAftermath.Landing.Direction8).FineX);
    }

    [Fact]
    public void TheOutcodeAnswersFeedItStraightThrough() {
        // The chain a fleeing party actually runs: a direction code out of the outcode, through
        // LandingFor, into the record. Asserted end to end because each link alone can be right
        // while the pair is wired to the wrong entry.
        DefCombEntry r = Record();

        Assert.Equal(2, r.LandingFor(EncounterAftermath.LandingFor(2)).FineX);
        Assert.Equal(4, r.LandingFor(EncounterAftermath.LandingFor(4)).FineX);
        Assert.Equal(8, r.LandingFor(EncounterAftermath.LandingFor(8)).FineX);

        // 1 and the five directions that share its entry.
        foreach (int direction in new[] { 1, 3, 5, 6, 7 }) {
            Assert.Equal(1, r.LandingFor(EncounterAftermath.LandingFor(direction)).FineX);
        }
    }

    [Fact]
    public void ARecordThatDefinesNoLandingsStillAnswersRatherThanReturningNull() {
        // Every field is constructed, so a caller never has to null-check a landing — a record with
        // no landings drops the party at its own tile's origin, which is the original's behaviour
        // for the same all-zero bytes.
        var bare = new DefCombEntry();

        Assert.NotNull(bare.LandingFor(EncounterAftermath.Landing.Direction8));
        Assert.Equal(0, bare.LandingFor(EncounterAftermath.Landing.Direction8).FineX);
    }
}
