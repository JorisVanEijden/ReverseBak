namespace GameData;

[Flags]
public enum ItemFlags {
    Lit = 0x1,
    Unknown2 = 0x2,
    /// <summary>
    /// Set by degradation at the same time as <see cref="Repairable"/> — meaning not established.
    /// </summary>
    /// <remarks>
    /// The V102CD build sets <c>0x4</c> first and <c>0x20</c> only if the break roll passes; the
    /// floppy sets <c>0x24</c> in one go. So <c>0x4</c> marks something about having been struck
    /// that survives even when no wear is applied. Left named Unknown rather than guessed.
    /// </remarks>
    Unknown4 = 0x4,
    Unknown8 = 0x8,
    /// <summary>Worn out — condition reached zero. Set by degradation alongside
    /// <see cref="Repairable"/>.</summary>
    Broken = 0x10,

    /// <summary>
    /// Damaged and therefore repairable.
    /// </summary>
    /// <remarks>
    /// <b>THIS IS A STATE, NOT A CAPABILITY, and the polarity is the opposite of how the name
    /// reads.</b> It is SET when an item takes wear (<c>cbstat_damage_equipped_items</c>,
    /// CBSTAT.C:476) and CLEARED when the item is repaired (EVTCOND.C:40, via
    /// <c>party_surveyArmourAndOptionallyRepair</c>). A pristine item does NOT carry it. Read as
    /// "this item can be repaired in principle" it looks like a static property of the item type,
    /// and code that filtered on it that way would show every undamaged weapon as needing work.
    ///
    /// <para><b>The name is kept because it is the GAME'S OWN WORD for the state</b> — the shipped
    /// executable carries the UI string "Repairable" (<c>item.repairable</c> in the string
    /// manifest). Renaming it to <c>Damaged</c> would read more obviously but would diverge from
    /// what the player is shown. Checked 2026-08-29 (TASK-250).</para>
    /// </remarks>
    Repairable = 0x20,
    Equipped = 0x40,
    Poisoned = 0x80,
    Flaming = 0x100,
    SteelFired = 0x200,
    Frosted = 0x400,
    Enhanced1 = 0x800,
    Enhanced2 = 0x1000,
    Blessed1 = 0x2000,
    Blessed2 = 0x4000,
    Blessed3 = 0x8000,
}