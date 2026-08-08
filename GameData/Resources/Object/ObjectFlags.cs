namespace GameData.Resources.Object;

/// <summary>
/// The object record's flag word. The four use-gate bits are the guard chain every "use" runs
/// through first — <c>itemuse_dispatch_on_target</c> (ITEMUSE.C:104-131, <c>Use_Item</c> @0x58cbd),
/// which tests each one and plays a refusal record instead of dispatching.
/// </summary>
[Flags]
public enum ObjectFlags {
    B0001 = 0x0001,
    NotEquipable = 0x0002,
    B0004 = 0x0004,
    B0008 = 0x0008,
    B0010 = 0x0010,
    B0020 = 0x0020,
    OnlyUsableInCombat = 0x0040,

    /// <summary>Only a spellcaster may use it: refused (DDX 1800005) when the member's
    /// <see cref="ActorAttribute.AccuracyCasting"/> maximum is 0 — <c>(flags &amp; 0x80) &amp;&amp;
    /// member->stats[7].max == 0</c>. Stat index 7 is AccuracyCasting; the indexing is confirmed
    /// independently by the repair branch, which picks stat 9/10 (ArmorCraft/WeaponCraft) by
    /// target category.</summary>
    SpellcastersOnly = 0x0080,

    NotUsableInCombat = 0x0100,

    /// <summary>The mirror gate: refused (DDX 1800049) when the member IS a spellcaster —
    /// <c>(flags &amp; 0x200) &amp;&amp; member->stats[7].max != 0</c>. Was named
    /// <c>ArchersOnly</c>, which was a guess: the predicate is casting skill, not a bow. Warriors
    /// and archers pass it only because they have no casting skill.</summary>
    NonSpellcastersOnly = 0x0200,

    B0400 = 0x0400,
    Stackable = 0x0800,
    B1000 = 0x1000,
    LimitedUses = 0x2000,
    B4000 = 0x4000,
    B8000 = 0x8000
}