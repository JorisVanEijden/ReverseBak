namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Dialog.Actions;
using GameData.Resources.GameState;

using ResourceExtraction.Extractors.Dialog;

using System.IO;

using Xunit;

public class DialogActionFactoryTests {
    private static BinaryReader Reader(params byte[] bytes) => new(new MemoryStream(bytes));

    [Fact]
    public void SetGlobalValue_DirectVarWrite_DecodesToSetVarEffect() {
        // key=30016 (LE), mask=0, data=0, unused=0, value=2 (LE)
        var reader = Reader(0x40, 0x75, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00);
        var action = Assert.IsType<GlobalEffectAction>(DialogActionFactory.Build(4, reader));
        var v = Assert.IsType<SetVarEffect>(action.Effect);
        Assert.Equal(16, v.Var);
        Assert.Equal(2, v.Value);
    }

    [Fact]
    public void SetTemporaryFlag_DecodesToTimedSetFlag() {
        // globalKey=7042 (uint32 LE), duration=600 (uint32 LE)
        var reader = Reader(0x82, 0x1B, 0x00, 0x00, 0x58, 0x02, 0x00, 0x00);
        var action = Assert.IsType<GlobalEffectAction>(DialogActionFactory.Build(14, reader));
        var f = Assert.IsType<SetFlagEffect>(action.Effect);
        Assert.Equal(7042, f.Flag);
        Assert.True(f.Set);
        Assert.Equal(600u, f.ForTicks);
    }

    [Fact]
    public void SetTimer_SetFlagType_DecodesOnExpiryToSetFlag() {
        // Type=SetFlag(3), Flag=Reset(0x80), Key=8127 (LE), Time=600 (u32 LE)
        var reader = Reader(0x03, 0x80, 0xBF, 0x1F, 0x58, 0x02, 0x00, 0x00);
        var action = Assert.IsType<SetTimerAction>(DialogActionFactory.Build(22, reader));
        var f = Assert.IsType<SetFlagEffect>(action.OnExpiry);
        Assert.Equal(8127, f.Flag);
        Assert.True(f.Set);
        Assert.Null(action.TimerTarget);
    }

    [Fact]
    public void SetTimer_LightType_KeepsRawTarget() {
        // Type=Light(1), Flag=0, Key=5, Time=100
        var reader = Reader(0x01, 0x00, 0x05, 0x00, 0x64, 0x00, 0x00, 0x00);
        var action = Assert.IsType<SetTimerAction>(DialogActionFactory.Build(22, reader));
        Assert.Null(action.OnExpiry);
        Assert.Equal(5, action.TimerTarget);
    }

    [Fact]
    public void SetReturnValue_KeepsTheSIGN_because0xFFFFIsMinusOne() {
        // *** 84 of the corpus's 97 SetReturnValue actions are negative, and reading the word
        // unsigned disabled every one. *** `nResult` is a 16-bit signed int: the armourer's refusal
        // answers -1 and WCURSOR.C:355 cancels the whole click on it, while GdsSceneRules.OutcomeFor
        // maps -1, -2, -3, -4 and -5 onto scene actions. As 65535 it matched nothing anywhere.
        var refusal = Reader(0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
        Assert.Equal(-1, Assert.IsType<SetReturnValueAction>(
            DialogActionFactory.Build(21, refusal)).Value);

        var minusFour = Reader(0xFC, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
        Assert.Equal(-4, Assert.IsType<SetReturnValueAction>(
            DialogActionFactory.Build(21, minusFour)).Value);

        // Positives are untouched: 1 is the commonest non-negative answer in shipped data.
        var one = Reader(0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
        Assert.Equal(1, Assert.IsType<SetReturnValueAction>(
            DialogActionFactory.Build(21, one)).Value);
    }
}
