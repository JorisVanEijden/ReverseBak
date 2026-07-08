namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.GameState;
using GameData.Resources.World;

using ResourceExtraction.Extractors;

using System;
using System.IO;
using System.Text;

using Xunit;

public class TileEventExtractorTests {
    static TileEventExtractorTests() {
        // TileEventExtractor opens BinaryReader with Encoding.GetEncoding(437) (DOS CP437).
        // On non-Windows .NET the codepage requires the CodePages provider.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
    // 19-byte trigger record matching TileEventExtractor's read order.
    private static byte[] Trigger(ushort type, uint entry, ushort requiredKey, ushort forbiddenKey, ushort setOnFireKey) {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        bw.Write(type);
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0); // StartX/EndY/EndX/StartY
        bw.Write(entry);          // EntryNumber (u32)
        bw.Write((byte)0);        // fireOnce
        bw.Write(requiredKey);
        bw.Write(forbiddenKey);
        bw.Write(setOnFireKey);
        bw.Write((ushort)0);      // Field11
        return ms.ToArray();      // 19 bytes
    }

    // A 1920-byte file whose chapter-1 block holds one trigger.
    private static byte[] FileWithOneTrigger(byte[] trigger) {
        var file = new byte[1920];
        var block = new MemoryStream();
        var bw = new BinaryWriter(block);
        bw.Write((ushort)1);
        bw.Write(trigger);
        Array.Copy(block.ToArray(), 0, file, 0, (int)block.Length);
        return file;
    }

    [Fact]
    public void TriggerKeysDecodeToConditionsAndEffect() {
        byte[] file = FileWithOneTrigger(Trigger((ushort)TileEventType.Dial, 0, 6489, 142, 7447));

        TileEventTile tile = new TileEventExtractor().Extract("T010203.DAT", new MemoryStream(file));
        TileEventTrigger trigger = tile.Chapters[0].Triggers[0];

        var requires = Assert.IsType<FlagCondition>(trigger.Requires);
        Assert.Equal(6489, requires.Flag);
        Assert.True(requires.Set);
        var forbids = Assert.IsType<FlagCondition>(trigger.Forbids);
        Assert.Equal(142, forbids.Flag);
        var onFire = Assert.IsType<SetFlagEffect>(trigger.OnFire);
        Assert.Equal(7447, onFire.Flag);
        Assert.True(onFire.Set);
    }

    [Fact]
    public void ZeroKeysDecodeToNull() {
        byte[] file = FileWithOneTrigger(Trigger((ushort)TileEventType.Dial, 0, 0, 0, 0));

        TileEventTile tile = new TileEventExtractor().Extract("T010203.DAT", new MemoryStream(file));
        TileEventTrigger trigger = tile.Chapters[0].Triggers[0];

        Assert.Null(trigger.Requires);
        Assert.Null(trigger.Forbids);
        Assert.Null(trigger.OnFire);
    }
}
