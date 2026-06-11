namespace ResourceExtraction.Extractors;

using GameData.Resources.Scene;
using ResourceExtraction.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Parses a GDS&lt;number&gt;&lt;letter&gt;.DAT interactive-scene file.
///
/// Layout (reversed from ovr149 <c>gds_loadSceneFile</c> @0x4d878, verified against all 118 shipped
/// files — every size equals 2 + 41 + 36 * hotspotCount):
///   s2  totalBodySize
///   --- body (41-byte header) ---
///   char[13] label          (animation resource basename, null-padded; empty = no animation)
///   i16  entryFlagWord       (@0x0D; 0x80 set -> SetGlobalValue((v&0x7F)+6480, 1) on entry)
///   i16  nextSceneLetter     (@0x0F; default sub-scene letter / exit)
///   i16  song
///   i16  unknown13           (@0x13; meaning undetermined; sole use is a <25 threshold picking a near-constant, inaudible music volume)
///   i16  entryAnimationTag   (@0x15)
///   i16  transitionAnimTag   (@0x17; always 0 in shipped data)
///   i16  idleAnimationTag    (@0x19)
///   i16  hotspotCount
///   i32  sceneDialogId
///   i32  (resolved dialog pointer  - stale, ignored)
///   i32  (resolved container pointer - stale, ignored)
///   --- hotspotCount x 36-byte records ---
///   see <see cref="ReadHotspot"/>.
/// </summary>
public class GdsSceneExtractor : ExtractorBase<GdsScene> {
    private const int LabelLength = 13;

    public override GdsScene Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var scene = new GdsScene(id);

        reader.ReadInt16(); // total body size; redundant with the stream length, not retained

        scene.AnimationResource = ReadFixedString(reader, LabelLength);
        scene.EntryFlagWord = reader.ReadInt16();
        scene.NextSceneLetter = reader.ReadInt16();
        scene.Song = reader.ReadInt16();
        scene.Unknown13 = reader.ReadInt16();
        scene.EntryAnimationTag = reader.ReadInt16();
        scene.TransitionAnimationTag = reader.ReadInt16();
        scene.IdleAnimationTag = reader.ReadInt16();
        int hotspotCount = reader.ReadInt16();
        scene.SceneDialogId = reader.ReadInt32();
        reader.ReadInt32(); // header resolved-dialog pointer  (runtime scratch, stale in file)
        reader.ReadInt32(); // header resolved-container pointer (runtime scratch, stale in file)

        var hotspots = new List<GdsHotspot>(hotspotCount);
        for (int i = 0; i < hotspotCount; i++) {
            hotspots.Add(ReadHotspot(reader));
        }
        scene.Hotspots = hotspots.ToArray();

        return scene;
    }

    private static GdsHotspot ReadHotspot(BinaryReader reader) {
        short x = reader.ReadInt16();
        short y = reader.ReadInt16();
        short w = reader.ReadInt16();
        short h = reader.ReadInt16();
        // Raw mode-13h pixels -> canonical 1600x1200 (x5 / x6), same space as REQ/Label/TTM/DDX layout.
        var hotspot = new GdsHotspot {
            XPosition = AspectCorrection.ScaleVgaX(x),
            YPosition = AspectCorrection.ScaleVgaY(y),
            Width = AspectCorrection.ScaleVgaX(w),
            Height = AspectCorrection.ScaleVgaY(h),
            ChapterHideMask = reader.ReadInt16(),
            Cursor = reader.ReadInt16(),
            ActionCode = reader.ReadByte(),
        };
        reader.ReadByte(); // runtime scratch (visit counter, zeroed at load), stale in file
        hotspot.NextSceneLetter = reader.ReadInt16();
        hotspot.AnimationSceneId = reader.ReadInt16();
        hotspot.ActionDialogId = reader.ReadInt32();
        hotspot.ExamineDialogId = reader.ReadInt32();
        reader.ReadInt32(); // hotspot resolved-dialog pointer (runtime scratch, stale in file)
        hotspot.GlobalKey = reader.ReadInt16();
        hotspot.GlobalMin = reader.ReadInt16();
        hotspot.GlobalMax = reader.ReadInt16();
        return hotspot;
    }

    private static string ReadFixedString(BinaryReader reader, int length) {
        byte[] raw = reader.ReadBytes(length);
        int end = 0;
        while (end < raw.Length && raw[end] != 0) {
            end++;
        }
        // Labels are DOS 8.3 resource basenames (e.g. "g_town"), always ASCII.
        return Encoding.ASCII.GetString(raw, 0, end);
    }
}
