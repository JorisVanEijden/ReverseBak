namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BetrayalAtKrondor.Tests.Content;
using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The monster caster's candidate set, measured over the SHIPPED spell table rather than asserted
/// from the filter's own definition.
/// </summary>
/// <remarks>
/// <b>Written from what the ORIGINAL was observed to cast.</b> In DEF_COMB entry 306 (zone 10, the
/// only chapter-1 fight that fields a caster) the creature-30 Rogue Mage put an effect on a party
/// member, and the live effect pool recorded it as spell <b>3, "Despair Thy Eyes"</b>, with an
/// invested cost of 2 — matching that spell's fixed 2..2 cost. So the port's selector must be able
/// to reach spell 3; a filter that quietly excluded it would diverge from the game in a way no
/// port-side test could see.
///
/// <para>Skip-if-absent, like the rest of the corpus tests.</para>
/// </remarks>
public class MonsterRepertoireCorpusTests {
    private static IReadOnlyDictionary<int, (bool IsMartial, int TargetingType, int MinCost)>? Catalogue() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "spells.json"));
        if (gen == null) {
            return null;
        }
        var table = new Dictionary<int, (bool, int, int)>();
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "spells.json")));
        foreach (JsonProperty p in doc.RootElement.GetProperty("Spells").EnumerateObject()) {
            table[int.Parse(p.Name)] = (
                p.Value.GetProperty("IsMartial").GetBoolean(),
                p.Value.GetProperty("TargetingType").GetInt32(),
                p.Value.GetProperty("MinimumCost").GetInt32());
        }
        return table;
    }

    [Fact]
    public void TheSpellTheOriginalsRogueMageCastIsInTheCandidateSet() {
        IReadOnlyDictionary<int, (bool IsMartial, int TargetingType, int MinCost)>? spells = Catalogue();
        if (spells == null) {
            return;
        }

        (bool martial, int targeting, int minCost) = spells[SpellIds.DespairThyEyes];
        Assert.True(MonsterSpellcasting.InMonsterRepertoire(SpellIds.DespairThyEyes, martial, targeting),
            "the original delivered Despair Thy Eyes from a monster caster, so the port must admit it");
        // The pool recorded investedCost 2 against this spell; the table's floor agrees.
        Assert.Equal(2, minCost);
    }

    [Fact]
    public void TheCandidateSetIsNeitherEmptyNorEverything() {
        IReadOnlyDictionary<int, (bool IsMartial, int TargetingType, int MinCost)>? spells = Catalogue();
        if (spells == null) {
            return;
        }

        var candidates = new List<int>();
        foreach (KeyValuePair<int, (bool IsMartial, int TargetingType, int MinCost)> s in spells) {
            if (MonsterSpellcasting.InMonsterRepertoire(s.Key, s.Value.IsMartial, s.Value.TargetingType)) {
                candidates.Add(s.Key);
            }
        }

        // A filter that admitted everything, or nothing, would pass a definition-only test and fail
        // the game. Both bounds are measured against the shipped 45-spell table.
        Assert.InRange(candidates.Count, 1, spells.Count - 1);
        Assert.Contains(SpellIds.DespairThyEyes, candidates);
        Assert.DoesNotContain(SpellIds.Invitation, candidates);
        Assert.DoesNotContain(SpellIds.ThoughtsLikeClouds, candidates);
    }
}
