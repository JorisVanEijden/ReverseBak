namespace ResourceExtraction.Extractors.Exe;

using System.Collections.Generic;

public sealed class ExeStringTable {
    public string KeyPrefix { get; set; }
    public string Anchor { get; set; }
    public int Stride { get; set; }
    public int Count { get; set; }
    public string[] Names { get; set; }
}

public sealed class ExeStringSingle {
    public string Key { get; set; }
    public string Text { get; set; }
    public int Occurrence { get; set; }
}

/// <summary>
/// Which strings we take out of KRONDOR.EXE, and what to call them. This is RE knowledge, so it
/// lives in the extractor and nowhere else. Declarations are content-anchored — see ExeStringReader
/// for why no address appears here.
/// </summary>
public static class ExeStringManifest {
    /// <summary>Fixed-width tables. These are indexed arithmetically in the original, so they have
    /// ZERO data xrefs and no analysis can discover them — declaring them is the only way.</summary>
    public static IReadOnlyList<ExeStringTable> Tables { get; } = new List<ExeStringTable> {
        // 0x37897 in the IDA database, stride 23, 6 entries.
        new ExeStringTable {
            KeyPrefix = "condition", Anchor = "Plagued", Stride = 23, Count = 6,
            Names = new[] { "plagued", "poisoned", "drunk", "healing", "starving", "near_death" },
        },
        // 0x37930, stride 15, 16 entries. Order matches ActorAttribute's first 16 members.
        new ExeStringTable {
            KeyPrefix = "attribute", Anchor = "Health", Stride = 15, Count = 16,
            Names = new[] {
                "health", "stamina", "speed", "strength", "defense",
                "accy_crossbow", "accy_melee", "accy_casting", "assessment", "armorcraft",
                "weaponcraft", "barding", "haggling", "lockpick", "scouting", "stealth",
            },
        },
    };

    public static IReadOnlyList<ExeStringSingle> Singles { get; } = new List<ExeStringSingle> {
        // --- Task 4 Step 3: singletons needed by the Task 7-9 cutover ---
        new ExeStringSingle { Key = "base:uistring:money.gold_and_silver", Text = "%ld gold %ld silver", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.silver_only", Text = "%ld silver", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.gold_only", Text = "%ld gold", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.sovereigns_and_royal", Text = "%ld sovereigns and %ld royal", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.sovereign_and_royal", Text = "%ld sovereign and %ld royal", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.royal_only", Text = "%ld royal", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.abbreviated", Text = "%lds %ldr", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.teleport_cost", Text = "%d sovereigns", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.party_gold_label", Text = "Party Gold:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:npc.shopkeeper", Text = "shopkeeper", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:npc.tavernkeeper", Text = "tavernkeeper", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.unavailable", Text = "Unavailable", Occurrence = 0 },

        // --- Task 4 Step 4: the remaining docs/re-notes/exe-display-strings.md candidates ---
        // Listed in the baseline's address order, which is also file order, so occurrence values
        // for a repeated text below can be read top-to-bottom.

        new ExeStringSingle { Key = "base:uistring:boot.loading", Text = "Loading Betrayal at Krondor... please wait.", Occurrence = 0 },

        // Meaning not confirmed against any known caller — no Unity port or doc references this
        // yet. Name is a best-effort description of the format's shape (a %Fs bracketed by
        // dashes), not a claim about where it is drawn.
        new ExeStringSingle { Key = "base:uistring:item.bracketed_name_format", Text = "- %Fs -", Occurrence = 0 },

        // "From:"/"Cost:" sit between the teleport-cost singleton (money.teleport_cost, "%d
        // sovereigns") and the money wordings in the baseline, so grouped with the teleport UI
        // rather than a bare "item" label.
        new ExeStringSingle { Key = "base:uistring:money.teleport_from_label", Text = "From:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.teleport_cost_label", Text = "Cost:", Occurrence = 0 },

        // Meaning not confirmed — three numeric-format snippets sitting between the shop/money
        // block and the item Ratings:/Condition: block below. Best guess is a shop listing's
        // price/discount annotation; named generically rather than guessing the exact wording.
        new ExeStringSingle { Key = "base:uistring:item.count_suffix", Text = " (%d)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.percent_suffix", Text = " (%d%%)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.percent_value", Text = "%3d%%", Occurrence = 0 },

        // The item quality/appraisal block: a "Ratings:" header, a "Condition:" label, a
        // "<descriptor> (<pct>%)" value format, and "Normal" as one such descriptor. Distinct from
        // ItemInspectText's plain "Condition: %d%%" (item.condition below) — different call site,
        // different literal.
        new ExeStringSingle { Key = "base:uistring:item.ratings_header", Text = "Ratings:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.condition_label", Text = "Condition:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.condition_descriptor_format", Text = "%Fs (%d%)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.condition_normal", Text = "Normal", Occurrence = 0 },

        // The "More Info" stat panel (ItemStatsText.cs, UI_showItemStats @0x5A1DA).
        // Melee (Sword/Staff): Thrust|Swing two-column block.
        new ExeStringSingle { Key = "base:uistring:itemstats.thrust_label", Text = "Thrust", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.swing_label", Text = "Swing", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.base_damage_melee_label", Text = "Base Dmg:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.thrust_damage_value", Text = "%d+Strength", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.swing_damage_value", Text = "%d+Strength", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:itemstats.accuracy_melee_label", Text = "Accuracy:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.thrust_accuracy_value", Text = "%d+Skill", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.swing_accuracy_value", Text = "%d+Skill", Occurrence = 1 },
        // Ranged (Crossbow, or a quarrel item): single-column block. Each row names the partner
        // item it combines with ("Quarrel" for a crossbow's row, "CrossBow" for a quarrel's row);
        // the damage row and accuracy row each embed their own copy of the partner-name literals
        // (two call sites per name, not a melee/ranged split — this section is ranged-only).
        new ExeStringSingle { Key = "base:uistring:itemstats.base_damage_ranged_label", Text = "Base Damage:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.ranged_damage_value", Text = "%d+%s", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.ranged_damage_partner_quarrel", Text = "Quarrel", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.ranged_damage_partner_crossbow", Text = "CrossBow", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.accuracy_ranged_label", Text = "Accuracy:", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:itemstats.ranged_accuracy_value", Text = "%d+%s+Skill", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.ranged_accuracy_partner_quarrel", Text = "Quarrel", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:itemstats.ranged_accuracy_partner_crossbow", Text = "CrossBow", Occurrence = 1 },
        // Armor.
        new ExeStringSingle { Key = "base:uistring:itemstats.armor_mod_label", Text = "Armor Mod:", Occurrence = 0 },
        // Enchantment block (swords and armor only).
        new ExeStringSingle { Key = "base:uistring:itemstats.active_mods_label", Text = "Active Mods:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.resistances_label", Text = "Resistances:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.active_mods_value_format", Text = "%s%s%s%s%s%s%s", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_poisoned", Text = "Poisoned ", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_frosted", Text = "Frosted ", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_flaming", Text = "Flaming ", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_steelfired", Text = "Steelfired ", Occurrence = 0 },
        // Faithful oddity (see ItemStatsText.ActiveMods): Enhanced1 (0x800) and Enhanced2 (0x1000)
        // are two separate literal embeds of the same word, one per flag check.
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_enhanced_1", Text = "Enhanced", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_enhanced_2", Text = "Enhanced", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_type_label", Text = "Bless Type:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_type_value_format", Text = "%s%s%s%s", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_tier_1", Text = "#1 (+5%)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_tier_2", Text = "#2 (+10%)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_tier_3", Text = "#3 (+15%)", Occurrence = 0 },
        // Racial line.
        new ExeStringSingle { Key = "base:uistring:itemstats.racial_mod_label", Text = "Racial Mod:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.race_tsurani", Text = "Tsurani", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.race_dwarf", Text = "Dwarf", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.race_human", Text = "Human", Occurrence = 0 },
        // Affects-player-statistics line.
        new ExeStringSingle { Key = "base:uistring:itemstats.affects_stats_format", Text = "%s player statistics", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.affects_stats_affecting", Text = "Affecting", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.affects_stats_can_affect", Text = "Can affect", Occurrence = 0 },

        // The item-inspect view (ItemInspectText.cs, UI_showItem @0x5A778). TypeLine's four
        // mutually-exclusive variants, then StatusLine's words.
        new ExeStringSingle { Key = "base:uistring:item.amount", Text = "Amount: %d", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.uses_left", Text = "Uses left: %d", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.value_rating", Text = "Value Rating: %d%", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.condition", Text = "Condition: %d%%", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.using", Text = "Using", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.broken", Text = "Broken", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.repairable", Text = "Repairable", Occurrence = 0 },

        // World interaction messages.
        new ExeStringSingle { Key = "base:uistring:world.object_pushed", Text = "Object will be pushed.", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:world.path_blocked", Text = "Path is blocked!", Occurrence = 0 },

        // A creature/actor info block immediately preceding the "Choose a target" combat panels —
        // short colon-suffixed labels, distinct from the attribute table's bare "Health" etc.
        new ExeStringSingle { Key = "base:uistring:combat.creature_health_label", Text = "Health:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.creature_stamina_label", Text = "Stamina:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.creature_speed_label", Text = "Speed:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.creature_strength_label", Text = "Strength:", Occurrence = 0 },

        // Two back-to-back "Choose a target" / "Accuracy:" / "Damage:" blocks — a melee weapon
        // panel followed by a ranged (crossbow) one, the latter immediately followed by
        // "quarrels remaining" (ammo count only makes sense for the ranged weapon).
        new ExeStringSingle { Key = "base:uistring:combat.choose_target_melee", Text = "Choose a target", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.accuracy_label_melee", Text = "Accuracy:", Occurrence = 2 },
        new ExeStringSingle { Key = "base:uistring:combat.damage_label_melee", Text = "Damage:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.choose_target_ranged", Text = "Choose a target", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.accuracy_label_ranged", Text = "Accuracy:", Occurrence = 3 },
        new ExeStringSingle { Key = "base:uistring:combat.damage_label_ranged", Text = "Damage:", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.quarrels_remaining", Text = "quarrels remaining", Occurrence = 0 },

        // A weapon comparison table: Thrust/Swing repeat as column headers here (second call
        // site — distinct from the stat-panel labels above), plus bare "Damage"/"Accuracy"
        // (no colon, so not duplicates of the labels above) and a "Right" [-hand] column. No
        // "Left" string exists anywhere in the exe, so this reads as a single weapon-slot table
        // rather than a Left/Right pair.
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_thrust", Text = "Thrust", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_swing", Text = "Swing", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_damage", Text = "Damage", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_accuracy", Text = "Accuracy", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_right", Text = "Right", Occurrence = 0 },

        // Spell casting cost/damage and the health+stamina readout.
        new ExeStringSingle { Key = "base:uistring:combat.spell_cost_format", Text = "Cost: %d Health+Stamina", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.spell_damage_format", Text = "Damage: %d", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.health_stamina_format", Text = "Health/Stamina:  %d of %d", Occurrence = 0 },
    };

    /// <summary>Resolve every declaration against an executable image. Throws (via ExeStringReader)
    /// naming the declaration if anything does not match.</summary>
    public static IDictionary<string, string> Extract(byte[] exe) {
        var result = new Dictionary<string, string>();
        foreach (ExeStringTable t in Tables) {
            IReadOnlyList<string> values = ExeStringReader.ReadTable(exe, t.Anchor, t.Stride, t.Count);
            for (int i = 0; i < t.Names.Length; i++) {
                result[$"base:uistring:{t.KeyPrefix}.{t.Names[i]}"] = values[i];
            }
        }
        foreach (ExeStringSingle s in Singles) {
            result[s.Key] = ExeStringReader.ReadSingle(exe, s.Text, s.Occurrence);
        }
        return result;
    }
}
