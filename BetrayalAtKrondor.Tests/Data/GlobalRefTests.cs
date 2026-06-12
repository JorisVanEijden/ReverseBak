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
        Effect e = GlobalRef.DecodeEffect(30016, mask: 0, data: 0, value: 2);
        var v = Assert.IsType<SetVarEffect>(e);
        Assert.Equal(16, v.Var);
        Assert.Equal(2, v.Value);
    }

    [Fact]
    public void DirectWriteToStoryFlag_DecodesToSetFlag() {
        var f = Assert.IsType<SetFlagEffect>(GlobalRef.DecodeEffect(8127, 0, 0, 1));
        Assert.Equal(8127, f.Flag);
        Assert.True(f.Set);
        Assert.Null(f.ForTicks);
    }

    [Fact]
    public void MaskedWrite_ExpandsToFlagList() {
        // key 56010 -> group 1; mask 0b0000_0101 sets bits 0 and 2; data gives their values.
        Effect e = GlobalRef.DecodeEffect(56010, mask: 0b0000_0101, data: 0b0000_0100, value: 0);
        var set = Assert.IsType<SetFlagsEffect>(e);
        Assert.Equal(2, set.Flags.Count);
        // bit 0 -> key 56000+10+0+1 = 56011, data bit 0 = 0 -> clear
        Assert.Contains(set.Flags, f => f.Flag == 56011 && !f.Set);
        // bit 2 -> key 56000+10+2+1 = 56013, data bit 2 = 1 -> set
        Assert.Contains(set.Flags, f => f.Flag == 56013 && f.Set);
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
