namespace ResourceExtraction.Extractors;

using GameData.Resources.Config;
using ResourceExtraction.Imaging;
using System.IO;

/// <summary>
/// Parses START.DAT — ten little-endian int16 with no header, count or terminator, so the read
/// ORDER below is the whole format. It mirrors <c>LoadSTART.DAT</c> (ovr129 @0x41620) call for call.
/// See <see cref="StartData"/> for what each value means and who consumes it.
///
/// <para>Signed, not unsigned: two of the ten are negative (the combat camera's pitches), and
/// reading them as u16 turns -2112 into 63424 — a value that would look like a plausible angle and
/// tilt the camera the wrong way.</para>
/// </summary>
public class StartDataExtractor : ExtractorBase<StartData> {
    public override StartData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);

        return new StartData(id) {
            CombatCameraHeightAboveGround = reader.ReadInt16(),
            CombatCameraHeightUnderground = reader.ReadInt16(),
            CombatCameraPitchAboveGround = reader.ReadInt16(),
            CombatCameraPitchUnderground = reader.ReadInt16(),
            CombatGridCellSize = reader.ReadInt16(),
            // Screen coordinates, so they cross into canonical space here and the original's
            // 320x200 stops at this boundary.
            ViewportX = AspectCorrection.ScaleVgaX(reader.ReadInt16()),
            ViewportY = AspectCorrection.ScaleVgaY(reader.ReadInt16()),
            ViewportWidth = AspectCorrection.ScaleVgaX(reader.ReadInt16()),
            ViewportHeight = AspectCorrection.ScaleVgaY(reader.ReadInt16()),
            ProjectionShift = reader.ReadInt16(),
        };
    }
}
