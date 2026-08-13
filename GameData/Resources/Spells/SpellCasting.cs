namespace GameData.Resources.Spells;

using GameData.Resources.GameState;
using GameData.Resources.Inventory;

/// <summary>
/// Who may cast, what they may cast, how much power they may put behind it, and what that costs.
///
/// <para>Ported from <c>gstate_actor_is_caster</c> (GSTATE.C), <c>cspell_check_castable</c> and
/// <c>cspell_select_power</c> (CSPELL.C), and <c>spellfx_party_member_take_damage</c> (SPELLFX.C).
/// The interactive ring, target picking and the effect itself are not here — this is only the rule
/// layer those screens sit on.</para>
/// </summary>
public static class SpellCasting {
    /// <summary>
    /// The object id of the chapter-8 power source, and the flag that marks a usable one.
    ///
    /// <para>In chapter 8 this item is a <b>battery</b>: it caps the power a caster may select and
    /// is drained by the amount spent. Without one, <see cref="IsCastable"/> refuses everything.
    /// </para>
    /// </summary>
    public const int PowerSourceObjectId = 1;

    /// <summary>The flag a chapter-8 power source must carry to count.</summary>
    public const int PowerSourceReadyFlag = 0x40;

    /// <summary>The chapter whose special power-source rules apply.</summary>
    public const int PowerSourceChapter = 8;

    /// <summary>
    /// The zone kind (<c>g_game_mode</c>, the first field of <c>Z##DEF.DAT</c>) that has no sky.
    ///
    /// <para>Do not read this as "in combat" — it is a property of the <i>zone</i>, not the
    /// encounter. The spell data is what makes that unambiguous: Candle Glow is castable only here
    /// and nowhere else, while Skyfire and Stardusk are castable everywhere <i>but</i> here.</para>
    /// </summary>
    public const int EnclosedZoneKind = 2;

    /// <summary>The summoning spell kind, which needs a free actor slot.</summary>
    private const int SummonKind = 6;

    /// <summary>Combat actor slots; at this many, nothing more can be summoned.</summary>
    private const int CombatActorSlots = 7;

    /// <summary>Stardusk is refused outdoors from this hour...</summary>
    private const int StarduskBlockedFromHour = 8;

    /// <summary>...up to (but not including) this one.</summary>
    private const int StarduskBlockedUntilHour = 0x11;

    /// <summary>
    /// Whether a character is a spellcaster at all — <c>gstate_actor_is_caster</c>.
    ///
    /// <para>It reads the casting skill's <b>maximum</b>, not its current value, so a caster who is
    /// drained to zero is still a caster. This is the gate the character sheet and the inventory
    /// screen use to decide whether to offer a spellbook page.</para>
    /// </summary>
    public static bool IsCaster(int castingSkillMaximum) => castingSkillMaximum != 0;

    /// <summary>
    /// The most power a caster can put behind a spell, before the spell's own ceiling applies.
    ///
    /// <para>This is the combined health-and-stamina pool, clamped in chapter 8 to the charge left
    /// in the power source. <b>The two callers disagree about the flag</b> and that disagreement is
    /// reproduced rather than smoothed over: <c>cspell_check_castable</c> requires
    /// <see cref="PowerSourceReadyFlag"/> and refuses outright when no flagged source is carried,
    /// while <c>cspell_select_power</c> takes the first matching object regardless of flags and
    /// applies no clamp at all when none is carried. It only shows when an actor holds an unflagged
    /// power source, which the castability gate normally prevents.</para>
    /// </summary>
    /// <param name="requireReadyFlag">
    /// True for the castability rule, false for the power slider — see above.
    /// </param>
    /// <returns>False only in the castability case with no flagged power source: nothing is castable.</returns>
    public static bool TryGetPowerBudget(SpellCastContext context, bool requireReadyFlag,
        out int budget) {
        budget = context == null ? 0 : context.HealthStaminaPool;
        if (context == null || context.Chapter != PowerSourceChapter) {
            return context != null;
        }

        if (!requireReadyFlag
            && InventoryQuery.CountByKind(context.Inventory, PowerSourceObjectId) == 0) {
            // The slider leaves the budget alone when nothing is carried; only the castability
            // rule treats that as disqualifying.
            return true;
        }

        RuntimeItem source = FindPowerSource(context, requireReadyFlag);
        if (source == null) {
            return !requireReadyFlag;
        }
        if (budget > source.Variable) {
            budget = source.Variable;
        }
        return true;
    }

    /// <summary>
    /// Whether a specific spell can be cast right now — <c>cspell_check_castable</c>.
    ///
    /// <para>Order matters here: the knowledge check applies unconditionally, while everything else
    /// is skipped when <paramref name="knowledgeOnly"/> is set. That is how one function answers
    /// both "is this in the character's spellbook" (for rendering the book) and "can it be cast at
    /// this moment" (for the ring).</para>
    /// </summary>
    /// <param name="knowledgeOnly">
    /// True to ask only whether the spell is known, skipping cost, components, zone and timing.
    /// </param>
    public static bool IsCastable(int spellId, Spell spell, SpellCastContext context,
        bool knowledgeOnly = false) {
        if (spell == null || context == null) {
            return false;
        }

        var castable = true;
        if (!knowledgeOnly) {
            if (!TryGetPowerBudget(context, requireReadyFlag: true, out int budget)) {
                return false;
            }

            // Strictly greater: a caster whose pool exactly equals the base cost cannot cast, which
            // is what stops any spell from taking the last point.
            if (spell.MinimumCost >= budget) {
                castable = false;
            }

            if (spell.ObjectId != -1
                && InventoryQuery.CountByKind(context.Inventory, spell.ObjectId) == 0) {
                castable = false;
            }

            if (context.ZoneKind == EnclosedZoneKind) {
                if (spellId == SpellIds.Skyfire || spellId == SpellIds.Stardusk
                    || spellId == SpellIds.MadGodsRage) {
                    return false;
                }
            } else {
                if (spellId == SpellIds.CandleGlow) {
                    return false;
                }
                if (spellId == SpellIds.Stardusk) {
                    int hour = GameClock.HourOfDay(context.GameTimeIn2Seconds);
                    if (hour >= StarduskBlockedFromHour && hour < StarduskBlockedUntilHour) {
                        return false;
                    }
                }
            }

            if ((spell.TargetingType == SummonKind || spellId == SpellIds.DannonsDelusions)
                && context.CombatActorCount == CombatActorSlots) {
                castable = false;
            }
        }

        return castable && SpellBook.IsKnown(context.KnownSpells, spellId);
    }

    /// <summary>
    /// The range of power the caster may select — <c>cspell_select_power</c>.
    ///
    /// <para>The spell's own <see cref="Spell.MaximumCost"/> is lowered to one below the caster's
    /// budget, so the selectable band narrows as a caster tires. When it collapses onto
    /// <see cref="Spell.MinimumCost"/> there is nothing to choose and the original skips the slider
    /// entirely, casting at the base cost — <see cref="PowerRange.IsFixed"/>.</para>
    /// </summary>
    public static PowerRange GetPowerRange(Spell spell, SpellCastContext context) {
        if (spell == null || !TryGetPowerBudget(context, requireReadyFlag: false, out int budget)) {
            return default;
        }

        int maximum = spell.MaximumCost;
        if (maximum >= budget) {
            maximum = budget - 1;
        }
        return new PowerRange(spell.MinimumCost, maximum);
    }

    /// <summary>
    /// Spends a cast — <c>spellfx_party_member_take_damage</c>.
    ///
    /// <para>The power selected <b>is</b> the cost: it comes off the combined health-and-stamina
    /// pool as damage, health first. In chapter 8 the power source is drained by the same amount
    /// and floors at zero rather than going negative.</para>
    /// </summary>
    /// <returns>The pool delta actually applied, in whole points.</returns>
    public static int ApplyCost(SpellCastContext context, int cost,
        Character.ActorStat health, Character.ActorStat stamina, out bool collapsed) {
        collapsed = false;
        if (context == null || cost <= 0) {
            return 0;
        }

        int applied = Character.StatEngine.ModifyHealthPool(health, stamina,
            -(long)cost << 8, 100, out collapsed);

        if (context.Chapter == PowerSourceChapter) {
            RuntimeItem source = FindPowerSource(context, requireReadyFlag: true);
            if (source != null) {
                source.Variable = source.Variable < cost ? (byte)0 : (byte)(source.Variable - cost);
            }
        }
        return applied;
    }

    private static RuntimeItem FindPowerSource(SpellCastContext context, bool requireReadyFlag) {
        if (context.Inventory == null) {
            return null;
        }
        foreach (RuntimeItem item in context.Inventory.Items) {
            if (item.ObjectId != PowerSourceObjectId) {
                continue;
            }
            if (requireReadyFlag && (item.ItemFlags & PowerSourceReadyFlag) == 0) {
                continue;
            }
            return item;
        }
        return null;
    }
}

/// <summary>The band of power a caster may select, and whether there is any choice in it.</summary>
public readonly struct PowerRange {
    public PowerRange(int minimum, int maximum) {
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>Lowest selectable power — always the spell's base cost.</summary>
    public int Minimum { get; }

    /// <summary>Highest selectable power, after the caster's budget has been applied.</summary>
    public int Maximum { get; }

    /// <summary>
    /// No choice to offer: the band has collapsed onto the base cost, so the original casts at
    /// <see cref="Minimum"/> without showing the slider.
    /// </summary>
    public bool IsFixed => Maximum == Minimum;

    /// <summary>
    /// <b>The budget cannot even cover the base cost.</b> The original has no branch for this — it
    /// would open a slider whose range runs backwards and sit there until cancelled — so callers
    /// must check it rather than relying on the range being sane.
    /// </summary>
    public bool IsEmpty => Maximum < Minimum;
}

/// <summary>Everything the casting rules read about the caster and the world around them.</summary>
public sealed class SpellCastContext {
    /// <summary>Current chapter; only <see cref="SpellCasting.PowerSourceChapter"/> is special.</summary>
    public int Chapter { get; set; }

    /// <summary>The zone's kind — <c>g_game_mode</c>, not a combat flag. See
    /// <see cref="SpellCasting.EnclosedZoneKind"/>.</summary>
    public int ZoneKind { get; set; }

    /// <summary>Game time in two-second units, for the Stardusk daylight rule.</summary>
    public int GameTimeIn2Seconds { get; set; }

    /// <summary>The caster's spellbook mask.</summary>
    public ushort[] KnownSpells { get; set; }

    /// <summary>The caster's pack — spell components and the chapter-8 power source.</summary>
    public RuntimeContainer Inventory { get; set; }

    /// <summary>Combined health and stamina, the pool spell cost is paid from.</summary>
    public int HealthStaminaPool { get; set; }

    /// <summary>Actors on the combat grid (<c>g_combat_count_A</c>), which gates summoning.</summary>
    public int CombatActorCount { get; set; }
}
