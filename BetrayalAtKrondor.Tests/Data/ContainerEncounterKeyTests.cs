namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using Xunit;

/// <summary>
/// The encounter record's global keys, which are UNSIGNED.
/// </summary>
/// <remarks>
/// <c>GetGlobalValue</c> @0x42250 compares the key with <c>jb</c>/<c>jnb</c> — unsigned branches —
/// and its top band is the 56000+ range backed by <c>global_flags2[]</c>. A signed 16-bit read
/// turns a key in that band into a negative number that matches no band at all.
/// </remarks>
public class ContainerEncounterKeyTests {
    private static SaveGameContainerEncounterData Encounter(int key1, int key2) =>
        new SaveGameContainerEncounterData(key1, key2, 0, 0, 0, 0, 0);

    [Fact]
    public void AKeyAboveShortMaxSurvives() {
        // 56012 is one of the keys the shipped saves actually carry. As a signed 16-bit it reads
        // -9524.
        SaveGameContainerEncounterData e = Encounter(56012, 56315);

        Assert.Equal(56012, e.GlobalDataKey1);
        Assert.Equal(56315, e.GlobalDataKey2);
    }

    [Fact]
    public void TheKeysAreWideEnoughForTheWholeSpace() =>
        // ushort.MaxValue is the widest a 16-bit key can be; the field must hold it as itself.
        Assert.Equal(ushort.MaxValue, Encounter(ushort.MaxValue, 0).GlobalDataKey1);

    [Fact]
    public void AKeyIsNeverNegative() {
        // The regression this came from: every shipped key is a non-negative global id, so a
        // negative one means the read was signed.
        foreach (int key in new[] { 0, 8086, 56012, 56252, 56315, ushort.MaxValue }) {
            Assert.True(Encounter(key, key).GlobalDataKey1 >= 0, "key " + key);
        }
    }

    [Fact]
    public void ZeroStaysZero() =>
        // 0 is meaningful: handle_Catapult @0x77164 shows "This must not be very important" when
        // the key is 0, so it must not be conflated with "no encounter data".
        Assert.Equal(0, Encounter(0, 0).GlobalDataKey1);
}
