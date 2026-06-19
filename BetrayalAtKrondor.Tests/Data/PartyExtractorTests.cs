namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;

using ResourceExtraction.Extractors;

using System.IO;
using System.Text;

using Xunit;

/// <summary>
/// Verifies PARTY.DAT parsing: six (here, synthetic) 95-byte actor records, a u16
/// file-size trailer, then a NUL-terminated name block. Names are resolved by each
/// record's name_pointer (offset into the block), NOT positionally — see
/// <see cref="PartyData"/> / docs/FileFormats/PARTY.DAT.md.
/// </summary>
public class PartyExtractorTests {

    static PartyExtractorTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Writes one 95-byte actor record: name_pointer, 3 spell words, 16
    /// attributes (maximum only, rest 0), actor_number, inventory_pointer, combat_pointer.</summary>
    private static void WriteActor(BinaryWriter w, ushort namePointer, byte[] attributeMaxima) {
        w.Write(namePointer);
        w.Write((short)0); w.Write((short)0); w.Write((short)0); // known spells
        for (int i = 0; i < 16; i++) {
            w.Write(attributeMaxima[i]); // maximum
            w.Write((byte)0);            // current
            w.Write((byte)0);            // currentEffective
            w.Write((byte)0);            // experience
            w.Write((byte)0);            // modifier
        }
        w.Write((byte)0);   // actorNumber
        w.Write((uint)0);   // inventoryPointer
        w.Write((ushort)0); // combatDataPointer
    }

    // record 0 -> name_pointer 6 ("Alpha"); record 1 -> name_pointer 0 ("Bravo").
    // Positional resolution would (wrongly) give record0="Bravo".
    private static byte[] BuildPartyDat() {
        var attr0 = new byte[16]; attr0[0] = 55; attr0[7] = 63;  // Health=55, AccuracyCasting=63
        var attr1 = new byte[16]; attr1[0] = 40; attr1[13] = 62; // Health=40, Lockpick=62

        var ms = new MemoryStream();
        var w = new BinaryWriter(ms, Encoding.ASCII);
        WriteActor(w, namePointer: 6, attributeMaxima: attr0); // record 0 -> "Alpha"
        WriteActor(w, namePointer: 0, attributeMaxima: attr1); // record 1 -> "Bravo"
        for (int i = 2; i < PartyData.MemberCount; i++) {       // PARTY.DAT always has 6 records
            WriteActor(w, namePointer: 0, attributeMaxima: new byte[16]);
        }
        w.Write((ushort)0);                       // file-size trailer (value irrelevant to parse)
        w.Write(Encoding.ASCII.GetBytes("Bravo")); w.Write((byte)0); // offset 0
        w.Write(Encoding.ASCII.GetBytes("Alpha")); w.Write((byte)0); // offset 6
        return ms.ToArray();
    }

    private static PartyData Extract() =>
        new PartyExtractor().Extract("PARTY.DAT", new MemoryStream(BuildPartyDat()));

    [Fact]
    public void Extract_ResolvesNamesByPointerNotPosition() {
        PartyData party = Extract();
        Assert.Equal(PartyData.MemberCount, party.Members.Count);
        Assert.Equal("Alpha", party.Members[0].Name); // name_pointer 6, not the 1st string
        Assert.Equal("Bravo", party.Members[1].Name); // name_pointer 0, not the 2nd string
    }

    [Fact]
    public void Extract_DecodesAttributesInActorAttributeOrder() {
        PartyData party = Extract();
        Assert.Equal(55, party.Members[0].Actor.Health.Maximum);
        Assert.Equal(63, party.Members[0].Actor.AccuracyCasting.Maximum);
        Assert.Equal(62, party.Members[1].Actor.Lockpick.Maximum);
        // maximum populated, the other four attribute bytes are zero at game start
        Assert.Equal(0, party.Members[0].Actor.Health.Current);
    }
}
