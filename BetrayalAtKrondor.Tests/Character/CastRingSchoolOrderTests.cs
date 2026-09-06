namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using ResourceExtraction.Extractors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

/// <summary>
/// INVSPELL.DAT's group order against the SYMBOL files' order.
/// </summary>
/// <remarks>
/// <b>They are not the same ordering, and the code assumed they were.</b> A cast-screen button's
/// action id resolves to a SYMBOL index (<see cref="CastMenuSelection.SchoolForAction"/>), while its
/// icon and enabled state come from a spellbook GROUP. Using one index for both gave the last two
/// buttons each other's icon and each other's enabled state — see
/// <see cref="CastRingSchools.SchoolForGroup"/>.
///
/// <para>This derives the pairing from the shipped files instead of trusting the constant, so a data
/// change that reorders either side fails here rather than silently mislabelling two buttons.</para>
/// </remarks>
public class CastRingSchoolOrderTests {
    static CastRingSchoolOrderTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static string FindOriginalGameDir() {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame");
            if (File.Exists(Path.Combine(candidate, "INVSPELL.DAT"))) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null; // shipped data absent (CI)
    }

    [Fact]
    public void EachSpellbookGroupPairsWithTheSymbolFileSchoolForGroupNames() {
        string gameDir = FindOriginalGameDir();
        if (gameDir == null) {
            return;
        }

        SpellBookPage page;
        using (FileStream stream = File.OpenRead(Path.Combine(gameDir, "INVSPELL.DAT"))) {
            page = new SpellBookPageExtractor().Extract("INVSPELL.DAT", stream);
        }

        // The spell ids each SYMBOL file's ring positions name, as a set per school.
        var symbolSets = new List<HashSet<int>>();
        for (var school = 0; school < page.Groups.Count; school++) {
            string path = Path.Combine(gameDir, $"SYMBOL{school + 1}.DAT");
            using FileStream stream = File.OpenRead(path);
            SpellSymbolLayout layout = new SpellSymbolExtractor().Extract(Path.GetFileName(path), stream);
            symbolSets.Add(layout.Nodes.Select(n => n.SpellId).ToHashSet());
        }

        for (var group = 0; group < page.Groups.Count; group++) {
            var groupSet = page.Groups[group].Spells.Select(s => s.SpellId).ToHashSet();
            int school = CastRingSchools.SchoolForGroup(group);

            Assert.True(groupSet.SetEquals(symbolSets[school]),
                $"group {group} should pair with SYMBOL{school + 1}. "
                + $"group holds [{string.Join(",", groupSet.OrderBy(i => i))}], "
                + $"that file holds [{string.Join(",", symbolSets[school].OrderBy(i => i))}]");
        }
    }

    [Fact]
    public void TheMappingIsItsOwnInverse_AndOnlyTheLastTwoMove() {
        // Four of the six are the identity; asserting that as well as the swap is what makes a
        // future "just add one" edit fail loudly instead of shifting every button by a place.
        Assert.Equal(0, CastRingSchools.SchoolForGroup(0));
        Assert.Equal(1, CastRingSchools.SchoolForGroup(1));
        Assert.Equal(2, CastRingSchools.SchoolForGroup(2));
        Assert.Equal(3, CastRingSchools.SchoolForGroup(3));
        Assert.Equal(5, CastRingSchools.SchoolForGroup(4));
        Assert.Equal(4, CastRingSchools.SchoolForGroup(5));

        for (var i = 0; i < CastRingLayout.CategoryCount; i++) {
            Assert.Equal(i, CastRingSchools.SchoolForGroup(CastRingSchools.SchoolForGroup(i)));
        }
    }
}
