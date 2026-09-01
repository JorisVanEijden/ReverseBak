namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Whether the arena sweep can find floor underground — the premise the underground half of
/// TASK-282 rests on.
/// </summary>
/// <remarks>
/// <b>The original does not sweep underground; it renders the scene and reads pixels.</b>
/// <c>arena_buildGridByRenderProbe</c> @0x2e671 floods the buffer with a sentinel, redraws the world
/// over it, and calls a cell open if its projected point still shows the far-floor colour. There is
/// no framebuffer here, so the port answers the same question from geometry instead — the proximity
/// scan that already confines the party to the corridors, classified by
/// <see cref="CombatGroundCheck.OpenGroundKinds"/>.
///
/// <para><b>That substitution is only sound while underground floors classify as open ground</b>,
/// and nothing else in either tree checks it. If an extractor change moved the unnamed kind 14, or
/// a zone paved its corridors with something outside the set, every underground encounter would
/// silently stop firing — the sweep would report zero open cells and
/// <c>EnoughGroundToFight</c> would refuse. That failure is invisible: no exception, no log, just
/// fights that never happen in the mines.</para>
///
/// <para><b>Skips rather than fails when <c>generated/</c> is absent</b>, the same contract the
/// other corpus tests use.</para>
/// </remarks>
public class UndergroundArenaGroundTests {
    /// <summary>Zones whose <c>Z##DEF</c> carries <c>ZoneLocation == 2</c>.</summary>
    private static readonly int[] UndergroundZones = { 10, 11, 12 };

    private static string? TblDir() {
        string? root = GeneratedCorpus.FindDir("TBL");
        return root == null ? null : Path.Combine(root, "TBL");
    }

    /// <summary>Every entry's entity kind, as the extractor writes it — a name or a bare number.</summary>
    private static IReadOnlyList<(string Name, int? Kind)> KindsOf(string tblPath) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(tblPath));
        var kinds = new List<(string, int?)>();
        foreach (JsonElement entry in doc.RootElement.GetProperty("Entries").EnumerateArray()) {
            if (!entry.TryGetProperty("Dat", out JsonElement dat)
                || !dat.TryGetProperty("EntityType", out JsonElement type)) {
                continue;
            }
            string name = entry.TryGetProperty("Name", out JsonElement n) ? n.GetString() ?? "" : "";
            kinds.Add((name, type.ValueKind == JsonValueKind.Number ? type.GetInt32()
                : NumberFor(type.GetString())));
        }
        return kinds;
    }

    /// <summary>
    /// The extractor writes a NAME for a kind the enum covers and a NUMBER for one it does not, so a
    /// test reading either has to map back.
    /// </summary>
    private static int? NumberFor(string? name) =>
        name != null && System.Enum.TryParse(name, out GameData.Resources.World.WorldEntityType t)
            ? (int)t
            : null;

    [Fact]
    public void EveryUndergroundZonePavesItsFloorsWithOpenGroundKinds() {
        // *** THE SWEEP HAS TO FIND SOMETHING. *** CombatGroundCheck's set treats an empty position
        // as a block, which is what keeps an arena off a void — and underground that is most of the
        // volume, because unmodelled rock has no polygon at all. So the corridors' own floor models
        // are the only thing that can make an underground cell open, and they have to be in the set.
        string? dir = TblDir();
        if (dir == null) {
            return;   // generated/ not present — skip, do not fail
        }

        foreach (int zone in UndergroundZones) {
            string path = Path.Combine(dir, $"Z{zone:D2}.json");
            Assert.True(File.Exists(path), $"Z{zone:D2}.json is missing from the corpus");

            IReadOnlyList<(string Name, int? Kind)> kinds = KindsOf(path);
            int open = kinds.Count(k => k.Kind != null && CombatGroundCheck.IsOpenGround(k.Kind.Value));

            // A tenth is a floor, not a target: zone 10 is about four fifths open-ground entries.
            // The number that matters is that it is not ZERO, which is the regression this catches.
            Assert.True(open > kinds.Count / 10,
                $"zone {zone}: only {open} of {kinds.Count} entries are open ground — "
                + "the arena sweep would refuse every fight down there");
        }
    }

    [Fact]
    public void TheUnnamedKind14IsWhatUndergroundActuallyPavesWith() {
        // Kind 14 has no name in the enum and is carried in OpenGroundKinds as a bare literal, which
        // makes it the easiest member of that set to "clean up" by mistake. It is also the one doing
        // the work: it is the majority of what a mine tile is built from. Pinning it here means a
        // deletion fails a test that says why it exists rather than only breaking play underground.
        string? dir = TblDir();
        if (dir == null) {
            return;
        }

        IReadOnlyList<(string Name, int? Kind)> kinds = KindsOf(Path.Combine(dir, "Z10.json"));
        Assert.Contains(14, CombatGroundCheck.OpenGroundKinds);
        Assert.True(kinds.Count(k => k.Kind == 14) > 0,
            "zone 10 has no kind-14 entries — the assumption this set is built on has moved");
    }

    [Fact]
    public void APitUndergroundIsStillNotSomethingYouCanFightOn() {
        // The one kind you may walk onto and not fight on, and zone 10 has one. Above ground that
        // distinction keeps an arena off open pits; the sweep now runs underground too, so it has to
        // hold there as well.
        string? dir = TblDir();
        if (dir == null) {
            return;
        }

        Assert.False(CombatGroundCheck.IsOpenGround(CombatGroundCheck.WalkableOnlyKind));
        Assert.DoesNotContain(CombatGroundCheck.WalkableOnlyKind, CombatGroundCheck.OpenGroundKinds);
    }
}
