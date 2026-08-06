namespace GameData.Resources.Dialog;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// Faithful port of <c>dialog_render_text_with_tokens</c>'s <c>@</c> substitution (DIALOG.C:598-632,
/// <c>RenderDialogText</c> @0x48d7b).
///
/// <list type="bullet">
/// <item><c>@N</c> (a digit) is replaced by text-variable slot N. <b>An empty slot contributes
/// nothing</b> — the engine copies a zero-length string and moves on, so a slot nothing filled
/// makes the token disappear. It never renders the token itself.</item>
/// <item><c>@</c> followed by anything else is replaced by the CURRENT ACTOR's name and only the
/// <c>@</c> is consumed — that is how "<c>@ gaped in astonishment</c>" and "<c>@'s lack of musical
/// mastery</c>" read as sentences.</item>
/// <item><c>@Ns</c> appends the possessive 's'. (The engine also reshapes a creature name's tail
/// there — 'h' to "he", 'y' to "ie" — but only for slots holding a creature, which nothing fills
/// yet.)</item>
/// </list>
/// </summary>
public static class TextVariableResolver {
    /// <param name="currentActorName">The name a bare <c>@</c> resolves to (the engine's
    /// <c>nEvtArgActor0</c>). Empty leaves a bare <c>@</c> verbatim rather than silently deleting
    /// it, so a missing actor is visible instead of quietly changing the sentence.</param>
    public static string Substitute(string text, IReadOnlyList<string> slots,
        string currentActorName = null) {
        if (string.IsNullOrEmpty(text)) {
            return text;
        }
        var sb = new StringBuilder(text.Length + 16);
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c != '@') {
                sb.Append(c);
                continue;
            }
            if (i + 1 < text.Length && char.IsDigit(text[i + 1])) {
                int n = text[i + 1] - '0';
                if (n >= 0 && n < slots.Count) {
                    // Substituted even when empty: that is the engine's own behaviour, and it is
                    // what stops an unfilled slot printing as "@4".
                    sb.Append(slots[n] ?? "");
                    i++; // consume the digit
                    if (i + 1 < text.Length && text[i + 1] == 's') {
                        sb.Append('s'); i++; // @Ns possessive
                    }
                    continue;
                }
                // A digit outside the six slots. The engine would read past its slot array; refuse
                // to invent something and leave the token visible instead.
                sb.Append(c);
                continue;
            }
            if (!string.IsNullOrEmpty(currentActorName)) {
                sb.Append(currentActorName); // only the '@' is consumed
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
