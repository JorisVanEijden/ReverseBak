namespace ResourceExtraction.Extractors.Exe;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>One row of a fixed-width table: the key suffix we give it, and the text we expect to
/// find there. A pair rather than two parallel arrays, so a key can never silently drift onto the
/// wrong expected text — there is no second array to forget to edit.</summary>
public sealed class ExeStringTableEntry {
    public ExeStringTableEntry(string name, string text) {
        Name = name;
        Text = text;
    }

    /// <summary>Key suffix — the <c>health</c> in <c>base:uistring:attribute.health</c>.</summary>
    public string Name { get; }

    /// <summary>The exact text this slot must contain. Checked on every extract; a mismatch throws
    /// rather than being written into the catalog (spec §6).</summary>
    public string Text { get; }
}

public sealed class ExeStringTable {
    public string KeyPrefix { get; set; }
    public int Stride { get; set; }
    public ExeStringTableEntry[] Entries { get; set; }

    /// <summary>Derived, never declared: the table is located by its first entry's text, so a
    /// separately-written anchor could only ever disagree with it.</summary>
    public string Anchor => Entries != null && Entries.Length > 0 ? Entries[0].Text : null;

    /// <summary>Derived, never declared — a declared count that disagreed with the entries would be
    /// exactly the silent mismatch this shape exists to prevent.</summary>
    public int Count => Entries?.Length ?? 0;

    /// <summary>The key suffixes, in table order.</summary>
    public string[] Names {
        get {
            var names = new string[Count];
            for (int i = 0; i < names.Length; i++) {
                names[i] = Entries[i].Name;
            }
            return names;
        }
    }
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
    private static ExeStringTableEntry E(string name, string text) => new ExeStringTableEntry(name, text);

    public static IReadOnlyList<ExeStringTable> Tables { get; } = new List<ExeStringTable> {
        // 0x37897 in the IDA database, stride 23, 6 entries.
        new ExeStringTable {
            KeyPrefix = "condition", Stride = 23,
            Entries = new[] {
                E("plagued", "Plagued"), E("poisoned", "Poisoned"), E("drunk", "Drunk"),
                E("healing", "Healing"), E("starving", "Starving"), E("near_death", "Near-death"),
            },
        },
        // 0x37930, stride 15, 16 entries. The index order is the executable's — GetAttributeFromActor
        // (0x42fca) walks the actor record with the same number that indexes this table — and
        // ActorAttribute's first 16 members happen to agree; see GameData's ActorAttributeValues.
        new ExeStringTable {
            KeyPrefix = "attribute", Stride = 15,
            Entries = new[] {
                E("health", "Health"), E("stamina", "Stamina"), E("speed", "Speed"),
                E("strength", "Strength"), E("defense", "Defense"),
                E("accy_crossbow", "Accy: Crossbow"), E("accy_melee", "Accy: Melee"),
                E("accy_casting", "Accy: Casting"), E("assessment", "Assessment"),
                E("armorcraft", "Armorcraft"), E("weaponcraft", "Weaponcraft"),
                E("barding", "Barding"), E("haggling", "Haggling"), E("lockpick", "Lockpick"),
                E("scouting", "Scouting"), E("stealth", "Stealth"),
            },
        },
    };

    public static IReadOnlyList<ExeStringSingle> Singles { get; } = new List<ExeStringSingle> {
        // --- Task 4 Step 3: singletons needed by the Task 7-9 cutover ---
        // The gold/silver wordings exist TWICE, and both copies are real call sites. Occurrence 0
        // (0x3a582/0x3a596/0x3a5a1) is inside FormatMoneyToString (0x42d9a) — the helper
        // MoneyFormatter ports, and the same function the sovereign/royal wordings below come from,
        // so these keys keep the unqualified names. Occurrence 1 (0x3ad3a/0x3ad4e/0x3ad59) is
        // UI_DrawInventory's (0x5674d) own embed, keyed separately below.
        //
        // The second copies were invisible until the extractor started enforcing "found more often
        // than declared" (spec §6): the candidate baseline lists only UI_DrawInventory's, because
        // FormatMoneyToString never calls a text-draw primitive and the classifier is function-level.
        new ExeStringSingle { Key = "base:uistring:money.gold_and_silver", Text = "%ld gold %ld silver", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.silver_only", Text = "%ld silver", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.gold_only", Text = "%ld gold", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.inventory_gold_and_silver", Text = "%ld gold %ld silver", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:money.inventory_silver_only", Text = "%ld silver", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:money.inventory_gold_only", Text = "%ld gold", Occurrence = 1 },
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
        // rather than a bare "item" label. "To:" (UI_teleportation @0x4ee7e) draws between them,
        // the destination counterpart to "From:" — only found once IDA's string-window minlen
        // was lowered to 2 (default 5 hides it, same as "Left" below).
        new ExeStringSingle { Key = "base:uistring:money.teleport_from_label", Text = "From:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.teleport_to_label", Text = "To:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:money.teleport_cost_label", Text = "Cost:", Occurrence = 0 },

        // Meaning not confirmed — three numeric-format snippets sitting between the shop/money
        // block and the item Ratings:/Condition: block below. Best guess is a shop listing's
        // price/discount annotation; named generically rather than guessing the exact wording.
        new ExeStringSingle { Key = "base:uistring:item.count_suffix", Text = " (%d)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.percent_suffix", Text = " (%d%%)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:item.percent_value", Text = "%3d%%", Occurrence = 0 },

        // UI_show_attribute (0x57dec) and UI_show_attribute_x_of_y (0x5800f): the attribute-value
        // readout used elsewhere (e.g. the More Info panel), not part of the item block above or
        // below despite sitting between them in the string pool. "N/A" is the fallback when the
        // actor has no value for the attribute; "of" is the separator in a "<current> of <max>"
        // reading. Both only found once IDA's string-window minlen was lowered to 2.
        new ExeStringSingle { Key = "base:uistring:attribute.value_unavailable", Text = "N/A", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:attribute.current_of_max_separator", Text = "of", Occurrence = 0 },

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
        // EXTRACTED BUT UNCONSUMED, and with an unexplained arity — see also
        // itemstats.bless_type_value_format below. This format has SEVEN %s, while
        // ItemStatsText.ActiveMods appends SIX words (Poisoned, Frosted, Flaming, Steelfired,
        // Enhanced×2). The bless format has FOUR %s against THREE tiers. The same +1 in both places
        // is unlikely to be coincidence: it suggests each port is missing a component the original
        // supplies (a prefix, a separator, or an empty-string sentinel). Neither key is used today —
        // ItemStatsText concatenates with a StringBuilder rather than formatting — so nothing is
        // visibly wrong; the open RE question is backlog task-74. Do not "tidy" these away: if the
        // extra component is real, deleting the declaration deletes the evidence.
        new ExeStringSingle { Key = "base:uistring:itemstats.active_mods_value_format", Text = "%s%s%s%s%s%s%s", Occurrence = 0 },
        // Fallback text when none of the mod flags below are set. Only found once IDA's
        // string-window minlen was lowered to 2.
        new ExeStringSingle { Key = "base:uistring:itemstats.active_mods_none", Text = "None", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_poisoned", Text = "Poisoned ", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_frosted", Text = "Frosted ", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_flaming", Text = "Flaming ", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_steelfired", Text = "Steelfired ", Occurrence = 0 },
        // Faithful oddity (see ItemStatsText.ActiveMods): Enhanced1 (0x800) and Enhanced2 (0x1000)
        // are two separate literal embeds of the same word, one per flag check.
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_enhanced_1", Text = "Enhanced", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.mod_enhanced_2", Text = "Enhanced", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_type_label", Text = "Bless Type:", Occurrence = 0 },
        // Extracted but unconsumed; 4 x %s against ItemStatsText.BlessType's 3 tiers — the same +1
        // as active_mods_value_format above. Backlog task-74.
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_type_value_format", Text = "%s%s%s%s", Occurrence = 0 },
        // Fallback text when none of the blessed-tier flags below are set. Same literal as
        // itemstats.active_mods_none above but a distinct call site, hence occurrence 1. Only
        // found once IDA's string-window minlen was lowered to 2.
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_type_none", Text = "None", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_tier_1", Text = "#1 (+5%)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_tier_2", Text = "#2 (+10%)", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.bless_tier_3", Text = "#3 (+15%)", Occurrence = 0 },
        // Racial line.
        new ExeStringSingle { Key = "base:uistring:itemstats.racial_mod_label", Text = "Racial Mod:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:itemstats.race_tsurani", Text = "Tsurani", Occurrence = 0 },
        // Only found once IDA's string-window minlen was lowered to 2 — "Elf" is 3 characters.
        new ExeStringSingle { Key = "base:uistring:itemstats.race_elf", Text = "Elf", Occurrence = 0 },
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

        // drawFloatingCombatNumber (0x5d6dd): replaces the numeric damage readout with this
        // literal when an attack misses. Sits between the item-inspect block and the world
        // messages below in the string pool despite being combat feedback, not item or world
        // text. Only found once IDA's string-window minlen was lowered to 2.
        new ExeStringSingle { Key = "base:uistring:combat.miss_indicator", Text = "miss", Occurrence = 0 },

        // World interaction messages.
        new ExeStringSingle { Key = "base:uistring:world.object_pushed", Text = "Object will be pushed.", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:world.path_blocked", Text = "Path is blocked!", Occurrence = 0 },

        // A creature/actor info block immediately preceding the "Choose a target" combat panels —
        // short colon-suffixed labels, distinct from the attribute table's bare "Health" etc.
        new ExeStringSingle { Key = "base:uistring:combat.creature_health_label", Text = "Health:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.creature_stamina_label", Text = "Stamina:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.creature_speed_label", Text = "Speed:", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.creature_strength_label", Text = "Strength:", Occurrence = 0 },

        // A SECOND block of the same four labels, at 0x3b283-0x3b29b, embedded by UI_assessOpponent
        // (0x63721) — the Assess panel, a different screen from the creature-info block above
        // (UI_show_healthStaminaSpeedStrength, 0x5e640). Absent from the candidate baseline, and
        // found only once the extractor began enforcing "found more often than declared" (§6):
        // the baseline jumps straight from 0x3b235 to 0x3b50e, so this whole block sits in one of
        // the classifier's blind spots. Nothing consumes these keys yet — the Assess screen is not
        // ported — but they are keyed now so the port has them and a translation covers both screens.
        new ExeStringSingle { Key = "base:uistring:combat.assess_health_label", Text = "Health:", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.assess_stamina_label", Text = "Stamina:", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.assess_speed_label", Text = "Speed:", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.assess_strength_label", Text = "Strength:", Occurrence = 1 },

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
        // (no colon, so not duplicates of the labels above) and a "Left"/"Right" [-hand] column
        // pair, both pushed back-to-back in the same function (sub_ovr168_1C03 @0x60ec3).
        // "Left" was previously believed not to exist anywhere in the exe — that was wrong: IDA's
        // string-window default (minlen 5) was hiding it (4 characters). Found once the minlen
        // was lowered to 2.
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_thrust", Text = "Thrust", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_swing", Text = "Swing", Occurrence = 1 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_damage", Text = "Damage", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_accuracy", Text = "Accuracy", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_left", Text = "Left", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.weapon_table_right", Text = "Right", Occurrence = 0 },

        // Spell casting cost/damage and the health+stamina readout.
        new ExeStringSingle { Key = "base:uistring:combat.spell_cost_format", Text = "Cost: %d Health+Stamina", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.spell_damage_format", Text = "Damage: %d", Occurrence = 0 },
        new ExeStringSingle { Key = "base:uistring:combat.health_stamina_format", Text = "Health/Stamina:  %d of %d", Occurrence = 0 },
    };

    /// <summary>
    /// How many times each declared text may appear in the image: exactly the number of
    /// declarations that name it. Derived rather than hand-written, because a hand-written count is
    /// a place to quietly record "yes I know there are five, four is fine" — which is the failure
    /// spec §6 asks us to make loud. "Found more often than declared" therefore means precisely
    /// "the executable has an occurrence no key claims".
    /// </summary>
    private static Dictionary<string, int> ExpectedOccurrenceCounts() {
        var counts = new Dictionary<string, int>();
        foreach (ExeStringSingle s in Singles) {
            counts.TryGetValue(s.Text, out int n);
            counts[s.Text] = n + 1;
        }
        return counts;
    }

    /// <summary>Resolve every declaration against an executable image. Throws
    /// <see cref="InvalidDataException"/> naming the declaration if anything does not match: a
    /// missing anchor, a table entry whose text is not what was declared, or a text found more
    /// often than the declarations account for (spec §6). There is no partial read — a silently
    /// wrong string is worse than an absent file, because nothing downstream can detect it.</summary>
    public static IDictionary<string, string> Extract(byte[] exe) {
        var result = new Dictionary<string, string>();
        foreach (ExeStringTable t in Tables) {
            IReadOnlyList<string> values = ExeStringReader.ReadTable(exe, t.Anchor, t.Stride, t.Count);
            for (int i = 0; i < t.Entries.Length; i++) {
                ExeStringTableEntry entry = t.Entries[i];
                // The reader only anchors on entry 0; every later slot is reached by arithmetic and
                // could be anything at all if the stride or the build is wrong. Checking each one
                // against its declaration is what turns "wrong build" into a named failure instead
                // of a catalog full of plausible-looking garbage.
                if (!string.Equals(values[i], entry.Text, StringComparison.Ordinal)) {
                    throw new InvalidDataException(
                        $"EXE string table '{t.KeyPrefix}' (anchor '{t.Anchor}', stride {t.Stride}) " +
                        $"index {i} ('{entry.Name}'): declared \"{entry.Text}\", found \"{values[i]}\". " +
                        "The executable is not the expected build, or the declaration is wrong.");
                }
                result[$"base:uistring:{t.KeyPrefix}.{entry.Name}"] = values[i];
            }
        }
        Dictionary<string, int> expected = ExpectedOccurrenceCounts();
        foreach (ExeStringSingle s in Singles) {
            result[s.Key] = ExeStringReader.ReadSingle(exe, s.Text, s.Occurrence, expected[s.Text]);
        }
        return result;
    }
}
