namespace ResourceExtractor.Extensions;

using System.Text;

public static class BinaryReaderExtensions {
    public static string ReadZeroTerminatedString(this BinaryReader reader) {
        var text = new StringBuilder();
        char character = reader.ReadChar();
        while (character != '\0') {
            text.Append(character);
            character = reader.ReadChar();
        }

        return text.ToString();
    }
}