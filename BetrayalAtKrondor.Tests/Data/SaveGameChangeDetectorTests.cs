namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;
using GameData.Resources.Data;

using ResourceExtraction;
using ResourceExtraction.Extractors;

using System.IO;
using System.Linq;

using Xunit;

/// <summary>
/// <c>lastSeenStepSpeed</c> / <c>lastSeenGridStride</c> — the pair our model called StepSize and
/// TurnSize, which named the quantities right and their ROLE wrong.
/// </summary>
public class SaveGameChangeDetectorTests {
    // The extractor reads CP437 names out of the header; without the provider it throws.
    static SaveGameChangeDetectorTests() =>
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    [Fact]
    public void TheyHoldResolvedMovementScalars_NotPreferenceIndices() {
        byte[]? save = ReadGameFile(Path.Combine("GAMES", "dir.G01", "SAVE02.GAM"));
        byte[]? movement = ReadGameFile("MOVEMENT.DAT");
        if (save == null || movement == null) {
            return; // skip-if-absent, like the other game-data tests
        }

        // Strip the 100-byte slot header so the extractor sees a bare body.
        var body = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(save, SaveGameOffsets.HeaderSize, body, 0, body.Length);
        SaveGame parsed = new SaveGameExtractor().Extract("SAVE02", new MemoryStream(body));
        SaveGameStateData state = parsed.Data!.StateData;

        MovementData table = new MovementExtractor().Extract("MOVEMENT.DAT", new MemoryStream(movement));

        // The discriminating part: these are the RESOLVED scalars, so each value must be a member of
        // the corresponding MOVEMENT.DAT table. A preference index (0..2) would not be, and neither
        // would a value read from the wrong offset — which is what makes this a fence on the layout
        // as well as on the meaning.
        Assert.Contains(state.LastSeenStepSpeed, table.StepDistances);
        Assert.Contains(state.LastSeenGridStride, table.TurnAngles);

        // Shipped values, stated so a silent shift is visible: Large step, Medium turn.
        Assert.Equal(1600, state.LastSeenStepSpeed);
        Assert.Equal(2048, state.LastSeenGridStride);
        Assert.Equal(2, System.Array.IndexOf(table.StepDistances, (int)state.LastSeenStepSpeed));
        Assert.Equal(1, System.Array.IndexOf(table.TurnAngles, (int)state.LastSeenGridStride));
    }

    [Fact]
    public void ANewGameHasNotLookedYet() {
        byte[]? startup = ReadGameFile("STARTUP.GAM");
        if (startup == null) {
            return;
        }

        var body = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(startup, SaveGameOffsets.HeaderSize, body, 0, body.Length);
        SaveGameStateData state = new SaveGameExtractor().Extract("STARTUP", new MemoryStream(body))
            .Data!.StateData;

        // Zero, not a default preset — the pair means "what these were when last observed", and a
        // fresh game has never observed them. That asymmetry against SAVE02 is the clearest single
        // piece of evidence that they are a change detector rather than a setting.
        Assert.Equal(0, state.LastSeenStepSpeed);
        Assert.Equal(0, state.LastSeenGridStride);
    }

    private static byte[]? ReadGameFile(string name) {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }
        return null;
    }
}
