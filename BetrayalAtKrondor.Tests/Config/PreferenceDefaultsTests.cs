namespace BetrayalAtKrondor.Tests.Config;

using System.IO;
using GameData.Resources.Config;
using ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// The built-in <see cref="Preferences"/> initializers are a copy of the shipped DEFAULT.DAT, and
/// this asserts the copy against the original instead of trusting a comment.
/// </summary>
/// <remarks>
/// <b>A comment in PreferencesService claimed they matched and one of them did not.</b>
/// <c>StepSize</c> initialized to Medium where byte 0 of the file is 2 = Large, so every consumer
/// that fell back to the initializers walked one preset too fast — MOVEMENT.DAT's step distance
/// AND, because its third array is indexed by the same preference, the game seconds elapsed per
/// step. Measured against the original on the same save: 200 units and 120 s per keypress against
/// the original's 100 and 60.
/// </remarks>
public class PreferenceDefaultsTests {
    /// <summary>The five bytes of the shipped DEFAULT.DAT (CDEFAULT.DAT differs only in flags).</summary>
    private static readonly byte[] ShippedDefaultDat = { 0x02, 0x01, 0x02, 0x00, 0x0F };

    private static Preferences FromBytes(byte[] blob) =>
        new PreferencesExtractor().Extract("DEFAULT.DAT", new MemoryStream(blob));

    [Fact]
    public void BuiltInDefaultsMatchShippedDefaultDat() {
        Preferences shipped = FromBytes(ShippedDefaultDat);
        var builtIn = new Preferences("DEFAULT");

        Assert.Equal(shipped.StepSize, builtIn.StepSize);
        Assert.Equal(shipped.TurnSize, builtIn.TurnSize);
        Assert.Equal(shipped.DetailLevel, builtIn.DetailLevel);
        Assert.Equal(shipped.TextSpeed, builtIn.TextSpeed);
        Assert.Equal(shipped.Sound, builtIn.Sound);
        Assert.Equal(shipped.GameMusic, builtIn.GameMusic);
        Assert.Equal(shipped.CombatMusic, builtIn.CombatMusic);
        Assert.Equal(shipped.Introduction, builtIn.Introduction);
        Assert.Equal(shipped.CdMusic, builtIn.CdMusic);
    }

    [Fact]
    public void TheStepPresetPicksBothTheDistanceAndTheClockRate() {
        // MOVEMENT.DAT: StepDistances[3] then TurnAngles[3] then SecondsPerStep[3], and
        // WORLDMOV.C indexes the FIRST and THIRD with step_speed. So one wrong preset moves the
        // party further per press *and* burns game time faster — the reason a wrong default shows
        // up as attrition (exhaustion, rations) rather than as a movement bug.
        var table = new MovementData("MOVEMENT.DAT") {
            StepDistances = new[] { 400, 800, 1600 },
            TurnAngles = new[] { 1024, 2048, 4096 },
            SecondsPerStep = new[] { 60, 120, 240 }
        };

        Assert.Equal(400, table.StepDistanceFor(StepSize.Small));
        Assert.Equal(60, table.SecondsPerStepFor(StepSize.Small));
        Assert.Equal(800, table.StepDistanceFor(StepSize.Medium));
        Assert.Equal(120, table.SecondsPerStepFor(StepSize.Medium));

        // The turn array is indexed by the OTHER preference, so it does not move with StepSize.
        Assert.Equal(2048, table.TurnAngleFor(TurnSize.Medium));
    }
}
