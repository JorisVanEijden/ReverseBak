namespace GameData.Resources.Spells;

using GameData;

/// <summary>
/// The per-spell arm of IDA <c>Cast_Spell</c> — <c>switch (spellNumber - 3)</c> @0x687a5, forty-two
/// cases of which <b>only sixteen do anything</b>.
///
/// <para>This is the layer where a spell stops being data and becomes itself. Nothing here is
/// derivable from <c>SPELLS.DAT</c>: a record says how to turn cost into a number, and then this
/// switch decides whether that number is used, thrown away, replaced by a die roll, or handed to a
/// routine of the spell's own. Four spells discard the magnitude the calculation just produced, one
/// works only against a single creature, one only against creatures of the right disposition, and
/// one succeeds on a percentage the record never mentions.</para>
/// </summary>
public static class SpellPerSpellHandlers {
    /// <summary>
    /// Whether this spell has a handler at all.
    /// </summary>
    /// <remarks>
    /// Twenty-nine of the forty-five spells fall through to the shared tail untouched. The switch is
    /// large but mostly empty, which is why reading the jump table rather than the case list is the
    /// only honest way to know who is special.
    /// </remarks>
    public static bool HasHandler(int spellId) {
        switch (spellId) {
            case SpellIds.DespairThyEyes:
            case SpellIds.HochosHaven:
            case SpellIds.BaneOfBlackSlayers:
            case SpellIds.Nightfingers:
            case SpellIds.GriefOfAThousandNights:
            case SpellIds.Mirrorwall:
            case SpellIds.TouchOfLimsKragma:
            case SpellIds.UnfortunateFlux:
            case SpellIds.MadGodsRage:
            case SpellIds.SkinOfTheDragon:
            case SpellIds.Steelfire:
            case SpellIds.WindsOfEortis:
            case SpellIds.Invitation:
            case SpellIds.BlackNimbus:
            case SpellIds.StrengthDrain:
            case SpellIds.EvilSeek:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Spells whose handler is <b>nothing but a sound</b>.
    /// </summary>
    /// <remarks>
    /// Their entire mechanical effect comes from the calculation switch — all three are
    /// CostTimesDuration spells that registered a lingering effect before ever reaching here. Worth
    /// knowing before someone goes looking for the code that makes Skin of the Dragon work: there
    /// isn't any, beyond the duration arithmetic and a sound cue.
    /// </remarks>
    public static bool HandlerIsSoundOnly(int spellId) =>
        spellId == SpellIds.HochosHaven
        || spellId == SpellIds.UnfortunateFlux
        || spellId == SpellIds.SkinOfTheDragon;

    /// <summary>
    /// Spells whose handler <b>zeroes the magnitude the calculation just produced</b>.
    /// </summary>
    /// <remarks>
    /// Strength Drain and Evil Seek unconditionally, Bane of Black Slayers unless its target
    /// qualifies. Together with the two that zero themselves after the animation
    /// (<see cref="SpellCastTail.ZeroesItsOwnMagnitude"/>), <b>five spells compute a number the
    /// original never delivers</b> — and every one of them carries a calculation and a damage field
    /// that say otherwise.
    /// </remarks>
    public static bool ZeroesMagnitude(int spellId, bool targetIsBlackSlayer = false) {
        if (spellId == SpellIds.StrengthDrain || spellId == SpellIds.EvilSeek) {
            return true;
        }

        return spellId == SpellIds.BaneOfBlackSlayers && !targetIsBlackSlayer;
    }

    /// <summary>
    /// <b>Bane of Black Slayers does nothing to anything else.</b>
    /// </summary>
    /// <remarks>
    /// The handler calls <c>IsBlackSlayer</c> on the target and, if it says no, jumps to the same
    /// magnitude-zeroing tail Strength Drain uses. Its record's damage of 5 against a cost of 10-15
    /// is therefore 50-75 against exactly one creature and nothing at all against everything else —
    /// a restriction with no field to express it.
    /// </remarks>
    public static bool BaneAppliesTo(bool targetIsBlackSlayer) => targetIsBlackSlayer;

    /// <summary>
    /// The strength Strength Drain takes: <b>the cost divided by the record's damage field</b>.
    /// </summary>
    /// <param name="spellCost">The effective cost.</param>
    /// <param name="damage">The record's damage field — <c>-1</c> for the shipped spell.</param>
    /// <remarks>
    /// The shipped record carries a damage of -1, so the negation makes the divisor 1 and the drain
    /// is exactly the cost invested — 10 to 20 points of Strength. The spell is Special2, which is
    /// one of the calculations <see cref="SpellEffectMagnitude"/> answers zero for; the real
    /// arithmetic is here, and it is a <i>division</i>, so a mod raising the damage field weakens
    /// the spell rather than strengthening it.
    ///
    /// <para>The sign guard is the same one the other two divisions use, with the same shape and the
    /// same blind spot: a damage of exactly zero takes the non-negative branch and divides by it.
    /// This port answers 0 rather than faulting.</para>
    /// </remarks>
    public static int StrengthDrained(int spellCost, int damage) {
        int divisor = damage >= 0
            ? damage
            : (damage == SpellEffectApplication.MostNegativeDuration
                ? SpellEffectApplication.OverflowGuard
                : -damage);

        return divisor == 0 ? 0 : spellCost / divisor;
    }

    /// <summary>
    /// Strength Drain's projectile <b>flies out and comes back</b>.
    /// </summary>
    /// <remarks>
    /// Its handler runs the projectile sweep twice: once from the caster to the target, and then —
    /// from whatever actor the first sweep actually struck, which need not be the intended target —
    /// back to the caster, with that actor flinching in between. So the visual encodes the transfer,
    /// and the return leg's origin is decided by the outbound hit rather than by the targeting.
    /// </remarks>
    public static bool DrainProjectileReturnsToCaster => true;

    /// <summary>The amount Despair Thy Eyes subtracts from each accuracy.</summary>
    public const int DespairAccuracyPenalty = -20;

    /// <summary>The three attributes Despair Thy Eyes attacks.</summary>
    public static readonly ActorAttribute[] DespairAttributes = {
        ActorAttribute.AccuracyCrossbow,
        ActorAttribute.AccuracyMelee,
        ActorAttribute.AccuracyCasting,
    };

    /// <summary>
    /// <b>Despair Thy Eyes hits party members and monsters through different machinery.</b>
    /// </summary>
    /// <param name="targetActorNumber">0 for a monster; 1-6 for a member of the party.</param>
    /// <remarks>
    /// A monster takes the penalty through the ordinary attribute-change call — a straight, permanent
    /// -20 on all three accuracies. A named character instead gets three <i>timed</i> modifiers
    /// registered in their eight-slot modifier table. Same spell, same twenty points, two different
    /// durability rules, chosen by whether the victim has a slot table at all.
    ///
    /// <para>The two paths also differ in scale: the permanent call is passed -0x1400 while the
    /// timed one is passed -20, because the permanent path works in the 8.8 fixed point
    /// <c>StatEngine.Modify</c> already models. Copying one number into the other path is out by a
    /// factor of 256.</para>
    /// </remarks>
    public static bool DespairIsPermanentFor(int targetActorNumber) => targetActorNumber == 0;

    /// <summary>
    /// Whether a timed attribute modifier is accepted into the victim's slot table.
    /// </summary>
    /// <param name="occupiedFlags">The <c>ActorAttributeFlag</c> mask of each occupied slot.</param>
    /// <param name="occupiedKinds">Each occupied slot's kind word, parallel to the masks.</param>
    /// <param name="attribute">The attribute this modifier targets.</param>
    /// <remarks>
    /// <b>A modifier is refused if a modifier of a <i>different kind</i> already holds the same
    /// attribute.</b> The scan compares the slot's kind against this routine's own (0x100) and only
    /// rejects when they differ, so spell modifiers of the same kind coexist while something else
    /// holding that attribute blocks the spell entirely — silently, with no feedback to the caster.
    /// </remarks>
    public static bool TimedModifierAccepted(int[] occupiedKinds, ActorAttributeFlag[] occupiedFlags,
        ActorAttribute attribute) {
        if (occupiedKinds == null || occupiedFlags == null) {
            return true;
        }

        var wanted = (ActorAttributeFlag)(1 << (int)attribute);
        int slots = occupiedKinds.Length < occupiedFlags.Length
            ? occupiedKinds.Length
            : occupiedFlags.Length;

        for (int i = 0; i < slots; i++) {
            if (occupiedKinds[i] != 0 && occupiedFlags[i] == wanted
                && occupiedKinds[i] != TimedModifierKind) {
                return false;
            }
        }

        return true;
    }

    /// <summary>The kind word a spell-cast timed attribute modifier carries.</summary>
    public const int TimedModifierKind = 0x100;

    /// <summary>
    /// The timed modifier's second timestamp is the current game time <b>doubled</b>.
    /// </summary>
    /// <remarks>
    /// Recorded as read rather than modelled: the record's first field is the current game time and
    /// the second is that value shifted left once, which as an expiry would make the modifier last
    /// exactly as long as the game has been running. Either the field is not an expiry or this is a
    /// genuine quirk; nothing in <c>Cast_Spell</c> settles it, and guessing would put a wrong
    /// duration on a real debuff.
    /// </remarks>
    public static bool ModifierExpiryIsGameTimeDoubled => true;

    /// <summary>
    /// <b>Grief of 1000 Nights works only on creatures of the right kind.</b>
    /// </summary>
    /// <param name="creatureType">The target's creature type.</param>
    /// <remarks>
    /// The gate is a thirty-one case switch over creature types 28 upward, answering no for a
    /// specific twelve and yes for everything else — including every creature numbered below 28,
    /// which falls out of range and takes the default. So the exemption list is short and explicit
    /// while the eligibility is the fallback, which is the opposite of how it reads.
    /// </remarks>
    public static bool GriefAffects(int creatureType) {
        switch (creatureType) {
            case 28:
            case 39:
            case 41:
            case 42:
            case 43:
            case 44:
            case 46:
            case 49:
            case 54:
            case 56:
            case 57:
            case 58:
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// Black Nimbus's success chance, as a percentage: <b>ten plus seven per point of cost</b>.
    /// </summary>
    /// <remarks>
    /// Written in the original as <c>cost * 700 / 100 + 10</c>, rolled against a d100. The spell's
    /// cost range is 1 to 10, so it runs from a 17% chance to an 80% one and <b>is never certain</b>
    /// — the only spell in the cast path whose whole effect is a die roll, and nothing in its record
    /// hints at it: it carries a NonCostRelated calculation, no damage and no duration.
    /// </remarks>
    public static int BlackNimbusChancePercent(int spellCost) => (spellCost * 700 / 100) + 10;

    /// <summary>Whether a Black Nimbus cast lands, given the d100 roll.</summary>
    public static bool BlackNimbusSucceeds(int rollUnder100, int spellCost) =>
        rollUnder100 < BlackNimbusChancePercent(spellCost);

    /// <summary>
    /// Two shipped spells use the negative-duration divide.
    /// </summary>
    /// <remarks>
    /// Skin of the Dragon (-1) and Thoughts Like Clouds (-2), both CostTimesDuration. So
    /// <see cref="SpellEffectApplication.NegativeDurationDivides"/> is not a defensive branch kept
    /// for mods — it is live game behaviour, halving Thoughts Like Clouds' effect against the cost
    /// while Skin of the Dragon's divisor of 1 leaves it equal to the cost.
    /// </remarks>
    public static bool NegativeDurationIsUsedByShippedSpells => true;
}
