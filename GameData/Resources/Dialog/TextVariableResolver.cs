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
    /// <summary>Substitute against a full slot table, so the creature-name rules (which need each
    /// slot's KIND, not just its text) can apply.</summary>
    public static string Substitute(string text, DialogSlotTable table, string currentActorName = null) =>
        Substitute(text, table?.Names, currentActorName, table?.Kinds);

    /// <param name="currentActorName">The name a bare <c>@</c> resolves to (the engine's
    /// <c>nEvtArgActor0</c>). Empty leaves a bare <c>@</c> verbatim rather than silently deleting
    /// it, so a missing actor is visible instead of quietly changing the sentence.</param>
    /// <param name="kinds">Each slot's kind, when known. Only slots marked
    /// <see cref="DialogSlotTable.CreatureActor"/> take the article/possessive reshaping; without
    /// kinds every slot is treated as an ordinary name.</param>
    public static string Substitute(string text, IReadOnlyList<string> slots,
        string currentActorName = null, IReadOnlyList<int> kinds = null) {
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
                    string name = slots[n] ?? "";
                    bool creature = kinds != null && n < kinds.Count
                        && kinds[n] == DialogSlotTable.CreatureActor;
                    bool possessive = i + 2 < text.Length && text[i + 2] == 's';

                    if (creature && TryFixArticle(sb, name)) {
                        // "a Owl" -> "an Owl": the article was already written, so the fix edits
                        // the output behind us. Mutually exclusive with the possessive reshape,
                        // as in the engine's if/else-if.
                    } else if (creature && possessive) {
                        name = ReshapeForPossessive(name);
                    }

                    // Substituted even when empty: that is the engine's own behaviour, and it is
                    // what stops an unfilled slot printing as "@4".
                    sb.Append(name);
                    i++; // consume the digit
                    if (possessive) {
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

    /// <summary>
    /// "a Owl" → "an Owl". The engine writes the article as ordinary text before it reaches the
    /// token, so the fix edits what has already been emitted: it overwrites the last character with
    /// 'n' and appends a space (<c>_fstrcpy(scratch + len - 1, "n ")</c>).
    ///
    /// <para>Its test is exactly "the creature's name starts with A or O, and the second-to-last
    /// character emitted so far is an 'a'" — it never checks that the character between them is a
    /// space, so "Anna @0" becomes "Annan Owl". That is the engine's own behaviour and is
    /// reproduced rather than tightened.</para>
    /// </summary>
    private static bool TryFixArticle(StringBuilder sb, string name) {
        if (string.IsNullOrEmpty(name) || (name[0] != 'A' && name[0] != 'O')) {
            return false;
        }
        if (sb.Length < 2 || char.ToLowerInvariant(sb[sb.Length - 2]) != 'a') {
            return false;
        }
        sb[sb.Length - 1] = 'n';
        sb.Append(' ');
        return true;
    }

    /// <summary>A creature name's tail before a possessive 's': 'h' becomes "he" and 'y' becomes
    /// "ie", so "Wraith's" reads "Wraithes" and "Harpy's" reads "Harpies". Only creature slots get
    /// this — a party member's name takes the plain 's'.</summary>
    private static string ReshapeForPossessive(string name) {
        if (string.IsNullOrEmpty(name)) {
            return name;
        }
        char last = name[name.Length - 1];
        if (last == 'h') {
            return name + "e";
        }
        if (last == 'y') {
            return name.Substring(0, name.Length - 1) + "ie";
        }
        return name;
    }
}
