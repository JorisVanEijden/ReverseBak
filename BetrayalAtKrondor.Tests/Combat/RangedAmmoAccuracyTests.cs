namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The ammunition-accuracy lookup behind the ranged hit chance.</summary>
public class RangedAmmoAccuracyTests {
    [Fact]
    public void FiveKindsShareOneRecord() {
        foreach (int kind in new[] { 0, 3, 4, 7, 9 }) {
            Assert.Equal(0x24, RangedAmmoAccuracy.RecordFor(kind));
        }
        Assert.Equal(0x25, RangedAmmoAccuracy.RecordFor(1));
        Assert.Equal(0x25, RangedAmmoAccuracy.RecordFor(8));
        Assert.Equal(0x26, RangedAmmoAccuracy.RecordFor(2));
    }

    [Fact]
    public void KindsFiveAndSixGetNOAccuracyAtAll() {
        // *** They fall through the switch. *** Inventing a fallback would give them an accuracy the
        // original never grants.
        Assert.False(RangedAmmoAccuracy.HasAccuracyRecord(5));
        Assert.False(RangedAmmoAccuracy.HasAccuracyRecord(6));
        Assert.Equal(0, RangedAmmoAccuracy.BonusFor(5, accuracyOfRecord: 99));
        Assert.Equal(0, RangedAmmoAccuracy.BonusFor(6, accuracyOfRecord: 99));
    }

    [Fact]
    public void ThisIsNotTheInventoryTable() {
        // *** The trap. *** QuarrelInventory maps each of eight kinds to a DISTINCT object; this one
        // is many-to-one, has gaps, and covers kinds 8 and 9 which the inventory table does not.
        // Reusing either for the other's purpose is wrong in both directions.
        Assert.Equal(0x2a, QuarrelInventory.ObjectIdByKind[3]);   // inventory: kind 3 is its own item
        Assert.Equal(0x24, RangedAmmoAccuracy.RecordFor(3));      // accuracy: kind 3 reads 0x24

        Assert.Equal(8, QuarrelInventory.ObjectIdByKind.Length);  // inventory stops at kind 7
        Assert.True(RangedAmmoAccuracy.HasAccuracyRecord(9));     // accuracy goes to 9
    }

    [Fact]
    public void TheAIsInnateShotKindHasARecord() {
        // MonsterActionChoice fires kind 8, which is never carried as an item but does have accuracy.
        Assert.True(RangedAmmoAccuracy.HasAccuracyRecord(MonsterActionChoice.QuarrelType));
    }

    [Fact]
    public void AKindWithARecordPassesTheAccuracyThrough() {
        Assert.Equal(7, RangedAmmoAccuracy.BonusFor(quarrelKind: 0, accuracyOfRecord: 7));
    }
}
