namespace GameData.Resources.Combat;

/// <summary>
/// Which button occupies each cell of the combat HUD — <c>COMBAT.DAT</c>'s capability slot and
/// <c>SHOOT.DAT</c>'s two pages.
///
/// <para><b>Both menus ship several entries at the SAME position.</b> COMBAT puts action ids 31, 46
/// and 14 all at one cell; SHOOT pairs 2/6, 3/8, 4/9 and 5/7 across four cells. They are alternates
/// chosen by state, not a list to lay out — a renderer that placed them naively would stack buttons
/// on top of each other and look correct until the state changed.</para>
///
/// <para>Both menus also sit on the travel HUD's own six button anchors, so combat replaces the
/// travel buttons in place rather than opening a screen.</para>
/// </summary>
public static class CombatMenuSlots {
    // ---- COMBAT.DAT: one cell, three faces, chosen by what the actor can do -----------------------
    // combat_arena_melee_menu_refresh (canassa COMBAT.C ~1335).

    /// <summary>Shown when the acting character can take a ranged shot.</summary>
    public const int ShootActionId = 0x1f;      // 31

    /// <summary>Shown when the acting character can cast.</summary>
    public const int CastActionId = 0x2e;       // 46

    /// <summary>
    /// Shown when the character can do neither — and <b>always gated</b>, so it is a label rather
    /// than a button. The shipped data agrees: this is the one COMBAT entry with Disabled set.
    /// </summary>
    public const int NeitherActionId = 0x0e;    // 14

    /// <summary>Nothing occupies the slot.</summary>
    public const int NoAction = -1;

    /// <summary>
    /// The action id live in the capability cell.
    /// </summary>
    /// <remarks>
    /// <b>Shooting is tested before casting</b>, so a character who can do both shows Shoot. That
    /// ordering is the original's and is invisible in the data — all three entries look alike there.
    /// </remarks>
    public static int CapabilitySlot(bool canShoot, bool canCast) {
        if (canShoot) {
            return ShootActionId;
        }
        return canCast ? CastActionId : NeitherActionId;
    }

    /// <summary>Whether the capability cell's occupant can be clicked.</summary>
    /// <remarks>The neither-case is gated even though it is drawn; the other two are live.</remarks>
    public static bool CapabilitySlotIsClickable(int actionId) => actionId != NeitherActionId;

    // ---- SHOOT.DAT: two pages of four, over the same four cells -----------------------------------
    // combat_arena_shootmenu_init / combat_arena_shootmenu_ent_avail (COMBAT.C ~902).

    /// <summary>Quarrel kinds on the first page, in cell order.</summary>
    public static readonly int[] FirstPageActionIds = { 2, 3, 4, 5 };

    /// <summary>Quarrel kinds on the second page, in the SAME four cells.</summary>
    public static readonly int[] SecondPageActionIds = { 6, 8, 9, 7 };

    /// <summary>The button that flips between the two pages.</summary>
    public const int PageFlipActionId = 50;

    /// <summary>First page number, as the original counts them.</summary>
    public const int FirstPage = 1;

    /// <inheritdoc cref="FirstPage"/>
    public const int SecondPage = 2;

    /// <summary>Flipping is a toggle between exactly two pages.</summary>
    public static int FlipPage(int page) => page == FirstPage ? SecondPage : FirstPage;

    /// <summary>Which page an action id belongs to, or 0 if it is not a quarrel button.</summary>
    public static int PageOf(int actionId) {
        foreach (int id in FirstPageActionIds) {
            if (id == actionId) {
                return FirstPage;
            }
        }
        foreach (int id in SecondPageActionIds) {
            if (id == actionId) {
                return SecondPage;
            }
        }
        return 0;
    }

    /// <summary>
    /// Whether a quarrel button is live: its page must be showing <b>and</b> the actor must actually
    /// carry that kind.
    /// </summary>
    /// <remarks>
    /// Two conditions, and the second is the one a port drops — it makes the menu show only the
    /// ammunition you have, so an empty kind greys out rather than being clickable and failing.
    /// </remarks>
    public static bool QuarrelIsAvailable(int actionId, int currentPage, int quarrelsOfThatKind) =>
        PageOf(actionId) == currentPage && quarrelsOfThatKind != 0;
}
