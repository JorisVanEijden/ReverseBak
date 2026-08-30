namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// What the SHIPPED pits actually look like, measured over the committed <c>generated/</c> corpus.
/// </summary>
/// <remarks>
/// <b>The rope crossing is gated on facts about the data, and until now none of them had been
/// checked against the data.</b> <see cref="PitRopeCrossing.AxisOf"/> answers
/// <see cref="PitRopeCrossing.PitAxis.None"/> for a pit at any angle but the four axis-aligned
/// ones, and the handler falls silently through for those — so "how many shipped pits can be
/// crossed at all" is a question the model raises and cannot answer by itself.
///
/// <para>Skip-if-absent, like the other corpus tests: no game data, no assertion.</para>
/// </remarks>
public class PitPlacementTests {
    // Every Pit-typed zone-table entry ships under one name, and both pit code paths key off it:
    // the polygon you walk onto and the object you click are the same entity.
    private const string PitEntryName = "m_pit";

    /// <summary>
    /// Whether a zone-table entry's Dat is a pit.
    /// </summary>
    /// <remarks>
    /// <b>The enum ships as its NAME, not its number.</b> The committed JSON writes
    /// <c>"EntityType": "Pit"</c>, so a <c>GetInt32</c> comparison throws rather than answering
    /// false — which is how this was written first. Both forms are accepted so a serializer setting
    /// cannot quietly turn this test into one that finds nothing.
    /// </remarks>
    private static bool IsPit(JsonElement dat) {
        if (!dat.TryGetProperty("EntityType", out JsonElement type)) {
            return false;
        }

        return type.ValueKind == JsonValueKind.Number
            ? type.GetInt32() == (int)WorldEntityType.Pit
            : type.GetString() == nameof(WorldEntityType.Pit);
    }

    private static IReadOnlyList<int> ShippedPitRotations(string gen) {
        // The Pit-typed entries, by their stable content key, across every zone table.
        var pitKeys = new HashSet<string>();
        foreach (string tblPath in Directory.GetFiles(Path.Combine(gen, "TBL"), "Z*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(tblPath));
            foreach (JsonElement entry in doc.RootElement.GetProperty("Entries").EnumerateArray()) {
                if (entry.TryGetProperty("Dat", out JsonElement dat) && IsPit(dat)) {
                    pitKeys.Add(entry.GetProperty("Key").GetString()!);
                }
            }
        }

        var rotations = new List<int>();
        foreach (string wldPath in Directory.GetFiles(Path.Combine(gen, "WLD"), "T*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(wldPath));
            foreach (JsonElement item in doc.RootElement.GetProperty("Items").EnumerateArray()) {
                if (item.TryGetProperty("EntityKey", out JsonElement key)
                    && key.GetString() is string k && pitKeys.Contains(k)) {
                    rotations.Add(item.GetProperty("Rotation").GetProperty("Z").GetInt32());
                }
            }
        }
        return rotations;
    }

    [Fact]
    public void EverySHIPPEDPitIsAxisAlignedAndThereforeCrossable() {
        // *** THE UNCROSSABLE CASE IS UNREACHABLE IN THE SHIPPED DATA. *** All 24 placements carry
        // one of the four axis rotations, so AxisOf never answers None for a real pit and no player
        // meets a chasm the rope cannot span. The None arm stays — it is what the routine does, and
        // a mod placing a diagonal pit would find it — but it is dead against the original's data,
        // which is worth knowing before treating a silent click as a bug.
        string? gen = GeneratedCorpus.FindDir("WLD", "TBL");
        if (gen == null) {
            return;
        }

        IReadOnlyList<int> rotations = ShippedPitRotations(gen);
        Assert.NotEmpty(rotations);

        foreach (int rotationZ in rotations) {
            Assert.NotEqual(PitRopeCrossing.PitAxis.None, PitRopeCrossing.AxisOf(rotationZ));
        }
    }

    [Fact]
    public void AllFourFacingsAreUsed_SoNeitherAxisArmIsDeadCode() {
        // Both arms of AxisOf are exercised by the shipped data — 0 and 0x8000 lie along X, 0x4000
        // and 0xC000 along Y. A test that only proved "not None" would pass with every pit on one
        // axis, and the along/across coordinate swap in the handler would then be untested by the
        // corpus in one of its two directions.
        string? gen = GeneratedCorpus.FindDir("WLD", "TBL");
        if (gen == null) {
            return;
        }

        var seen = new HashSet<int>(ShippedPitRotations(gen));
        Assert.Contains(PitRopeCrossing.RotationEast, seen);
        Assert.Contains(PitRopeCrossing.RotationWest, seen);
        Assert.Contains(PitRopeCrossing.RotationNorth, seen);
        Assert.Contains(PitRopeCrossing.RotationSouth, seen);
    }

    [Fact]
    public void TheOneEntryNameCoversBothPitPaths() {
        // Documented as an assertion because it is the fact that settles a recurring confusion: the
        // walk-onto polygon and the clickable object are not two entity types. They are one, named
        // m_pit, and the click dispatch and the movement loop both reach it.
        string? gen = GeneratedCorpus.FindDir("TBL");
        if (gen == null) {
            return;
        }

        var names = new HashSet<string>();
        foreach (string tblPath in Directory.GetFiles(Path.Combine(gen, "TBL"), "Z*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(tblPath));
            foreach (JsonElement entry in doc.RootElement.GetProperty("Entries").EnumerateArray()) {
                if (entry.TryGetProperty("Dat", out JsonElement dat) && IsPit(dat)) {
                    names.Add(entry.GetProperty("Name").GetString()!);
                }
            }
        }

        Assert.Equal(new HashSet<string> { PitEntryName }, names);
    }
}
