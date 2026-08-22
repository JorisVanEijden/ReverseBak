namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The describe text behind each combat button — the preview branch every case of
/// <c>combat_arena_show_message_by_id</c> opens with (canassa COMBAT.C ~1985).
///
/// <para><b>Right-click describes; left-click acts.</b> Each case begins
/// <c>if (is_preview) { dialog_play_record(id, 1); return; }</c>, so the describe path never touches
/// the fight — it plays a record and returns. Both menus share the one switch, which is why the
/// quarrel buttons appear here alongside Defend and Cast.</para>
/// </summary>
public static class CombatHelp {
    /// <summary>
    /// Help record for a combat action id, or -1 when the button has none.
    /// </summary>
    /// <remarks>
    /// <b>The records are consecutive in SWITCH order, not action-id order</b> — 0xfe through 0x10d
    /// walking 2,3,4,5,6,8,9,7,50,19,31,46,32,47,30,33. Deriving an id arithmetically from the action
    /// number would land on the wrong text for almost every button.
    ///
    /// <para>The quarrel run is <c>2,3,4,5,6,8,9,7</c> — the same out-of-order sequence as
    /// <see cref="CombatMenuSlots.ActionIdByQuarrelKind"/>, reached independently here. So the help
    /// records are in QUARREL KIND order, which is corroboration that the kind table is right.</para>
    /// </remarks>
    private static readonly Dictionary<int, int> ByActionId = new Dictionary<int, int> {
        { 2, 0xfe }, { 3, 0xff }, { 4, 0x100 }, { 5, 0x101 },
        { 6, 0x102 }, { 8, 0x103 }, { 9, 0x104 }, { 7, 0x105 },
        { 50, 0x106 },
        { 19, 0x107 },
        { 31, 0x108 },
        { 46, 0x109 },
        { 32, 0x10a },
        { 47, 0x10b },
        { 30, 0x10c },
        { 33, 0x10d },
    };

    /// <summary>No describe record for this button.</summary>
    public const int None = -1;

    /// <summary>The first record in the run.</summary>
    public const int FirstRecord = 0xfe;

    /// <summary>The describe record for a button, or <see cref="None"/>.</summary>
    /// <remarks>
    /// <b>Two combat buttons have no record, and both make sense.</b> Action 14 is the capability
    /// cell's disabled label — drawn but never clickable, so it can never be right-clicked either;
    /// and action 22 is the hidden character-screen zone, which ships <c>Visible=False</c>. Neither
    /// appears in the switch at all.
    /// </remarks>
    public static int DialogFor(int actionId) =>
        ByActionId.TryGetValue(actionId, out int record) ? record : None;

    /// <summary>Whether a button offers describe text.</summary>
    public static bool HasDialog(int actionId) => DialogFor(actionId) != None;

    /// <summary>How many buttons carry a record.</summary>
    public static int Count => ByActionId.Count;
}
