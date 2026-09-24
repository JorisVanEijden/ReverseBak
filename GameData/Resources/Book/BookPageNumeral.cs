namespace GameData.Resources.Book;

/// <summary>
/// The Roman numeral the original prints under a book page — <c>booktext_int_to_roman</c>
/// (canassa SCREENS/BOOKTEXT.C:364), fed the page's <see cref="Page.PageDisplayNumber"/>.
/// </summary>
/// <remarks>
/// <b>Not textbook numerals, on purpose.</b> The subtractive arm takes the largest smaller digit that
/// fits, not the one the rules pair, so 456 prints LDVI (not CDLVI), 49 prints IL and 99 IC. The
/// original's own screen shows LDVI on C92's first page; a port with correct numerals would show a
/// different book.
/// </remarks>
public static class BookPageNumeral {
    private const string Digits = "MDCLXVI";
    private static readonly int[] Values = [1000, 500, 100, 50, 10, 5, 1];
    private static readonly int[] SubtractivePair = [100, 100, 10, 10, 1, 1, 0];

    public static string For(int value) {
        var text = new System.Text.StringBuilder();
        while (value > 0) {
            for (var i = 0; i < Values.Length; i++) {
                if (Values[i] <= value) {
                    text.Append(Digits[i]);
                    value -= Values[i];
                    break;
                }
                if (SubtractivePair[i] != 0 && Values[i] - SubtractivePair[i] <= value) {
                    for (int j = Values.Length - 1; j > i; j--) {
                        if (Values[i] - Values[j] <= value) {
                            text.Append(Digits[j]);
                            value += Values[j];
                            break;
                        }
                    }
                    break;
                }
            }
        }
        return text.ToString();
    }
}
