namespace ResourceExtraction.Extractors;

using GameData.Resources.Location;
using System.IO;

/// <summary>
/// <c>TELEPORT.DAT</c> — forty 11-byte destination records.
///
/// <para>Lives here rather than in the extractor CLI because the game needs it <b>at runtime</b>:
/// a dialog <c>Teleport</c> action names a destination by id, so the table has to be loadable
/// through the resource system, not merely dumped to JSON at build time.</para>
///
/// <para>Each record is a <c>PlayerSpawnRecord</c> in the original — zone, tile x/y, sub-tile x/y
/// and a camera heading — followed by the GDS scene to enter on arrival, if any.</para>
/// </summary>
public class TeleportExtractor : ExtractorBase<TeleportDestinationSet> {
    public override TeleportDestinationSet Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var set = new TeleportDestinationSet(id);
        var index = 0;
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            var location = new Location {
                ZoneNumber = reader.ReadByte(),
                X = reader.ReadByte(),
                Y = reader.ReadByte(),
                XOffset = reader.ReadByte(),
                YOffset = reader.ReadByte(),
                ZRotation = reader.ReadUInt16(),
            };
            set.Destinations.Add(new TeleportDestination {
                Id = index++,
                Location = location,
                GdsNumber = reader.ReadInt16(),
                GdsLetter = reader.ReadInt16(),
            });
        }
        return set;
    }
}
