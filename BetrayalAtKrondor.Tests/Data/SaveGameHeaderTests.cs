namespace BetrayalAtKrondor.Tests.Data;

using System;
using System.IO;
using System.Text;

using GameData.Resources.Data;

using ResourceExtraction.Extractors;

using Xunit;

/// <summary>
/// Verifies the lightweight save-slot header reader (<see cref="SaveGameExtractor.ReadHeader"/>)
/// used for save-game listing without parsing the full save body. Builds a synthetic 100-byte
/// header in the SAVE%02d.GAM layout, so the test needs no original game data.
/// </summary>
public class SaveGameHeaderTests {
    static SaveGameHeaderTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static MemoryStream BuildHeader(
        string name, short chapter, short worldX, short worldY, short mapIcon, short version) {
        var buffer = new byte[SaveGameHeader.Size];
        byte[] nameBytes = Encoding.GetEncoding(437).GetBytes(name);
        Array.Copy(nameBytes, buffer, nameBytes.Length); // remainder stays null-padded
        int p = SaveGameHeader.NameLength;
        void WriteShort(short v) {
            buffer[p++] = (byte)(v & 0xFF);
            buffer[p++] = (byte)((v >> 8) & 0xFF);
        }
        WriteShort(chapter);
        WriteShort(worldX);
        WriteShort(worldY);
        WriteShort(mapIcon);
        WriteShort(version);
        return new MemoryStream(buffer);
    }

    [Fact]
    public void ReadHeader_ParsesAllFields_AndLeavesStreamAfterHeader() {
        using MemoryStream stream = BuildHeader(
            "Test Save", chapter: 3, worldX: 108, worldY: 90, mapIcon: 20, version: SaveGame.SupportedVersion);

        SaveGameHeader header = SaveGameExtractor.ReadHeader(stream);

        Assert.Equal("Test Save", header.Name);
        Assert.Equal(3, header.ChapterNumber);
        Assert.Equal(108, header.WorldX);
        Assert.Equal(90, header.WorldY);
        Assert.Equal(20, header.MapIcon);
        Assert.Equal(SaveGame.SupportedVersion, header.Version);
        Assert.True(header.IsSupportedVersion);
        // ReadHeader must leave the stream positioned right after the 100-byte header
        // so Extract() can continue reading the body from the same stream.
        Assert.Equal(SaveGameHeader.Size, stream.Position);
    }

    [Fact]
    public void ReadHeader_NameWithEmbeddedNull_TruncatesAtFirstNull() {
        // DOS save names are null-terminated C-strings: bytes at/after the first NUL are padding,
        // not part of the name. A corrupt save can have a char zeroed mid-name ("dungeon\0ight");
        // ReadHeader must return "dungeon", not a string still carrying the embedded NUL.
        using MemoryStream stream = BuildHeader(
            "dungeon\0ight", chapter: 1, worldX: 0, worldY: 0, mapIcon: 0, version: SaveGame.SupportedVersion);

        SaveGameHeader header = SaveGameExtractor.ReadHeader(stream);

        Assert.Equal("dungeon", header.Name);
        // Must still consume exactly the 100-byte header (fixed 90-byte name field).
        Assert.Equal(SaveGameHeader.Size, stream.Position);
    }

    [Fact]
    public void ReadHeader_NameWithLeadingNull_IsEmpty() {
        // Leading NUL ("\0ookmark") => empty name (the C-string is zero-length). The listing layer
        // falls back to the file name for an empty display name.
        using MemoryStream stream = BuildHeader(
            "\0ookmark", chapter: 1, worldX: 0, worldY: 0, mapIcon: 0, version: SaveGame.SupportedVersion);

        SaveGameHeader header = SaveGameExtractor.ReadHeader(stream);

        Assert.Equal(string.Empty, header.Name);
    }

    [Fact]
    public void ReadHeader_WrongVersion_IsNotSupported() {
        using MemoryStream stream = BuildHeader(
            "Old", chapter: 1, worldX: 0, worldY: 0, mapIcon: 0, version: 0x15);

        SaveGameHeader header = SaveGameExtractor.ReadHeader(stream);

        Assert.False(header.IsSupportedVersion);
    }
}
