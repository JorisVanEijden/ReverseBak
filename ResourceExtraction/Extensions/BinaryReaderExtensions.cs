namespace ResourceExtraction.Extensions;

using System.Collections.Generic;
using System.IO;
using System.Text;

public static class BinaryReaderExtensions {
    private const int TagLength = 4;

    public static string ReadZeroTerminatedString(this BinaryReader reader) {
        var text = new StringBuilder();
        char character = reader.ReadChar();
        while (character != '\0') {
            text.Append(character);
            character = reader.ReadChar();
        }

        return text.ToString();
    }

    public static byte PeekByte(this BinaryReader reader) {
        long position = reader.BaseStream.Position;
        byte value = reader.ReadByte();
        reader.BaseStream.Position = position;

        return value;
    }

    public static string ReadTag(this BinaryReader resourceReader) {
        string tagString = new(resourceReader.ReadChars(TagLength));

        return tagString.TrimEnd(':');
    }

    public static Dictionary<int, string> ReadTags(this BinaryReader reader) {
        uint tagSize = reader.ReadUInt32();
        ushort numberOfTags = reader.ReadUInt16();
        var tags = new Dictionary<int, string>(numberOfTags);
        for (var i = 0; i < numberOfTags; i++) {
            tags[reader.ReadUInt16()] = reader.ReadZeroTerminatedString();
        }

        return tags;
    }
}