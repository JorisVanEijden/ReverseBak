namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// That every effect sprite's bitmap lands inside FIGS.BMX — the fact the arena's projectile
/// drawing rests on.
/// </summary>
/// <remarks>
/// <b>An effect entry's <c>BitmapIndex</c> is a GLOBAL index over the concatenated bitmap slots,
/// and in a fight slot 0 is FIGS.BMX rather than the zone's.</b> <c>combat_arena_mode_enter</c>
/// @0x5f2c0 opens an encounter with <c>fillBitmapSlot(0, figs.bmx)</c> beside
/// <c>SwapTblDataSlots(0, 1)</c>.
///
/// <para><b>Getting that wrong draws the wrong picture and nothing complains.</b> Resolved against
/// the zone's own slot 0 instead, Flamecast's bitmap 2 comes out as a pine tree — a real sprite, of
/// a real size, rendered without an error. That is exactly what the port did until 2026-09-02, and
/// no test or log would have caught it. What makes it catchable is the count: FIGS holds 16 images
/// and every effect entry indexes below that, so a chain built from the wrong first link is a
/// different arithmetic, not a missing file.</para>
///
/// <para><b>Skips rather than fails when <c>generated/</c> is absent</b>, the same contract the
/// other corpus tests use.</para>
/// </remarks>
public class EffectSpriteBitmapTests {
    /// <summary>The flown entries that are BILLBOARDS — three of the four.</summary>
    /// <remarks>
    /// <b><see cref="CombatEffectSprite.BaneOfBlackSlayers"/> is deliberately absent.</b> COMBAT.TBL
    /// entry 3, <c>jack</c>, carries six <c>PolygonMeshFace</c>es and no sprite face at all: that
    /// projectile is a MODEL, not a billboard, and the arena's sprite builder cannot draw it. This
    /// list is the set the billboard path claims, so putting it here would assert something false.
    /// </remarks>
    private static readonly int[] SpriteEffectEntries = {
        CombatEffectSprite.Flamecast,
        CombatEffectSprite.Shot,
        CombatEffectSprite.GenericSpell,
    };

    private static string? Generated() => GeneratedCorpus.FindDir("TBL", "BMX");

    private static int FigsImageCount(string root) {
        string dir = Path.Combine(root, "BMX", "FIGS");
        return Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png").Length : -1;
    }

    private static IEnumerable<int> SpriteBitmapIndices(string root, int entryIndex) {
        using JsonDocument doc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "TBL", "COMBAT.json")));
        JsonElement entry = doc.RootElement.GetProperty("Entries")[entryIndex];
        if (!entry.GetProperty("Dat").TryGetProperty("Lods", out JsonElement lods)
            || lods.ValueKind != JsonValueKind.Array || lods.GetArrayLength() == 0) {
            yield break;
        }
        foreach (JsonElement mesh in lods[0].GetProperty("Meshes").EnumerateArray()) {
            foreach (JsonElement face in mesh.GetProperty("MeshFaces").EnumerateArray()) {
                if (face.TryGetProperty("BitmapIndex", out JsonElement bi)
                    && bi.ValueKind == JsonValueKind.Number) {
                    yield return bi.GetInt32();
                }
            }
        }
    }

    [Fact]
    public void FigsHoldsSixteenImages() {
        string? root = Generated();
        if (root == null) {
            return;
        }
        Assert.Equal(16, FigsImageCount(root));
    }

    /// <summary>Every flown entry's bitmap is inside FIGS, so the chain never has to continue into
    /// the zone's slots for one.</summary>
    [Fact]
    public void EveryEffectSpriteIndexesInsideFigs() {
        string? root = Generated();
        if (root == null) {
            return;
        }
        int figs = FigsImageCount(root);
        Assert.True(figs > 0, "FIGS.BMX was not extracted.");

        foreach (int id in SpriteEffectEntries) {
            List<int> indices = SpriteBitmapIndices(root, id).ToList();
            Assert.True(indices.Count > 0, $"COMBAT.TBL entry {id} has no sprite face.");
            foreach (int index in indices) {
                Assert.InRange(index, 0, figs - 1);
            }
        }
    }

    /// <summary>Each billboard effect is a single fixed picture — no pose set, no octant.</summary>
    [Fact]
    public void EverySpriteEffectHasExactlyOneSpriteFace() {
        string? root = Generated();
        if (root == null) {
            return;
        }
        foreach (int id in SpriteEffectEntries) {
            Assert.Single(SpriteBitmapIndices(root, id));
        }
    }

    /// <summary>Bane of Black Slayers' projectile is a MODEL rather than a billboard.</summary>
    /// <remarks>
    /// Pinned because the two are drawn by different code: the arena falls back to a vertex-coloured
    /// mesh when an effect entry has no sprite face (TASK-289). If a future extractor change gave
    /// entry 3 a sprite face this test would fail, which is the right moment to notice that the
    /// fallback is no longer the path being taken.
    /// </remarks>
    [Fact]
    public void BaneOfBlackSlayersHasNoSpriteFace() {
        string? root = Generated();
        if (root == null) {
            return;
        }
        Assert.Empty(SpriteBitmapIndices(root, CombatEffectSprite.BaneOfBlackSlayers));
    }
}
