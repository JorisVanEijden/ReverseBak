namespace ResourceExtraction.Extractors;

using GameData.Resources.Credits;

using ResourceExtraction.Imaging;

using System.IO;
using System.Text;

// Parses CRED.DAT into engine-independent CreditsData. Mirrors LoadCRED.DAT
// (KRONDOR.EXE 0x40520):
//   uint16 count; uint16 offsets[count]; uint16 blobSize; byte text[blobSize]
// Entry [0] is the title; entries [1..] are consumed as (role, name) pairs the
// way scrollCredits (0x405f1) walks them (si += 2). The original's content-based
// layout/easter-egg predicates are resolved here so the renderer needs no
// byte-pattern knowledge.
public class CredExtractor : ExtractorBase<CreditsData> {
    public override CreditsData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.ASCII, leaveOpen: true);

        int count = reader.ReadUInt16();
        var offsets = new int[count];
        for (var i = 0; i < count; i++) {
            offsets[i] = reader.ReadUInt16();
        }
        int blobSize = reader.ReadUInt16();
        byte[] blob = reader.ReadBytes(blobSize);

        var strings = new string[count];
        for (var i = 0; i < count; i++) {
            strings[i] = ReadString(blob, offsets[i]);
        }

        var credits = new CreditsData(id) {
            Title = count > 0 ? strings[0] : string.Empty,
            Layout = new CreditsLayout(),
            Frame = AspectCorrection.CanonicalFrame()
        };

        // The closing block (left index > count-5) is centered rather than
        // columnar — scrollCredits left-aligns only while count-5 >= si.
        int centeredThreshold = count - 5;

        for (int i = 1; i < count; i += 2) {
            string role = strings[i];
            string name = i + 1 < count ? strings[i + 1] : string.Empty;
            credits.Lines.Add(new CreditLine {
                Role = role,
                Name = name,
                Centered = i > centeredThreshold,
                RareReveal = role.StartsWith("LO"),
                Sparkle = name.Length > 5 && name[0] == 'N' && name[5] == 'B'
            });
        }

        return credits;
    }

    // Decode a NUL-terminated string with Latin1 semantics (each byte → char).
    // The shipped credits are pure ASCII; this avoids depending on an encoding
    // provider being registered, which netstandard2.1 consumers may not do.
    private static string ReadString(byte[] blob, int offset) {
        int end = offset;
        while (end < blob.Length && blob[end] != 0) {
            end++;
        }
        var chars = new char[end - offset];
        for (var i = 0; i < chars.Length; i++) {
            chars[i] = (char)blob[offset + i];
        }
        return new string(chars);
    }
}
