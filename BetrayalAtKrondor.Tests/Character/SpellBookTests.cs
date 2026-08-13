namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The known-spell bitmask (combat_actor_bitmap_set_bit). Three 16-bit words indexed by
/// <c>spellId / 16</c>, which is the same test the spellbook page uses to decide what to print.
/// </summary>
public class SpellBookTests {
    [Fact]
    public void AFreshSpellbookKnowsNothing() {
        ushort[] words = SpellBook.Empty();

        Assert.Equal(SpellBook.Words, words.Length);
        Assert.Equal(0, SpellBook.Count(words));
        Assert.False(SpellBook.IsKnown(words, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]   // last bit of word 0
    [InlineData(16)]   // first bit of word 1
    [InlineData(31)]
    [InlineData(32)]   // first bit of word 2
    [InlineData(47)]   // the very last slot
    public void ASpellCanBeLearnedAnywhereInTheThreeWords(int spellId) {
        ushort[] words = SpellBook.Empty();

        Assert.True(SpellBook.Learn(words, spellId));
        Assert.True(SpellBook.IsKnown(words, spellId));
        Assert.Equal(1, SpellBook.Count(words));
    }

    [Fact]
    public void LearningLandsInTheRightWordRatherThanSmearingAcrossThem() {
        ushort[] words = SpellBook.Empty();

        SpellBook.Learn(words, 16);

        Assert.Equal(0, words[0]);
        Assert.Equal(1, words[1]);
        Assert.Equal(0, words[2]);
    }

    [Fact]
    public void LearningReportsTrueOnlyTheFirstTime() {
        // The original's return value, and the reason a re-read does not consume the scroll.
        ushort[] words = SpellBook.Empty();

        Assert.True(SpellBook.Learn(words, 9));
        Assert.False(SpellBook.Learn(words, 9));
        Assert.Equal(1, SpellBook.Count(words));
    }

    [Fact]
    public void LearningOneSpellDoesNotDisturbAnother() {
        ushort[] words = SpellBook.Empty();
        SpellBook.Learn(words, 3);
        SpellBook.Learn(words, 4);

        Assert.True(SpellBook.IsKnown(words, 3));
        Assert.True(SpellBook.IsKnown(words, 4));
        Assert.False(SpellBook.IsKnown(words, 5));
        Assert.Equal(2, SpellBook.Count(words));
    }

    [Fact]
    public void AnIdOutsideTheMaskIsRefusedRatherThanWrapping() {
        // 48 would otherwise fold onto word 3 (off the end) or, with a sloppy modulo, onto spell 0.
        ushort[] words = SpellBook.Empty();

        Assert.False(SpellBook.Learn(words, SpellBook.MaxSpellId + 1));
        Assert.False(SpellBook.IsKnown(words, SpellBook.MaxSpellId + 1));
        Assert.Equal(0, SpellBook.Count(words));
    }

    [Fact]
    public void ANegativeIdIsRefusedToo() {
        ushort[] words = SpellBook.Empty();

        Assert.False(SpellBook.Learn(words, -1));
        Assert.Equal(0, SpellBook.Count(words));
    }

    [Fact]
    public void ForgettingIsTheInverseOfLearning() {
        ushort[] words = SpellBook.Empty();
        SpellBook.Learn(words, 12);
        SpellBook.Learn(words, 13);

        SpellBook.Forget(words, 12);

        Assert.False(SpellBook.IsKnown(words, 12));
        Assert.True(SpellBook.IsKnown(words, 13));
    }
}
