namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.Config;
using ResourceExtraction.Extractors;
using Xunit;

public class MovementExtractorTests {
    // The actual shipping MOVEMENT.DAT (18 bytes, 9 × u16 LE):
    //   step  : 0190 0320 0640 = 400  800  1600
    //   turn1 : 0400 0800 1000 = 1024 2048 4096
    //   turn2 : 0001 0002 0004 = 1    2    4
    private static readonly byte[] ShippingBytes = [
        0x90, 0x01, 0x20, 0x03, 0x40, 0x06, // step distances
        0x00, 0x04, 0x00, 0x08, 0x00, 0x10, // turn angles
        0x01, 0x00, 0x02, 0x00, 0x04, 0x00, // raw per-step time units
    ];

    [Fact]
    public void Extract_ParsesShippingLayout() {
        using var stream = new MemoryStream(ShippingBytes);

        MovementData data = new MovementExtractor().Extract("MOVEMENT.DAT", stream);

        Assert.Equal(new[] { 400, 800, 1600 }, data.StepDistances);
        Assert.Equal(new[] { 1024, 2048, 4096 }, data.TurnAngles);
        // raw 1/2/4 game-time units → 60/120/240 real seconds per step (× 60).
        Assert.Equal(new[] { 60, 120, 240 }, data.SecondsPerStep);
    }

    [Fact]
    public void Extract_TypedAccessorsIndexByPreference() {
        using var stream = new MemoryStream(ShippingBytes);

        MovementData data = new MovementExtractor().Extract("MOVEMENT.DAT", stream);

        // Step distance and per-step time are both keyed by the step-size preset.
        Assert.Equal(1600, data.StepDistanceFor(StepSize.Large));
        Assert.Equal(240, data.SecondsPerStepFor(StepSize.Large));
        // Turn angle is keyed by the turn-size preset.
        Assert.Equal(1024, data.TurnAngleFor(TurnSize.Small));
        Assert.Equal(4096, data.TurnAngleFor(TurnSize.Large));
    }

    [Fact]
    public void TurnAngles_DivideFullRevolutionEvenly() {
        using var stream = new MemoryStream(ShippingBytes);

        MovementData data = new MovementExtractor().Extract("MOVEMENT.DAT", stream);

        // A full revolution is the 16-bit angle range 0x10000; every shipped
        // turn increment divides it evenly (64 / 32 / 16 steps per circle).
        foreach (int angle in data.TurnAngles) {
            Assert.Equal(0, 0x10000 % angle);
        }
    }
}
