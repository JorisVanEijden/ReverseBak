namespace GameData.Resources.Dialog;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// Faithful port of RenderDialogText's @N substitution (KRONDOR.EXE 0x48d7b). Replaces @N
/// (digit) with text-variable slot N; @Ns appends a possessive 's'; a non-digit after @ is
/// left verbatim (the DOS "current speaker" fallback is handled upstream by defaulting slots).
/// </summary>
public static class TextVariableResolver {
    public static string Substitute(string text, IReadOnlyList<string> slots) {
        if (string.IsNullOrEmpty(text)) {
            return text;
        }
        var sb = new StringBuilder(text.Length + 16);
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c == '@' && i + 1 < text.Length && char.IsDigit(text[i + 1])) {
                int n = text[i + 1] - '0';
                if (n >= 0 && n < slots.Count && !string.IsNullOrEmpty(slots[n])) {
                    sb.Append(slots[n]);
                    i++; // consume the digit
                    if (i + 1 < text.Length && text[i + 1] == 's') {
                        sb.Append('s'); i++; // @Ns possessive
                    }
                    continue;
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
