namespace ResourceExtraction.Extractors;

using GameData.Resources.Menu;
using ResourceExtraction.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Parses an IN_*.DAT "input form": a u16 string-pool length + pool, a u16 field count, then
/// that many 21-byte field records. See <see cref="InputForm"/> for the layout and the IDA
/// references (Load_in_file @ ovr140:0x469f0).
///
/// Coordinates (box X/Y/Width and caption X/Y) are mode-13h pixels on disk; they are scaled
/// into the canonical 1600×1200 space (×5 / ×6), like the other UI coordinate formats.
/// </summary>
public class InExtractor : ExtractorBase<InputForm> {
    private const int StyleByteCount = 5;

    public override InputForm Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var form = new InputForm(id);

        int poolLen = reader.ReadUInt16();
        byte[] pool = reader.ReadBytes(poolLen);

        int fieldCount = reader.ReadUInt16();
        for (int i = 0; i < fieldCount; i++) {
            var field = new InputField {
                ItemCount = reader.ReadUInt16(),
                X = AspectCorrection.ScaleVgaX(reader.ReadUInt16()),
                Y = AspectCorrection.ScaleVgaY(reader.ReadUInt16()),
                Width = AspectCorrection.ScaleVgaX(reader.ReadUInt16()),
            };
            for (int s = 0; s < StyleByteCount; s++) {
                field.StyleBytes.Add(reader.ReadByte());
            }
            ushort labelOffset = reader.ReadUInt16();
            field.Label = labelOffset == 0xFFFF ? "" : ReadPoolString(pool, labelOffset);
            field.LabelX = AspectCorrection.ScaleVgaX(reader.ReadUInt16());
            field.LabelY = AspectCorrection.ScaleVgaY(reader.ReadUInt16());
            field.AllocateFlag = reader.ReadUInt16();

            form.Fields.Add(field);
        }

        return form;
    }

    private static string ReadPoolString(byte[] pool, int offset) {
        if (offset >= pool.Length) {
            return "";
        }
        int end = System.Array.IndexOf(pool, (byte)0, offset);
        int length = (end >= 0 ? end : pool.Length) - offset;
        return Encoding.GetEncoding(DosCodePage).GetString(pool, offset, length);
    }
}
