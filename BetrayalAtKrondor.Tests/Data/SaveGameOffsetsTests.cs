namespace BetrayalAtKrondor.Tests.Data;

using System;
using System.IO;
using System.Text;

using GameData.Resources.Data;

using ResourceExtraction;
using ResourceExtraction.Extractors;

using Xunit;

public class SaveGameOffsetsTests {
    static SaveGameOffsetsTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // A bare body (no 100-byte header) so SaveGameExtractor parses it as TEMP.GAM.
    private static byte[] MakeBody() => new byte[SaveGameOffsets.BodySize];

    [Fact]
    public void BodySize_Matches_TempGam() {
        Assert.Equal(334505, SaveGameOffsets.BodySize);
    }

    [Fact]
    public void StateData_ScalarOffsets_MatchTheReader() {
        byte[] body = MakeBody();
        BitConverter.GetBytes((short)7).CopyTo(body, SaveGameOffsets.Chapter);
        BitConverter.GetBytes(1234).CopyTo(body, SaveGameOffsets.PartyGold);
        BitConverter.GetBytes(555000).CopyTo(body, SaveGameOffsets.GameTime);
        body[SaveGameOffsets.CurrentZone] = 9;
        body[SaveGameOffsets.WorldX] = 40;
        body[SaveGameOffsets.WorldY] = 41;
        BitConverter.GetBytes(11).CopyTo(body, SaveGameOffsets.PositionX);
        BitConverter.GetBytes(22).CopyTo(body, SaveGameOffsets.PositionY);
        BitConverter.GetBytes(33).CopyTo(body, SaveGameOffsets.PositionZ);
        BitConverter.GetBytes((short)512).CopyTo(body, SaveGameOffsets.Rotation);

        using var stream = new MemoryStream(body);
        SaveGame save = new SaveGameExtractor().Extract("test", stream);
        SaveGameStateData s = save.Data!.StateData;

        Assert.Equal((short)7, s.ChapterNumber);
        Assert.Equal(1234, s.PartyGold);
        Assert.Equal(555000, s.GameTimeIn2Seconds);
        Assert.Equal((byte)9, s.CurrentZoneNumber);
        Assert.Equal((byte)40, s.WorldXCoordinate);
        Assert.Equal((byte)41, s.WorldYCoordinate);
        Assert.Equal(11, s.PositionX);
        Assert.Equal(22, s.PositionY);
        Assert.Equal(33, s.PositionZ);
        Assert.Equal((short)512, s.CurrentZRotation);
    }
}
