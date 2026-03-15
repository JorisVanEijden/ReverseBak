namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
using System.IO;
using System.Text;

public class ZoneDefExtractor : ExtractorBase<ZoneDefinition>
{
    public override ZoneDefinition Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        return new ZoneDefinition(id)
        {
            ZoneLocation = reader.ReadInt16(),
            ZonePointer = reader.ReadInt16(),
            DefaultCameraZ = reader.ReadUInt32(),
            DefaultCameraPitch = reader.ReadUInt16(),
            Flags = reader.ReadUInt16(),
            SkyColor = reader.ReadByte(),
            GroundColor = reader.ReadByte(),
            MapMinZ = reader.ReadUInt32(),
            CameraZPosition = reader.ReadUInt32(),
            MapMaxZ = reader.ReadUInt32(),
            MapZoomStep = reader.ReadUInt32(),
            RmpResourceCount = reader.ReadInt16(),
            SpriteFogDivisor = reader.ReadInt16(),
            SpriteFogNearDistance = reader.ReadUInt32(),
            Unused26 = reader.ReadUInt32(),
            PolygonFogDivisor = reader.ReadInt16(),
            PolygonFogNearDistance = reader.ReadUInt32(),
            FarClipDistance = reader.ReadUInt32()
        };
    }
}
