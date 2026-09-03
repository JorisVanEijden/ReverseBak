namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The two magicians pooling what they know — <c>SpellBook.Share</c>, behind sub-action 16.
/// </summary>
public class SpellBookShareTests {
    private static ushort[] Book(params int[] spells) {
        ushort[] words = SpellBook.Empty();
        foreach (int spell in spells) {
            SpellBook.Learn(words, spell);
        }
        return words;
    }

    [Fact]
    public void BOTHEndUpWithTheUnionRatherThanOneOverwritingTheOther() {
        // It is a merge, not a copy: neither magician is the source, and neither loses a spell.
        ushort[] owyn = Book(0, 5, 20);
        ushort[] pug = Book(5, 33, 44);

        SpellBook.Share(owyn, pug);

        foreach (int spell in new[] { 0, 5, 20, 33, 44 }) {
            Assert.True(SpellBook.IsKnown(owyn, spell), $"Owyn lost or missed {spell}");
            Assert.True(SpellBook.IsKnown(pug, spell), $"Pug lost or missed {spell}");
        }
    }

    [Fact]
    public void ItReachesEveryWordOfTheMaskNotJustTheFirst() {
        // Spell 44 lives in word 2; a merge that only ORed word 0 would pass the naive test above
        // by accident if both books happened to be low-numbered.
        ushort[] owyn = Book(1);
        ushort[] pug = Book(44);

        SpellBook.Share(owyn, pug);

        Assert.True(SpellBook.IsKnown(owyn, 44));
        Assert.True(SpellBook.IsKnown(pug, 1));
    }

    [Fact]
    public void TheCountIsWhatThePAIRGainedBetweenThem() {
        // Two books of three sharing one spell: each gains the two it lacked, so four in total.
        Assert.Equal(4, SpellBook.Share(Book(0, 5, 20), Book(5, 33, 44)));
    }

    [Fact]
    public void TwoIdenticalBooksGainNothing() {
        Assert.Equal(0, SpellBook.Share(Book(3, 9), Book(9, 3)));
    }

    [Fact]
    public void ANullBookIsLeftAloneRatherThanThrowing() {
        // One of the two may have no record at all in a save that has not met him yet.
        ushort[] owyn = Book(7);

        Assert.Equal(0, SpellBook.Share(owyn, null));
        Assert.Equal(0, SpellBook.Share(null, owyn));
        Assert.True(SpellBook.IsKnown(owyn, 7));
    }
}
