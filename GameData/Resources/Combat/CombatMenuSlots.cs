namespace GameData.Resources.Combat;

using System.Collections.Generic;

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
    /// <b>UNVERIFIED PRECEDENCE — the original may not have any.</b> This returns a single id, on the
    /// reading that shooting is tested before casting. Re-checking against canassa on 2026-08-22 did
    /// not confirm it: <c>combat_arena_menu_refr_avail</c> (COMBAT.C ~1334) sets all three entries
    /// INDEPENDENTLY —
    /// <code>
    /// 0x1f -> combatenc_show_missile_stat_row(actor)
    /// 0x2e -> combatenc_actor_can_cast_spells(actor, 1)
    /// 0x0e -> neither of the above, and wEnable_gate = 1 always
    /// </code>
    /// with no precedence between them, and <c>bActive_flag</c> gates both drawing (WIDGET.C:161)
    /// and hit-testing (COMBAT.C:1049). So an actor who could shoot AND cast would have the original
    /// drawing BOTH at the same cell.
    ///
    /// <para>The routine this summary originally cited, <c>combat_arena_melee_menu_refresh</c>, does
    /// not exist anywhere in canassa — so the precedence claim has no located source and should be
    /// treated as unsupported until someone finds one.</para>
    ///
    /// <para><b>Not changed yet, deliberately.</b> Modelling it faithfully means expressing all three
    /// predicates rather than picking one, and neither predicate exists on our side yet for a party
    /// member (there is no canShoot/canCast for the party — <see cref="CombatAi"/>'s are monster
    /// profile fields). Fixing the precedence alone, before the predicates exist, would be churn.
    /// Whether the both-true case can even arise depends on a caster carrying a crossbow, which is
    /// also unestablished.</para>
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
    // combat_arena_shootmenu_rebuild / combat_arena_menu_find_item_page (COMBAT.C ~966).

    /// <summary>
    /// The action id for each quarrel kind, in kind order — the original's
    /// <c>g_awQuarrelKindItemIdTable</c>.
    /// </summary>
    /// <remarks>
    /// <b>This is the only static thing about the shoot menu.</b> Kind 0 is always id 2, kind 7
    /// always id 7. Which CELL an id occupies, and therefore which page it is on, is not fixed —
    /// see <see cref="PageOfSlot"/>.
    /// </remarks>
    public static readonly int[] ActionIdByQuarrelKind = { 2, 3, 4, 5, 6, 8, 9, 7 };

    /// <summary>The quarrel kind an action id stands for, or -1 if it is not a quarrel button.</summary>
    public static int QuarrelKindFor(int actionId) {
        for (var kind = 0; kind < ActionIdByQuarrelKind.Length; kind++) {
            if (ActionIdByQuarrelKind[kind] == actionId) {
                return kind;
            }
        }
        return -1;
    }

    /// <summary>Cells per page.</summary>
    public const int SlotsPerPage = 4;

    /// <summary>The button that flips between the two pages.</summary>
    public const int PageFlipActionId = 50;

    /// <summary>First page number, as the original counts them.</summary>
    public const int FirstPage = 1;

    /// <inheritdoc cref="FirstPage"/>
    public const int SecondPage = 2;

    /// <summary>Flipping is a toggle between exactly two pages.</summary>
    public static int FlipPage(int page) => page == FirstPage ? SecondPage : FirstPage;

    /// <summary>
    /// <b>A button's page comes from WHERE IT SITS, not from which id it is.</b>
    /// </summary>
    /// <remarks>
    /// <c>combat_arena_menu_find_item_page</c> looks the id up in the live entry list and returns
    /// <c>(index &gt;&gt; 2) + 1</c> — the first four cells are page one, the next four page two.
    ///
    /// <para><b>And the entry list is rewritten for every actor.</b> <c>shootmenu_rebuild</c> walks
    /// the eight quarrel kinds, and for each one the actor actually carries it writes that kind's id
    /// into the NEXT free cell; the leftover cells are stuffed with page fillers. So an archer
    /// carrying only kinds 5 and 6 has their ids in cells 0 and 1 — on page one — even though a
    /// table built from kind order would put them on page two.</para>
    ///
    /// <para>This is what the old model got wrong: it split
    /// <see cref="ActionIdByQuarrelKind"/> down the middle and called the halves the two pages. That
    /// is not even true of the shipped file, whose cells run 2,3,4,7 then 6,8,9,5, and it is not
    /// true at runtime for any actor who is missing a kind — which is most of them.</para>
    /// </remarks>
    public static int PageOfSlot(int slotIndex) =>
        slotIndex < 0 ? 0 : (slotIndex / SlotsPerPage) + 1;

    /// <summary>
    /// Whether the quarrel button in a cell is live: its page must be showing <b>and</b> the actor
    /// must carry that kind.
    /// </summary>
    /// <remarks>
    /// Both halves of <c>combat_arena_shootmenu_ent_avail</c>. The ammunition test is the one a port
    /// drops, and it is why the menu shows only what you are carrying rather than offering an empty
    /// kind that then fails.
    /// </remarks>
    public static bool QuarrelIsAvailable(int slotIndex, int currentPage, int quarrelsOfThatKind) =>
        PageOfSlot(slotIndex) == currentPage && quarrelsOfThatKind != 0;

    /// <summary>
    /// Packs the kinds an actor carries into cells, the way <c>shootmenu_rebuild</c> does.
    /// </summary>
    /// <param name="quarrelsOfKind">How many of each of the eight kinds the actor holds.</param>
    /// <returns>The action id for each cell, or -1 for a cell left empty.</returns>
    /// <remarks>
    /// The repack is the whole reason page cannot be read off an id. Kinds are visited in order and
    /// claim cells in order, so carrying fewer kinds pulls later ones onto the first page.
    /// </remarks>
    public static int[] PackCells(IReadOnlyList<int> quarrelsOfKind) {
        var cells = new int[ActionIdByQuarrelKind.Length];
        for (var i = 0; i < cells.Length; i++) {
            cells[i] = -1;
        }
        if (quarrelsOfKind == null) {
            return cells;
        }

        var claimed = 0;
        for (var kind = 0; kind < ActionIdByQuarrelKind.Length; kind++) {
            if (kind < quarrelsOfKind.Count && quarrelsOfKind[kind] != 0) {
                cells[claimed++] = ActionIdByQuarrelKind[kind];
            }
        }
        return cells;
    }
}
