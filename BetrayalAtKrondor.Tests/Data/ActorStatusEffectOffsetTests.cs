namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;

using ResourceExtraction;
using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Where a character's seven condition ranks actually live — TASK-203.
/// </summary>
/// <remarks>
/// <b>Asserted against the SHIPPED save and against the struct, never against our own round
/// trip.</b> The reader and the writer shared a single offset constant, so a round trip was
/// self-consistent and passed for as long as the offset was wrong. That is the whole reason this
/// defect survived: it could only be seen from outside our own pair of functions.
/// </remarks>
public class ActorStatusEffectOffsetTests {
    static ActorStatusEffectOffsetTests() =>
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    [Fact]
    public void TheRanksBlockStartsWhereTheStructSaysAndFillsExactlySixRows() {
        // 0x2cc, immediately after aSkillTrainRate's twelve bytes at 0x2c0 — and it ends exactly on
        // aActorStatModifiers, so there is no room for a "trailing unused" run.
        Assert.Equal(0x2cc, SaveGameOffsets.ActorStatusEffects);
        Assert.Equal(0x2f6,
            SaveGameOffsets.ActorStatusEffects
            + SaveGameOffsets.PartyActorCount * SaveGameOffsets.ActorStatusEffectsStride);
    }

    [Fact]
    public void TheOneAfflictionInTheSHIPPEDSaveBelongsToCharacterZero() {
        // *** The assertion the round trip could not make. *** SAVE02 carries exactly one non-zero
        // condition byte. Read seven bytes early it lands on character 1; read correctly it is
        // character 0's. Only the shipped bytes can tell those apart.
        byte[]? save = ReadGameFile(Path.Combine("GAMES", "dir.G01", "SAVE02.GAM"));
        if (save == null) {
            return;   // skip-if-absent, like the other game-data tests
        }

        using var stream = new MemoryStream(save);
        SaveGame parsed = new SaveGameExtractor().Extract("SAVE02.GAM", stream);
        SaveGameActorStatusEffectsData[] effects =
            parsed.Data.StateData.PartyConfigurationData.ActorStatusEffects;

        Assert.Equal(SaveGameOffsets.PartyActorCount, effects.Length);
        Assert.Equal(97, effects[0].Healing);
        for (var c = 1; c < effects.Length; c++) {
            Assert.Equal(0, effects[c].Sick);
            Assert.Equal(0, effects[c].Plagued);
            Assert.Equal(0, effects[c].Poisoned);
            Assert.Equal(0, effects[c].Drunk);
            Assert.Equal(0, effects[c].Healing);
            Assert.Equal(0, effects[c].Starving);
            Assert.Equal(0, effects[c].NearDeath);
        }
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
