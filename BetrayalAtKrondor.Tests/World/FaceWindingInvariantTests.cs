namespace BetrayalAtKrondor.Tests.World;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using global::GameData.Resources.World;
using global::ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// Pins the invariant the future backface-culling work depends on: every polygon face's winding
/// agrees with the normal its <see cref="PolygonFace.NormalVertexIndex"/> points at.
/// </summary>
/// <remarks>
/// <b>Why this exists before the feature does.</b> Both world shaders run <c>Cull Off</c>, and both
/// used to justify it with "the TBL→mesh conversion doesn't guarantee consistent winding". Measured
/// 2026-09-06, that is false: the winding is consistent for every one of the ~12.8k faces that carry
/// a normal, with zero exceptions. Culling is therefore possible — see
/// docs/re-notes/2026-09-06-face-winding-is-consistent.md.
///
/// <para>The risk this guards is that the invariant is currently INVISIBLE: nothing reads it today,
/// so a converter change that reversed a winding, or a data path that dropped the normal, would go
/// unnoticed until someone tried to switch culling on and found faces missing. This test makes that
/// break loud at the point it happens.</para>
///
/// <para><b>Asserted as a property, not a count.</b> The measurement found exactly 12790 qualifying
/// faces, but pinning that number would fail the moment the extractor legitimately parsed more or
/// fewer. What matters is that NONE disagree, plus a floor proving the walk actually reached the
/// data — a scan that silently visits nothing otherwise "passes".</para>
///
/// <para>The uniform sign is negative because vertex order runs opposite the stored normal's
/// direction; that is a convention, not an inconsistency, and a fixed convention is exactly what
/// culling needs. Note the extractor's per-axis scaling (VertexScale, plus the ×1.2 world-up aspect)
/// is all POSITIVE, so it preserves orientation and cannot flip this sign.</para>
/// </remarks>
public class FaceWindingInvariantTests {
    static FaceWindingInvariantTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Faces below this and the walk clearly did not reach the shipped data.</summary>
    private const int MinimumFacesExpected = 10000;

    [Fact]
    public void EveryPolygonFaceWindingAgreesWithItsStoredNormal() {
        string? gameDir = FindOriginalGameDir();
        if (gameDir == null) {
            return; // shipped data absent (CI) — nothing to check
        }

        int checkedFaces = 0;
        var disagreements = new List<string>();

        foreach (string path in Directory.EnumerateFiles(gameDir, "*.TBL")) {
            using FileStream stream = File.OpenRead(path);
            ZoneTable table = new ZoneTableExtractor().Extract(Path.GetFileName(path), stream);

            foreach (ZoneTableEntry entry in table.Entries) {
                foreach (LodLevel lod in entry.Dat.Lods) {
                    foreach (MeshRecord mesh in lod.Meshes) {
                        if (mesh.VertexPoolIndex < 0 || mesh.VertexPoolIndex >= lod.VertexPools.Count) {
                            continue;
                        }
                        List<Position3DInt> pool = lod.VertexPools[mesh.VertexPoolIndex];

                        foreach (MeshFaceRecord faceRecord in mesh.MeshFaces) {
                            if (faceRecord is not PolygonMeshFace polygon) {
                                continue;
                            }
                            foreach (PolygonFace face in polygon.Faces) {
                                // < 3 vertices is a line (2) or nothing; neither has a facing.
                                // 255 is the explicit "no normal" sentinel.
                                if (face.VertexIndices.Count < 3
                                    || face.NormalVertexIndex == 255
                                    || face.NormalVertexIndex >= pool.Count) {
                                    continue;
                                }
                                if (AnyIndexOutOfRange(face.VertexIndices, pool.Count)) {
                                    continue;
                                }

                                checkedFaces++;
                                double dot = Dot(NewellNormal(face.VertexIndices, pool),
                                                 pool[face.NormalVertexIndex]);
                                if (dot >= 0 && disagreements.Count < 10) {
                                    disagreements.Add(
                                        $"{Path.GetFileName(path)} '{entry.Name}' "
                                        + $"face[{string.Join(",", face.VertexIndices)}] "
                                        + $"normal[{face.NormalVertexIndex}] dot={dot:0.###}");
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.True(checkedFaces >= MinimumFacesExpected,
            $"only {checkedFaces} faces were examined — the walk did not reach the shipped TBLs, "
            + "so a pass here would prove nothing");
        Assert.True(disagreements.Count == 0,
            "winding must run opposite the stored normal for every face, because backface culling "
            + "will depend on it. Disagreeing: " + string.Join("; ", disagreements));
    }

    private static bool AnyIndexOutOfRange(List<int> indices, int poolCount) {
        foreach (int i in indices) {
            if (i < 0 || i >= poolCount) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Newell's method — handles the non-planar faces the shipped data contains.</summary>
    private static (double X, double Y, double Z) NewellNormal(
        List<int> indices, List<Position3DInt> pool) {
        double x = 0, y = 0, z = 0;
        for (int i = 0; i < indices.Count; i++) {
            Position3DInt a = pool[indices[i]];
            Position3DInt b = pool[indices[(i + 1) % indices.Count]];
            x += (double)(a.Y - b.Y) * (a.Z + b.Z);
            y += (double)(a.Z - b.Z) * (a.X + b.X);
            z += (double)(a.X - b.X) * (a.Y + b.Y);
        }
        return (x, y, z);
    }

    private static double Dot((double X, double Y, double Z) v, Position3DInt p) =>
        (v.X * p.X) + (v.Y * p.Y) + (v.Z * p.Z);

    private static string? FindOriginalGameDir() {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame");
            if (File.Exists(Path.Combine(candidate, "Z01.TBL"))) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
