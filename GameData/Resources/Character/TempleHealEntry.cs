namespace GameData.Resources.Character;

/// <summary>What asking a temple for healing actually gets you.</summary>
public enum TempleHealOpening {
    /// <summary>The Cure / Next / Done screen opens.</summary>
    Screen,

    /// <summary>Nobody is afflicted and nobody is hurt: the priest finds nothing wrong at all.</summary>
    NothingIsWrong,

    /// <summary>
    /// Nobody is afflicted, but somebody is wounded — and wounds are not what a temple mends.
    /// </summary>
    WoundsAreNotOurBusiness,
}

/// <summary>
/// Whether a temple opens its healing screen, and what it says when it does not. Faithful port of
/// the gate at the top of <c>charscreen_temple_heal_menu</c> @0x5877e (ovr160).
/// </summary>
/// <remarks>
/// <b>A temple treats afflictions, not injuries.</b> The gate asks two separate questions in order —
/// is anyone carrying a condition worth charging for, and failing that, is anyone below full health
/// — and the answers lead to two different refusals rather than one. Being merely wounded gets a
/// priest who explains that wounds heal with time or with a chirurgeon, and that he mends only
/// things of a spiritual nature. Collapsing the two into a single "nothing to do" would throw away
/// the one line that tells the player where to go instead.
/// </remarks>
public static class TempleHealEntry {
    /// <summary>The priest finds nothing wrong with anyone.</summary>
    public const int NothingIsWrongDialog = 1300075;

    /// <summary>Wounds are not a temple's business — try time, or a chirurgeon.</summary>
    public const int WoundsAreNotOurBusinessDialog = 1300083;

    /// <summary>
    /// The one mode that will also treat plain wounds.
    /// </summary>
    /// <remarks>
    /// <b>Mode is a generosity setting, and it only matters in one case.</b> An afflicted party
    /// opens the screen under every mode, because the first test short-circuits before the mode is
    /// ever consulted. The mode is reached only when nobody is afflicted — and there mode 4 still
    /// opens the screen for the wounded, while every other mode turns them away with
    /// <see cref="WoundsAreNotOurBusinessDialog"/>.
    /// </remarks>
    public const int ModeThatTreatsWounds = 4;

    /// <summary>
    /// What the temple does when asked.
    /// </summary>
    /// <param name="anyoneAfflicted">
    /// Whether any active party member prices above zero — the same call that later decides whether
    /// to offer them a Cure button. One function, two jobs; see <see cref="TempleHeal"/>.
    /// </param>
    /// <param name="anyoneWounded">Whether any active member is below full health-and-stamina.</param>
    /// <param name="mode">The service's mode; see <see cref="ModeThatTreatsWounds"/>.</param>
    public static TempleHealOpening Decide(bool anyoneAfflicted, bool anyoneWounded, int mode) {
        // Short-circuits before the mode is looked at: an afflicted party is always seen.
        if (anyoneAfflicted) {
            return TempleHealOpening.Screen;
        }

        if (!anyoneWounded) {
            return TempleHealOpening.NothingIsWrong;
        }

        return mode == ModeThatTreatsWounds
            ? TempleHealOpening.Screen
            : TempleHealOpening.WoundsAreNotOurBusiness;
    }

    /// <summary>The dialog an opening speaks, or 0 when it opens the screen instead.</summary>
    public static int DialogFor(TempleHealOpening opening) => opening switch {
        TempleHealOpening.NothingIsWrong => NothingIsWrongDialog,
        TempleHealOpening.WoundsAreNotOurBusiness => WoundsAreNotOurBusinessDialog,
        _ => 0,
    };

    // ---- the screen ---------------------------------------------------------------------------

    /// <summary>The layout, and the palette it borrows.</summary>
    /// <remarks>
    /// <b>It runs under the inventory's palette, not one of its own.</b> The screen is a character
    /// view first and a shop second, so it is dressed like the inventory screen it sits beside.
    /// </remarks>
    public const string Layout = "REQ_HEAL.DAT";

    /// <inheritdoc cref="Layout"/>
    public const string Palette = "INVENTOR.PAL";

    /// <summary>Cure the member currently shown.</summary>
    public const int CureActionId = 35;

    /// <summary>
    /// Show the next member.
    /// </summary>
    /// <remarks>
    /// The portrait carries this action too — REQ_HEAL ships an invisible click area over it with
    /// the same id — so clicking the character advances just as the button does.
    /// </remarks>
    public const int NextActionId = 49;

    /// <summary>Leave.</summary>
    public const int DoneActionId = 1;

    /// <summary>The quote shown before a cure is paid for.</summary>
    public const int PriceQuoteDialog = 1300077;
}
