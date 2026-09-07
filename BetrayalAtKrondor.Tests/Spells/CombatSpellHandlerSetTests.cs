namespace BetrayalAtKrondor.Tests.Spells;

using System;
using System.IO;
using System.Linq;
using System.Text;
using global::GameData.Resources.Spells;
using global::ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// Which spells have a per-spell arm, checked against the original's jump table.
/// </summary>
/// <remarks>
/// <c>Cast_Spell</c>'s <c>switch (spellNumber - 3)</c> is forty-two arms wide and mostly empty;
/// sixteen do something. Read straight off the reconstruction (CSPELL.C:1372-1455) the arms are
/// 3, 6, 9, 12, 13, 14, 15, 20, 21, 23, 25, 27, 30, 37, 42 and 44, and
/// <see cref="SpellPerSpellHandlers.HasHandler"/> answers true for exactly those.
///
/// <para><b>Both halves matter.</b> A spell wrongly IN the set gets an effect the original never
/// gives it; a spell wrongly OUT of it silently falls through to the generic path and merely does
/// less. The second is the quiet one — it looks like a spell that works.</para>
///
/// <para><b>The names are asserted against SPELLS.DAT, not trusted.</b> These sixteen constants are
/// the kind that get named from a decompilation and then believed, and a constant whose NUMBER is
/// right while its NAME is wrong is invisible until someone reads the code and reasons about the
/// wrong spell. Pairing each id with the shipped name makes that executable.</para>
/// </remarks>
public class CombatSpellHandlerSetTests {
    static CombatSpellHandlerSetTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>The original's non-empty arms, in id order.</summary>
    private static readonly int[] OriginalArms =
        { 3, 6, 9, 12, 13, 14, 15, 20, 21, 23, 25, 27, 30, 37, 42, 44 };

    private static SpellList? LoadShippedSpells() {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string path = Path.Combine(dir, "OriginalGame", "SPELLS.DAT");
            if (File.Exists(path)) {
                using FileStream stream = File.OpenRead(path);
                return new SpellExtractor().Extract("SPELLS.DAT", stream);
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    [Fact]
    public void ExactlyTheOriginalsArmsHaveAHandler() {
        int[] handled = Enumerable.Range(0, 45)
            .Where(SpellPerSpellHandlers.HasHandler)
            .ToArray();

        Assert.Equal(OriginalArms, handled);
    }

    [Theory]
    [InlineData(SpellIds.DespairThyEyes, "Despair Thy Eyes")]
    [InlineData(SpellIds.HochosHaven, "Hocho's Haven")]
    [InlineData(SpellIds.BaneOfBlackSlayers, "Bane of Black Slayers")]
    [InlineData(SpellIds.Nightfingers, "Nightfingers")]
    // The constant spells out what the shipped name digitises — kept as it is, since renaming the
    // constant to match would read worse and renaming nothing is what keeps this test honest.
    [InlineData(SpellIds.GriefOfAThousandNights, "Grief of 1000 Nights")]
    [InlineData(SpellIds.Mirrorwall, "Mirrorwall")]
    [InlineData(SpellIds.TouchOfLimsKragma, "Touch of Lims-Kragma")]
    [InlineData(SpellIds.UnfortunateFlux, "Unfortunate Flux")]
    [InlineData(SpellIds.MadGodsRage, "Mad God's Rage")]
    [InlineData(SpellIds.SkinOfTheDragon, "Skin of the Dragon")]
    [InlineData(SpellIds.Steelfire, "Steelfire")]
    [InlineData(SpellIds.WindsOfEortis, "Winds of Eortis")]
    [InlineData(SpellIds.Invitation, "Invitation")]
    [InlineData(SpellIds.BlackNimbus, "Black Nimbus")]
    [InlineData(SpellIds.StrengthDrain, "Strength Drain")]
    [InlineData(SpellIds.EvilSeek, "Evil Seek")]
    public void EachHandlerConstantNamesTheSpellItNumbers(int spellId, string shippedName) {
        SpellList? spells = LoadShippedSpells();
        if (spells?.Spells == null) {
            return;
        }

        Assert.True(spells.Spells.TryGetValue(spellId, out Spell? spell), $"no spell {spellId}");
        Assert.Equal(shippedName, spell!.Name);
        Assert.True(SpellPerSpellHandlers.HasHandler(spellId));
    }
}
