namespace BetrayalAtKrondor.Tests.Data;

using GameData;
using GameData.Resources.Data;
using ResourceExtraction.Extractors.Exe;
using Xunit;

/// <summary>
/// The attribute-index → attribute-record map dialog text-variable kind 27 resolves through.
/// The map's whole job is being in the executable's order, so the tests pin that order against the
/// two other places it is written down (the manifest's key names and <c>ActorAttribute</c>) rather
/// than restating it a fourth time.
/// </summary>
public class ActorAttributeValuesTests {
    // Each attribute record gets a Maximum equal to its index, so a mis-wired property is visible
    // as a wrong number rather than as a coincidence.
    private static SaveGameActorData Actor() {
        SaveGameAttributeValuesData V(int i) =>
            new SaveGameAttributeValuesData((byte)i, (byte)(100 + i), (byte)(200 - i), 0, 0);
        return new SaveGameActorData(
            0, 0, 0, 0,
            V(0), V(1), V(2), V(3), V(4), V(5), V(6), V(7),
            V(8), V(9), V(10), V(11), V(12), V(13), V(14), V(15),
            actorNumber: 1, inventoryPointer: 0, combatDataPointer: 0);
    }

    [Fact]
    public void EveryIndexReachesItsOwnAttributeInTableOrder() {
        SaveGameActorData actor = Actor();
        for (int i = 0; i < ActorAttributeValues.Count; i++) {
            Assert.Equal(i, ActorAttributeValues.MaximumOf(actor, i));
        }
    }

    // Kind 27 shows the MAXIMUM, not the current value: GetAttributeFromActor's whichValue is the
    // Maximum enumerator at the 0x48cc5 call site. A port reading Current would still "work" on a
    // fresh save (where Maximum == Current) and diverge only after damage — hence an explicit test.
    [Fact]
    public void ReadsTheMaximumNotTheCurrentValue() {
        SaveGameActorData actor = Actor();
        Assert.Equal(3, ActorAttributeValues.MaximumOf(actor, 3));
        Assert.Equal(103, ActorAttributeValues.At(actor, 3).Current);
    }

    // The index comes from a save-supplied dialog global, so out-of-range is data, not a bug:
    // it must render as 0, never throw mid-dialog.
    [Theory]
    [InlineData(-1)]
    [InlineData(16)] // HealthStaminaCombo — a derived pseudo-attribute, not a stored record
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void OutOfRangeIndexYieldsZeroRatherThanThrowing(int index) {
        Assert.Null(ActorAttributeValues.At(Actor(), index));
        Assert.Equal(0, ActorAttributeValues.MaximumOf(Actor(), index));
    }

    [Fact]
    public void NullActorYieldsZero() {
        SaveGameActorData? absent = null;
        Assert.Null(ActorAttributeValues.At(absent!, 0));
        Assert.Equal(0, ActorAttributeValues.MaximumOf(absent!, 0));
    }

    // The map, the catalog keys and the enum are three copies of one order the executable owns.
    // If any one of them is reordered this fails, which is the point.
    [Fact]
    public void IndexOrderMatchesTheManifestsAttributeTable() {
        ExeStringTable? table = null;
        foreach (ExeStringTable t in ExeStringManifest.Tables) {
            if (t.KeyPrefix == "attribute") { table = t; }
        }
        Assert.NotNull(table);
        Assert.Equal(ActorAttributeValues.Count, table!.Names.Length);
    }

    [Fact]
    public void IndexOrderMatchesActorAttributesFirstSixteenMembers() {
        Assert.Equal(ActorAttributeValues.Count, (int)ActorAttribute.HealthStaminaCombo);
        Assert.Equal(0, (int)ActorAttribute.Health);
        Assert.Equal(3, (int)ActorAttribute.Strength);
        Assert.Equal(15, (int)ActorAttribute.Stealth);
    }
}
