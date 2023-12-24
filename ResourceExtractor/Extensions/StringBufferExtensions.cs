namespace ResourceExtractor.Extensions;

using System.Text;

public static class StringBufferExtensions {
    internal static string GetZeroTerminatedString(this IReadOnlyList<char> stringBuffer, int offset) {
        var label = new StringBuilder();
        for (int i = offset; i < stringBuffer.Count; i++) {
            if (stringBuffer[i] == '\0') {
                break;
            }
            label.Append(stringBuffer[i]);
        }
        return label.ToString();
    }
}