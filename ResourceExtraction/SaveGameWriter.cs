namespace ResourceExtraction;

using System;
using System.Collections.Generic;
using System.Text;
using GameData.Resources.Data;

/// <summary>Result of writing a save: the full SAVE##.GAM bytes + which body bytes we authored.</summary>
public readonly record struct SaveGameWriteResult(byte[] Bytes, SaveCoverage Coverage);

/// <summary>
/// Writes a byte-interchangeable SAVE##.GAM (100-byte header + patched TEMP.GAM body). Preserve-and-
/// patch: copies the backing body and overwrites only the fields we model, so unmodeled regions stay
/// byte-identical to what a real engine wrote. Symmetric to <see cref="Extractors.SaveGameExtractor"/>.
/// </summary>
public static class SaveGameWriter {
    private const int DosCodePage = 437;

    public static SaveGameWriteResult Write(
        byte[] backingBody, in SaveGameFields fields,
        string name, short headerWorldX, short headerWorldY, short mapIcon,
        IReadOnlyList<DirtyContainerEdit> containerEdits = null) {
        if (backingBody is null) {
            throw new ArgumentNullException(nameof(backingBody));
        }
        if (backingBody.Length != SaveGameOffsets.BodySize) {
            throw new ArgumentException(
                $"Backing body must be {SaveGameOffsets.BodySize} bytes, was {backingBody.Length}.",
                nameof(backingBody));
        }

        // Patch a copy of the backing body; record every authored range.
        byte[] body = (byte[])backingBody.Clone();
        var coverage = new ByteCoverage();

        void PatchI16(int offset, short value) {
            BitConverter.GetBytes(value).CopyTo(body, offset);
            coverage.Add(offset, sizeof(short));
        }
        void PatchI32(int offset, int value) {
            BitConverter.GetBytes(value).CopyTo(body, offset);
            coverage.Add(offset, sizeof(int));
        }
        void PatchU8(int offset, byte value) {
            body[offset] = value;
            coverage.Add(offset, 1);
        }

        PatchI16(SaveGameOffsets.Chapter, fields.Chapter);
        PatchI32(SaveGameOffsets.PartyGold, fields.PartyGold);
        PatchI32(SaveGameOffsets.GameTime, fields.GameTime);
        PatchU8(SaveGameOffsets.CurrentZone, fields.CurrentZone);
        PatchU8(SaveGameOffsets.WorldX, fields.WorldX);
        PatchU8(SaveGameOffsets.WorldY, fields.WorldY);
        PatchI32(SaveGameOffsets.PositionX, fields.PositionX);
        PatchI32(SaveGameOffsets.PositionY, fields.PositionY);
        PatchI32(SaveGameOffsets.PositionZ, fields.PositionZ);
        PatchI16(SaveGameOffsets.Rotation, fields.Rotation);

        if (containerEdits != null) {
            foreach (DirtyContainerEdit edit in containerEdits) {
                PatchU8(edit.BodyOffset + ContainerGeometry.NumberOfItemsOffset, edit.NumberOfItems);
                int arrayOff = edit.BodyOffset + ContainerGeometry.ItemArrayOffset;
                for (int i = 0; i < edit.LiveItemBytes.Length; i++) {
                    PatchU8(arrayOff + i, edit.LiveItemBytes[i]);
                }
            }
        }

        // 100-byte header, then the patched body.
        byte[] output = new byte[SaveGameOffsets.HeaderSize + SaveGameOffsets.BodySize];
        WriteFixedLengthString(output, SaveGameOffsets.HeaderName, SaveGameOffsets.HeaderNameLength, name);
        BitConverter.GetBytes(fields.Chapter).CopyTo(output, SaveGameOffsets.HeaderChapter);
        BitConverter.GetBytes(headerWorldX).CopyTo(output, SaveGameOffsets.HeaderWorldX);
        BitConverter.GetBytes(headerWorldY).CopyTo(output, SaveGameOffsets.HeaderWorldY);
        BitConverter.GetBytes(mapIcon).CopyTo(output, SaveGameOffsets.HeaderMapIcon);
        BitConverter.GetBytes(SaveGame.SupportedVersion).CopyTo(output, SaveGameOffsets.HeaderVersion);
        Buffer.BlockCopy(body, 0, output, SaveGameOffsets.HeaderSize, body.Length);

        var cov = new SaveCoverage(SaveGameOffsets.BodySize, coverage.AuthoredBytes, coverage.Ranges);
        return new SaveGameWriteResult(output, cov);
    }

    // NUL-padded fixed-length CP437 field (mirror of the reader's ReadFixedLengthString).
    private static void WriteFixedLengthString(byte[] dest, int offset, int length, string value) {
        byte[] encoded = Encoding.GetEncoding(DosCodePage).GetBytes(value ?? string.Empty);
        int n = Math.Min(encoded.Length, length);
        Array.Copy(encoded, 0, dest, offset, n);
        // remaining bytes stay 0 (NUL) — dest is fresh.
    }
}
