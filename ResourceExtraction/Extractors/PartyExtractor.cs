namespace ResourceExtraction.Extractors;

using GameData.Resources.Config;
using GameData.Resources.Data;
using ResourceExtraction.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Parses PARTY.DAT — the six initial playable-character templates loaded into
/// TEMP.GAM at new-game. See <see cref="PartyData"/> for the layout and
/// <see cref="ActorRecordReader"/> for the shared 95-byte actor record (the same
/// record used inside save files).
///
/// Structure: 6 × 95-byte actor records, a u16 trailer (the file size), then the
/// six display names as sequential NUL-terminated strings, in record order.
/// </summary>
public class PartyExtractor : ExtractorBase<PartyData> {
    public override PartyData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var data = new PartyData(id);

        var actors = new SaveGameActorData[PartyData.MemberCount];
        for (int i = 0; i < PartyData.MemberCount; i++) {
            actors[i] = ActorRecordReader.ParseActor(reader);
        }

        reader.ReadUInt16(); // trailer == file size; not needed once records are parsed

        // Names are NOT positional: each record's NamePointer indexes the trailing
        // name block (offset relative to the block start). Build offset -> name, then
        // resolve. (e.g. record 1's pointer is 14 -> "Gorath", not the 2nd string "Owyn".)
        long nameBlockStart = reader.BaseStream.Position;
        var nameByOffset = new Dictionary<int, string>();
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            int offset = (int)(reader.BaseStream.Position - nameBlockStart);
            nameByOffset[offset] = reader.ReadZeroTerminatedString();
        }

        foreach (SaveGameActorData actor in actors) {
            string name = nameByOffset.TryGetValue(actor.NamePointer, out string? resolved) ? resolved : string.Empty;
            data.Members.Add(new PartyMember(name, actor));
        }

        return data;
    }
}
