namespace GameData.Resources.World;

/// <summary>
/// The travel HUD's keyboard shortcuts — which are not a table the game keeps at all.
/// </summary>
/// <remarks>
/// <b>REQ_MAIN's action ids ARE DOS scancodes, and that is the whole mechanism.</b> The four
/// movement entries have always been read that way (72/80/75/77 are the arrow keys), but it holds
/// for the round buttons too: the map is <b>50 = 0x32 = M</b>, encamp <b>18 = 0x12 = E</b>, cast
/// <b>46 = 0x2E = C</b>, follow-road <b>19 = 0x13 = R</b>, options <b>24 = 0x18 = O</b> and
/// bookmark <b>48 = 0x30 = B</b>. So the original needs no key-to-action mapping: the scancode the
/// keyboard produces is already the action the menu page dispatches, which is why those ids look
/// arbitrary until you put them next to a scancode chart.
///
/// <para>That also explains the toggle the port was missing. <c>WORLDLP.C:316</c> sends action 0x32
/// to <c>map_main_loop()</c>, and <c>MAP.C:398</c> — the map screen's OWN dispatch for the same
/// action — sets <c>keep_running = 0</c>. One action, both halves, because both pages carry an
/// entry for it (TASK-584).</para>
///
/// <para><b>Only letters are listed.</b> The arrows already reach movement through their own path,
/// and adding them here would give a second owner for keys the travel surface deliberately treats
/// as movement rather than as button presses.</para>
/// </remarks>
public static class TravelHotkeys {
    /// <summary>The action a letter names, or -1 — the letter's DOS scancode.</summary>
    /// <remarks>
    /// <b>Returning the scancode is not a shortcut, it IS the answer</b> — see the type's remarks.
    /// A caller should still check that the page it is dispatching to actually carries an entry for
    /// the id, exactly as <c>menupage_run</c> does: a letter whose scancode matches no entry must do
    /// nothing rather than fire a neighbouring action.
    /// </remarks>
    public static int ActionFor(char letter) {
        switch (char.ToUpperInvariant(letter)) {
            case 'R': return 0x13;   // 19 — follow road
            case 'E': return 0x12;   // 18 — encamp
            case 'C': return 0x2E;   // 46 — cast spell
            case 'B': return 0x30;   // 48 — bookmark
            case 'O': return 0x18;   // 24 — options
            case 'M': return 0x32;   // 50 — the local map
            default: return NoAction;
        }
    }

    /// <summary>What <see cref="ActionFor"/> answers for a letter the HUD does not use.</summary>
    public const int NoAction = -1;
}
