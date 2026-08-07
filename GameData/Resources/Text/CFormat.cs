namespace GameData.Resources.Text;

using System.Globalization;
using System.Text;

/// <summary>
/// The sliver of C <c>printf</c> the catalog's format strings actually use: <c>%d</c>, <c>%ld</c>,
/// <c>%s</c> (and <c>%Fs</c>, the far-pointer variant the original's 16-bit compiler emitted), plus
/// <c>%%</c> for a literal percent. Conversions consume arguments in order.
///
/// <para>Deliberately not a general printf: width, precision and flags never appear in the strings
/// we extract, and supporting them would be untested surface. A malformed override degrades to an
/// empty substitution rather than throwing — a wrong label is survivable, a crash mid-render is not.</para>
/// </summary>
public static class CFormat {
    public static string Apply(string format, params object[] args) {
        if (string.IsNullOrEmpty(format)) {
            return format ?? "";
        }
        var sb = new StringBuilder(format.Length + 16);
        int arg = 0;
        for (int i = 0; i < format.Length; i++) {
            if (format[i] != '%') {
                sb.Append(format[i]);
                continue;
            }
            int j = i + 1;
            if (j < format.Length && format[j] == '%') {
                sb.Append('%');
                i = j;
                continue;
            }
            // Skip length modifiers: l, ld, F (far), h.
            while (j < format.Length && (format[j] == 'l' || format[j] == 'F' || format[j] == 'h')) {
                j++;
            }
            if (j >= format.Length) {
                sb.Append(format, i, format.Length - i);
                break;
            }
            char conv = format[j];
            if (conv == 'd' || conv == 's' || conv == 'c' || conv == 'u') {
                object value = args != null && arg < args.Length ? args[arg] : null;
                arg++;
                if (value != null) {
                    sb.Append(System.Convert.ToString(value, CultureInfo.InvariantCulture));
                }
                i = j;
                continue;
            }
            sb.Append(format[i]); // unknown conversion: emit the '%' verbatim
        }
        return sb.ToString();
    }
}
