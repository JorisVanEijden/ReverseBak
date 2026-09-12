namespace GameData.Resources.Combat;

/// <summary>
/// The damage-TYPE mask a blow carries — <c>cbstat_armor_coverage_mask</c> (canassa
/// <c>CBSTAT.C:172</c>), which <c>combat_arena_apply_damage</c> matches against the target's class
/// affinity rows.
/// </summary>
/// <remarks>
/// <b>Not armour, despite canassa's name for it.</b> The bits are the effect-mask space
/// <see cref="CreatureAffinity"/> documents (1 = poison, 2 = Skin of the Dragon, 4 = Flamecast,
/// 0x10 = Candle Glow, 0x20 = Grief of 1000 Nights): what the routine answers is <i>which damage
/// types this swing is made of</i>, so a poison-resistant creature can halve a poisoned blade and a
/// fire-weak one can take half again from a burning one.
///
/// <para><b>A plain swing is not type-less.</b> The mask starts at <see cref="BaseSwing"/> = 0x580
/// before any coating is read, and eight shipped creature classes carry one of those three bits —
/// 244 combatant records between them — so the base value alone changes what a third of the
/// bestiary takes from an ordinary blow.</para>
///
/// <para><b>Only the first equipped SWORD contributes.</b> The original's loop breaks on the first
/// equipped item of category 1 whether or not it added anything, so a crossbow or a staff never
/// colours a blow and a second blade is never read.</para>
/// </remarks>
public static class CombatDamageMask {
    /// <summary>What every swing carries before any coating is read.</summary>
    public const int BaseSwing = 0x580;

    /// <summary>What a SPELL's damage carries — <c>cspell_resolve_cast</c>'s kind-0 arm.</summary>
    /// <remarks>
    /// The caster's own bill is a different call with a mask of <b>0</b>
    /// (<c>cspell_apply_damage_armor_wear</c>), so no affinity touches it.
    /// </remarks>
    public const int Spell = 0x200;

    /// <summary>
    /// The mask for a swing made with the given equipped-sword slot flags; pass 0 for no sword.
    /// </summary>
    public static int ForSwing(int swordSlotFlags) {
        int mask = BaseSwing;

        // Three separate slot bits all set the same mask bit in the original, written out as three
        // ifs. Kept as one test because they cannot disagree.
        if ((swordSlotFlags & 0xE000) != 0) {
            mask |= 0x800;
        }
        if ((swordSlotFlags & 0x0080) != 0) {
            mask |= 0x001;
        }
        // 0x100 and 0x200 both mean the same type, again as two ifs in the original.
        if ((swordSlotFlags & 0x0300) != 0) {
            mask |= 0x004;
        }
        if ((swordSlotFlags & 0x0400) != 0) {
            mask |= 0x002;
        }
        if ((swordSlotFlags & 0x0800) != 0) {
            mask |= 0x010;
        }
        if ((swordSlotFlags & 0x1000) != 0) {
            mask |= 0x020;
        }

        return mask;
    }
}
