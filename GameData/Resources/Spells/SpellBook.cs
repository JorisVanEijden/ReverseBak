namespace GameData.Resources.Spells;

using System;

/// <summary>
/// A character's known spells, held as the original's three 16-bit words.
///
/// <para>Ported from <c>combat_actor_bitmap_set_bit</c> (<c>SRC/COMBAT/ACTOR/CACTOR.C</c>), which
/// addresses them as <c>record + 2 + (spellId / 16) * 2</c> — the same three words
/// <c>SaveGameActorData</c> reads at offsets 2, 4 and 6, and the same test the spellbook page uses
/// to decide which entries to print.</para>
/// </summary>
public static class SpellBook {
    /// <summary>Words in the mask; 3 × 16 = 48 slots, against a catalogue of 45 spells.</summary>
    public const int Words = 3;

    /// <summary>Bits per word.</summary>
    public const int BitsPerWord = 16;

    /// <summary>Highest spell id the mask can hold.</summary>
    public const int MaxSpellId = (Words * BitsPerWord) - 1;

    /// <summary>A fresh, empty spellbook.</summary>
    public static ushort[] Empty() => new ushort[Words];

    /// <summary>Whether a character knows a spell.</summary>
    public static bool IsKnown(ushort[] words, int spellId) {
        if (words == null || spellId < 0 || spellId > MaxSpellId) {
            return false;
        }
        int word = spellId / BitsPerWord;
        return word < words.Length && (words[word] & Mask(spellId)) != 0;
    }

    /// <summary>
    /// Teaches a spell.
    /// </summary>
    /// <returns>
    /// <b>True only when the spell was not already known.</b> The original returns exactly this,
    /// and the item-use dispatch feeds it straight into the outcome — so reading a scroll you have
    /// already learned reports "no effect" and, because the tail only spends the item on a
    /// <i>successful</i> outcome, <b>the scroll is not consumed</b>. Inverting this would quietly
    /// eat scrolls for nothing.
    /// <para>(canassa calls the local <c>already_set</c>, which reads as the opposite of what it
    /// holds — it is set when the bit was <i>not</i> previously present.)</para>
    /// </returns>
    public static bool Learn(ushort[] words, int spellId) {
        if (words == null) {
            throw new ArgumentNullException(nameof(words));
        }
        if (spellId < 0 || spellId > MaxSpellId) {
            return false;
        }
        int word = spellId / BitsPerWord;
        if (word >= words.Length) {
            return false;
        }
        ushort mask = Mask(spellId);
        bool newlyLearned = (words[word] & mask) == 0;
        words[word] |= mask;
        return newlyLearned;
    }

    /// <summary>Forgets a spell. Not something the original does, but the inverse belongs with the
    /// mask rather than being open-coded by whatever needs it.</summary>
    public static void Forget(ushort[] words, int spellId) {
        if (words == null || spellId < 0 || spellId > MaxSpellId) {
            return;
        }
        int word = spellId / BitsPerWord;
        if (word < words.Length) {
            words[word] &= unchecked((ushort)~Mask(spellId));
        }
    }

    /// <summary>
    /// Gives both books the union of what either one holds.
    /// </summary>
    /// <returns>How many spells each of them gained; zero when they already matched.</returns>
    /// <remarks>
    /// <b>Both sides end up with the same book — it is a merge, not a copy.</b> EVTCOND.C case 16
    /// ORs the two masks word by word and writes the result back to BOTH, so neither magician is
    /// the source and neither loses anything they already knew.
    ///
    /// <para>The original hardcodes which two characters (Owyn and Pug); that is the caller's to
    /// know. What belongs here is that a book is <see cref="Words"/> words wide and that merging
    /// them is a bitwise OR, so nothing outside this type has to open-code the layout.</para>
    /// </remarks>
    public static int Share(ushort[] a, ushort[] b) {
        if (a == null || b == null) {
            return 0;
        }

        int before = Count(a) + Count(b);
        int words = Math.Min(Math.Min(a.Length, b.Length), Words);
        for (var i = 0; i < words; i++) {
            var union = (ushort)(a[i] | b[i]);
            a[i] = union;
            b[i] = union;
        }

        return Count(a) + Count(b) - before;
    }

    /// <summary>How many spells are known.</summary>
    public static int Count(ushort[] words) {
        if (words == null) {
            return 0;
        }
        var n = 0;
        foreach (ushort word in words) {
            ushort bits = word;
            while (bits != 0) {
                n += bits & 1;
                bits >>= 1;
            }
        }
        return n;
    }

    private static ushort Mask(int spellId) => (ushort)(1 << (spellId % BitsPerWord));
}
