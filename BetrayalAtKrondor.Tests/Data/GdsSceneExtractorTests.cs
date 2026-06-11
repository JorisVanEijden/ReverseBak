namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Scene;

using ResourceExtraction.Extractors;

using System.IO;
using System.Linq;
using System.Text;

using Xunit;

/// <summary>
/// Verifies GDS&lt;n&gt;&lt;l&gt;.DAT parsing against the layout reverse-engineered from
/// ovr149 gds_loadSceneFile (KRONDOR.EXE 0x4d878):
///   s2 totalBodySize; 41-byte header (label[13], 6 unknown words, hotspotCount,
///   sceneDialogId, 2 stale far pointers); then hotspotCount × 36-byte records.
/// The file is a verbatim struct dump, so the resolved dialog/container pointers are
/// stale and must be skipped — the synthetic data writes sentinel pointers to prove it.
/// </summary>
public class GdsSceneExtractorTests {
    private static byte[] BuildGds(string label, short[] headerWords, int sceneDialogId, byte[][] hotspots) {
        // headerWords = [flag0D, 0F, song, 13, 15, 17, 19]
        var output = new MemoryStream();
        var writer = new BinaryWriter(output);

        var body = new MemoryStream();
        var bw = new BinaryWriter(body);
        byte[] labelBytes = Encoding.ASCII.GetBytes(label);
        var fixedLabel = new byte[13];
        System.Array.Copy(labelBytes, fixedLabel, System.Math.Min(labelBytes.Length, 13));
        bw.Write(fixedLabel);
        foreach (short w in headerWords) {
            bw.Write(w);
        }
        bw.Write((short)hotspots.Length);
        bw.Write(sceneDialogId);
        bw.Write(0x0BADF00D); // stale resolved-dialog pointer — must be ignored
        bw.Write(0x0BADBEEF); // stale resolved-container pointer — must be ignored
        foreach (byte[] h in hotspots) {
            Assert.Equal(36, h.Length);
            bw.Write(h);
        }
        byte[] bodyBytes = body.ToArray();

        writer.Write((short)bodyBytes.Length);
        writer.Write(bodyBytes);
        return output.ToArray();
    }

    private static byte[] Hotspot(short x, short y, short w, short h, short chapterHideMask, short cursor,
        byte actionCode, short nextSceneLetter, short animSceneId, int actionDialogId, int examineDialogId,
        short globalKey, short globalMin, short globalMax) {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        bw.Write(x); bw.Write(y); bw.Write(w); bw.Write(h);
        bw.Write(chapterHideMask); bw.Write(cursor);
        bw.Write(actionCode);
        bw.Write((byte)0x99);       // scratch byte — must be ignored
        bw.Write(nextSceneLetter);
        bw.Write(animSceneId);
        bw.Write(actionDialogId);
        bw.Write(examineDialogId);
        bw.Write(0x0BADC0DE);       // stale per-hotspot dialog pointer — must be ignored
        bw.Write(globalKey); bw.Write(globalMin); bw.Write(globalMax);
        return ms.ToArray();
    }

    private static GdsScene Extract(byte[] data) =>
        new GdsSceneExtractor().Extract("GDS1A.DAT", new MemoryStream(data));

    private static readonly byte[] Sample = BuildGds(
        "g_town",
        [1, 0, 1014, 25, 1, 0, 13],
        0x0016E36A,
        [
            Hotspot(135, 47, 58, 40, 0, 6, 4, 2, 0, 0, 0x0016E36B, 0, 1, 30000),
            Hotspot(107, 12, 64, 25, 0, 17, 2, 0, 0, 0x001D0CA7, 0x0016E370, 5, 0, 0),
        ]);

    [Fact]
    public void Extract_ReadsHeader() {
        GdsScene scene = Extract(Sample);
        Assert.Equal("g_town", scene.AnimationResource);
        Assert.Equal(1014, scene.Song);
        Assert.Equal(0x0016E36A, scene.SceneDialogId);
        Assert.Equal(1, scene.EntryFlagWord);
        Assert.Equal(25, scene.Unknown13);
        Assert.Equal(2, scene.Hotspots.Length);
    }

    [Fact]
    public void Extract_ReadsHotspotRectAndDialogs() {
        GdsHotspot h = Extract(Sample).Hotspots[0];
        // Rect (135,47,58,40) scaled to canonical 1600x1200 (x5 / x6).
        Assert.Equal(675, h.XPosition);
        Assert.Equal(282, h.YPosition);
        Assert.Equal(290, h.Width);
        Assert.Equal(240, h.Height);
        Assert.Equal(6, h.Cursor);
        Assert.Equal(4, h.ActionCode);
        Assert.Equal(2, h.NextSceneLetter);
        Assert.Equal(0, h.ActionDialogId);
        Assert.Equal(0x0016E36B, h.ExamineDialogId);
    }

    [Fact]
    public void Extract_LeftClickHotspotCarriesActionDialog() {
        GdsHotspot h = Extract(Sample).Hotspots[1];
        Assert.Equal(0x001D0CA7, h.ActionDialogId);
        Assert.Equal(17, h.Cursor);
        Assert.Equal(5, h.GlobalKey);
    }

    [Fact]
    public void Extract_EmptyLabelWhenNoAnimation() {
        byte[] data = BuildGds("", [0, 0, 0, 0, 0, 0, 0], 0,
            [Hotspot(0, 0, 320, 200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]);
        Assert.Equal("", Extract(data).AnimationResource);
    }

    // ───────────────── Real shipped data (skips when absent) ─────────────────

    private static string? FindOriginalGameDir() {
        string? dir = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame", "GDS1A.DAT");
            if (File.Exists(candidate)) {
                return Path.GetDirectoryName(candidate);
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    [Fact]
    public void Extract_RealGds1A_MatchesKnownContent() {
        string? gameDir = FindOriginalGameDir();
        if (gameDir == null) {
            return; // shipped data not present (e.g. CI) — synthetic tests cover the logic
        }

        using FileStream stream = File.OpenRead(Path.Combine(gameDir, "GDS1A.DAT"));
        GdsScene scene = new GdsSceneExtractor().Extract("GDS1A.DAT", stream);

        Assert.Equal("g_town", scene.AnimationResource);
        Assert.Equal(1014, scene.Song);
        Assert.Equal(0x0016E36A, scene.SceneDialogId);
        Assert.Equal(5, scene.Hotspots.Length);
        // The top banner is the only hotspot with a left-click action dialog ("enter town").
        Assert.Single(scene.Hotspots, h => h.ActionDialogId == 1900007);
    }

    [Fact]
    public void Extract_AllShippedFiles_SizeInvariantHolds() {
        string? gameDir = FindOriginalGameDir();
        if (gameDir == null) {
            return;
        }

        foreach (string path in Directory.GetFiles(gameDir, "GDS*.DAT")) {
            long size = new FileInfo(path).Length;
            using FileStream stream = File.OpenRead(path);
            GdsScene scene = new GdsSceneExtractor().Extract(Path.GetFileName(path), stream);
            // fileSize == 2 (size word) + 41 (header) + 36 * hotspotCount
            Assert.Equal(size, 2 + 41 + 36L * scene.Hotspots.Length);
        }
    }
}
