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
            ZoneLocation = reader.ReadUInt16(),
            ZonePointer = reader.ReadUInt16(),
            Field04 = reader.ReadUInt32(),
            Field08 = reader.ReadUInt16(),
            Flags = reader.ReadUInt16(),
            Unknown0C = reader.ReadByte(),
            Unknown0D = reader.ReadByte(),
            Field0E = reader.ReadUInt32(),
            CameraZPosition = reader.ReadUInt32(),
            Field16 = reader.ReadUInt32(),
            Field1A = reader.ReadUInt32(),
            RmpResourceCount = reader.ReadUInt16(),
            Field20 = reader.ReadUInt16(),
            Field22 = reader.ReadUInt32(),
            Field26 = reader.ReadUInt32(),
            Field2A = reader.ReadUInt16(),
            Field2C = reader.ReadUInt32(),
            Field30 = reader.ReadUInt32()
        };
    }
}
