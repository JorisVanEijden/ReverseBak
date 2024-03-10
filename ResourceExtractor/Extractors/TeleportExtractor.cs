namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources.Location;

internal class TeleportExtractor : ExtractorBase {
    public static List<TeleportDestination> Extract(string filePath) {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(fileStream);
        var destinations = new List<TeleportDestination>();
        int id = 0;
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            Location location = new() {
                ZoneNumber = reader.ReadByte(),
                X = reader.ReadByte(),
                Y = reader.ReadByte(),
                XOffset = reader.ReadByte(),
                YOffset = reader.ReadByte(),
                ZRotation = reader.ReadUInt16()
            };
            TeleportDestination destination = new() {
                Id = id++,
                Location = location,
                GdsNumber = reader.ReadInt16(),
                GdsLetter = reader.ReadInt16()
            };
            destinations.Add(destination);
        }
        return destinations;
    }
}