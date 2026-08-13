namespace GameData.Resources.Spells;

using GameData;

/// <summary>
/// How much a spell actually does, given what the caster paid for it —
/// <c>Spell_CalcEffectMagnitude</c> @0x68245 (canassa's <c>cspell_compute_effect_magnitude</c>).
///
/// <para>This is the consumer that gives <see cref="SpellCalculation"/> its meaning: the enum
/// selects which of five arithmetics turns the invested cost into damage, healing or effect points.
/// It returns the <b>cost-scaled component only</b> — flat parts and duration effects are applied
/// by the dispatcher (<c>Cast_Spell</c>), which is why two of the six calculations legitimately
/// answer zero here. The spell-info panel calls this too, so what it returns is literally the
/// "Damage" figure the original prints.</para>
/// </summary>
public static class SpellEffectMagnitude {
    /// <summary>
    /// What Mad God's Rage always deals, whatever the data or the calculation says.
    ///
    /// <para><b>This overrides the catalogue.</b> The shipped record carries a Damage of 100 and a
    /// calculation of FixedAmount; the code ignores both and substitutes 1000 after the switch. A
    /// port that trusts the data file is out by a factor of ten.</para>
    /// </summary>
    public const int MadGodsRageMagnitude = 1000;

    /// <summary>
    /// The cost-scaled magnitude of a cast.
    /// </summary>
    /// <param name="spell">The catalogue record, for its calculation and effect amount.</param>
    /// <param name="spellId">
    /// The spell's number. Needed on its own because two rules key off the id rather than any field:
    /// Skyfire's metal-armour condition and Mad God's Rage's flat override.
    /// </param>
    /// <param name="spellCost">The power the caster invested — the value chosen on the slider.</param>
    /// <param name="targetHasMetalGear">
    /// Whether the target satisfies <c>IsUsingMetal</c> @0x639ba: carrying an <b>equipped, intact</b>
    /// item whose type is Armor, Sword <b>or Staff</b>. The staff is not an oversight — the lookup
    /// aliases Sword to Staff, so a staff-carrying target counts as "metal" too, and the engine's
    /// own name for the predicate overstates what it tests. Only consulted for Skyfire.
    /// </param>
    public static int Calculate(Spell spell, int spellId, int spellCost,
        bool targetHasMetalGear = false) {
        if (spell == null) {
            return 0;
        }

        int magnitude = 0;
        switch (spell.Calculation) {
            case SpellCalculation.FixedAmount:
                // Every FixedAmount spell simply yields its effect — except Skyfire, which yields
                // nothing at all unless the target is carrying metal. Skyfire's 40 damage against an
                // unarmoured foe is zero, which is a real tactical rule and not a rounding detail.
                if (spellId != SpellIds.Skyfire || targetHasMetalGear) {
                    magnitude = spell.Damage;
                }
                break;

            case SpellCalculation.CostTimesDamage:
                // The sign of the effect picks the operator: positive multiplies, negative divides.
                magnitude = spell.Damage > 0
                    ? spellCost * spell.Damage
                    : spellCost / Divisor(spell.Damage);
                break;

            case SpellCalculation.CombatGridElement:
                magnitude = spellCost * spell.Damage;
                break;

            case SpellCalculation.NonCostRelated:
            case SpellCalculation.CostTimesDuration:
            case SpellCalculation.Special2:
                // Zero on purpose. CostTimesDuration's magnitude genuinely is duration-based and is
                // computed by Cast_Spell, not here; the original still runs a spell-resistance check
                // on this path but discards the answer, so it cannot change the result.
                break;
        }

        return spellId == SpellIds.MadGodsRage ? MadGodsRageMagnitude : magnitude;
    }

    /// <summary>
    /// The divisor for a negative effect amount, reproducing the original's <c>0x8000</c> guard.
    ///
    /// <para><b>No shipped spell reaches this.</b> All eight CostTimesDamage records carry a
    /// positive effect, so the whole divide branch — guard included — is unreachable with the
    /// game's own data. It is here because a mod could supply a negative amount, and because
    /// leaving it out would misrepresent what the original does.</para>
    ///
    /// <para>An effect of exactly zero divides by zero in the original. That is left to throw
    /// rather than silently rewritten: a spell record that does it is broken, and quietly returning
    /// some invented number would hide it.</para>
    /// </summary>
    private static int Divisor(int effect) =>
        effect == short.MinValue ? short.MaxValue : -effect;
}
