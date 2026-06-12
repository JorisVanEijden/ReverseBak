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
}
