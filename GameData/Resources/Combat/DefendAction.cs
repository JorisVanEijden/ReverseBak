namespace GameData.Resources.Combat;

/// <summary>
/// The combat HUD's DEFEND button (action id 32) — <c>combatenc_set_flag8_clear_flag1</c>
/// (canassa CBENC.C:789).
///
/// <para><b>Defend and Rest are different buttons and were transposed here until 2026-08-22.</b>
/// Defend raises a guard and nothing else; <see cref="RestAction"/> (id 19) is the one that heals.
/// The game's own describe records settle it: DDX 266 "allows the current character to defend for
/// one turn" against DDX 263 "causes the current character to rest for one round".</para>
/// </summary>
public static class DefendAction {
    /// <summary>
    /// <b>Defend sets <see cref="CombatantFlags.Parry"/></b> — and that flag already has a consumer.
    /// </summary>
    /// <remarks>
    /// <see cref="CombatFormulas.MeleeHits"/> reads Parry and applies its penalty <b>to the attacker's
    /// ROLL rather than to the hit chance</b>, so the benefit cannot be swallowed by the 2..98 clamp.
    /// Defending therefore has a real, already-implemented effect the moment this is wired: it makes
    /// the defender harder to hit.
    ///
    /// <para>Contrast <see cref="RestAction.FlagSet"/>, which is
    /// <see cref="CombatantFlags.DefendCommand"/> (0x04) and feeds nothing in the to-hit path.</para>
    /// </remarks>
    public static CombatantFlags FlagSet => CombatantFlags.Parry;

    /// <summary>Help text shown when the button is right-clicked instead of pressed.</summary>
    public const int HelpDialog = 0x10a;

    /// <summary>
    /// Raise a guard for the round.
    /// </summary>
    /// <remarks>
    /// <b>The whole routine is two flag operations</b> — set Parry, clear Ready. There is no roll, no
    /// recovery and no animation: unlike <see cref="RestAction.Apply"/> it costs the turn and gives
    /// nothing back except the to-hit penalty imposed on whoever attacks.
    /// </remarks>
    public static void Apply(Combatant actor) {
        if (actor == null) {
            return;
        }
        actor.Flags |= FlagSet;
        actor.Flags &= ~CombatantFlags.Ready;
    }
}
