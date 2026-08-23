namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.GameState;

using ResourceExtraction.Extractors.GameState;

using Xunit;

public class GlobalRefTests {
    [Fact]
    public void StoryFlagInRange_DecodesToFlagSet() {
        Condition c = GlobalRef.DecodeCondition(456, 1, null);
        var flag = Assert.IsType<FlagCondition>(c);
        Assert.Equal(456, flag.Flag);
        Assert.True(flag.Set);
    }

    [Fact]
    public void StoryFlagClear_DecodesToFlagClear() {
        Condition c = GlobalRef.DecodeCondition(456, 0, 0);
        var flag = Assert.IsType<FlagCondition>(c);
        Assert.False(flag.Set);
    }

    [Fact]
    public void ItemCountRange_DecodesToHasItem() {
        Condition c = GlobalRef.DecodeCondition(50137, 3, null);
        var item = Assert.IsType<HasItemCondition>(c);
        Assert.Equal(137, item.Item);
        Assert.Equal(3, item.AtLeast);
        Assert.Null(item.AtMost);
    }

    [Fact]
    public void NoteRange_DecodesToHasNote() {
        var note = Assert.IsType<HasNoteCondition>(GlobalRef.DecodeCondition(51021, 1, null));
        Assert.Equal(21, note.Note);
    }

    [Fact]
    public void SpellTimerRange_DecodesToSpellTimerActive() {
        var t = Assert.IsType<SpellTimerActiveCondition>(GlobalRef.DecodeCondition(52003, 1, null));
        Assert.Equal(3, t.Timer);
    }

    [Fact]
    public void RandomRange_DecodesToRandom() {
        var r = Assert.IsType<RandomCondition>(GlobalRef.DecodeCondition(53050, 25, null));
        Assert.Equal(50, r.Bound);
        Assert.Equal(25, r.Min);
    }

    [Fact]
    public void NamedVarRange_DecodesToVar() {
        var v = Assert.IsType<VarCondition>(GlobalRef.DecodeCondition(30016, 1, 2));
        Assert.Equal(16, v.Var);
        Assert.Equal(1, v.Min);
        Assert.Equal(2, v.Max);
    }

    [Fact]
    public void PartyRange_DecodesToParty() {
        var p = Assert.IsType<PartyCondition>(GlobalRef.DecodeCondition(40005, 1, null));
        Assert.Equal(5, p.Check);
    }

    [Fact]
    public void Flags2SingleBit_DecodesToFlagWithCanonicalKey() {
        // group 1, bit 2 -> key 56000 + 1*10 + (2+1) = 56013
        var flag = Assert.IsType<FlagCondition>(GlobalRef.DecodeCondition(56013, 1, null));
        Assert.Equal(56013, flag.Flag);
        Assert.True(flag.Set);
    }

    [Fact]
    public void UnknownDivByTenGroupKey_FallsBackToRaw() {
        // 56010 is divisible by 10 but carries no mask here -> raw, never a guess
        Assert.IsType<RawGlobalCondition>(GlobalRef.DecodeCondition(56010, 1, null));
    }

    [Fact]
    public void DirectWriteToNamedVar_DecodesToSetVar() {
        Effect e = GlobalRef.DecodeEffect(30016, andMask: 0xFF, orMask: 0, xorMask: 0, value: 2);
        var v = Assert.IsType<SetVarEffect>(e);
        Assert.Equal(16, v.Var);
        Assert.Equal(2, v.Value);
    }

    [Fact]
    public void DirectWriteToStoryFlag_DecodesToSetFlag() {
        var f = Assert.IsType<SetFlagEffect>(GlobalRef.DecodeEffect(8127, 0xFF, 0, 0, 1));
        Assert.Equal(8127, f.Flag);
        Assert.True(f.Set);
        Assert.Null(f.ForTicks);
    }

    [Fact]
    public void BitGroupWrite_TouchesOnlyTheBitsTheMasksForce() {
        // The setter runs `group &= and; group |= or; group ^= xor`. With and = 0b0000_0101 the six
        // ZERO bits are cleared and bits 0 and 2 are PRESERVED; or then sets bit 2.
        Effect e = GlobalRef.DecodeEffect(56010, andMask: 0b0000_0101, orMask: 0b0000_0100,
            xorMask: 0, value: 0);
        var set = Assert.IsType<SetFlagsEffect>(e);

        // bit 2 -> 56000 + 10 + 2 + 1 = 56013, forced set by the OR.
        Assert.Contains(set.Flags, f => f.Flag == 56013 && f.Set);
        // bit 0 is preserved by the AND and untouched by the OR, so it must produce NO row.
        // Reading the first byte as "which bits are written" would emit 56011 -> clear here.
        Assert.DoesNotContain(set.Flags, f => f.Flag == 56011);
        // The six bits the AND zeroes are the ones actually cleared.
        foreach (int bit in new[] { 1, 3, 4, 5, 6, 7 }) {
            int flag = 56010 + bit + 1;
            Assert.Contains(set.Flags, f => f.Flag == flag && !f.Set);
        }
        Assert.Equal(7, set.Flags.Count);
    }

    [Fact]
    public void TheShippedPureSet_TouchesExactlyOneFlag() {
        // 117 of the 125 shipped bit-group writes look like this: and = 0xFF (a no-op) and a single
        // OR bit. The old reading emitted EIGHT rows for these — one set and seven spurious clears,
        // which would have wiped the rest of the group every time a dialog set one flag.
        Effect e = GlobalRef.DecodeEffect(56040, andMask: 0xFF, orMask: 0x04, xorMask: 0, value: 0);
        var set = Assert.IsType<SetFlagsEffect>(e);

        Assert.Single(set.Flags);
        Assert.Equal(56043, set.Flags[0].Flag);
        Assert.True(set.Flags[0].Set);
    }

    [Fact]
    public void TheShippedPureClear_ClearsTheBitTheAndMaskDrops() {
        // The case that proves the old reading backwards: and = 0xDF, or = 0x00 (shipped, key 56370).
        // It clears bit 5 and nothing else; the old code emitted the SEVEN other bits as clears and
        // left bit 5 alone.
        Effect e = GlobalRef.DecodeEffect(56370, andMask: 0xDF, orMask: 0x00, xorMask: 0, value: 0);
        var set = Assert.IsType<SetFlagsEffect>(e);

        Assert.Single(set.Flags);
        Assert.Equal(56376, set.Flags[0].Flag);   // 56370 + 5 + 1
        Assert.False(set.Flags[0].Set);
    }

    [Fact]
    public void AClearAndASetInOneOp_BothLand() {
        // Shipped at key 56280: and = 0xDF clears bit 5, or = 0x10 sets bit 4. One op, two flags,
        // and the other six untouched.
        Effect e = GlobalRef.DecodeEffect(56280, andMask: 0xDF, orMask: 0x10, xorMask: 0, value: 0);
        var set = Assert.IsType<SetFlagsEffect>(e);

        Assert.Equal(2, set.Flags.Count);
        Assert.Contains(set.Flags, f => f.Flag == 56286 && !f.Set);  // bit 5 cleared
        Assert.Contains(set.Flags, f => f.Flag == 56285 && f.Set);   // bit 4 set
    }

    [Fact]
    public void OrWinsOverAnd_BecauseItRunsSecond() {
        // A bit dropped by the AND and raised by the OR ends up SET — the order is not arbitrary.
        Effect e = GlobalRef.DecodeEffect(56000, andMask: 0x00, orMask: 0x01, xorMask: 0, value: 0);
        var set = Assert.IsType<SetFlagsEffect>(e);

        Assert.Contains(set.Flags, f => f.Flag == 56001 && f.Set);
    }

    [Fact]
    public void TemporaryFlag_DecodesToSetFlagWithTicks() {
        var f = Assert.IsType<SetFlagEffect>(GlobalRef.DecodeTemporaryEffect(7042, durationTicks: 600));
        Assert.Equal(7042, f.Flag);
        Assert.True(f.Set);
        Assert.Equal(600u, f.ForTicks);
    }

    [Fact]
    public void UnconfirmedRangeKey_FallsBackToRaw() {
        // Keys in 8500..29999 are unconfirmed namespace -> faithful RawGlobalCondition, never a guess.
        var raw = Assert.IsType<RawGlobalCondition>(GlobalRef.DecodeCondition(12000, 1, null));
        Assert.Equal(12000, raw.Key);
        Assert.Equal(1, raw.Min);
        Assert.Null(raw.Max);
    }
}
