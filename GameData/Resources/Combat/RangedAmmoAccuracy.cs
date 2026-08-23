namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// Which ammunition record supplies the accuracy bonus in
/// <see cref="CombatFormulas.RangedHitChance"/> — the switch inside
/// <c>combatenc_compute_hit_chance</c> (canassa CBENC.C:452).
///
/// <para><b>This is NOT <see cref="QuarrelInventory.ObjectIdByKind"/>.</b> That table maps each of
/// the eight carryable kinds to its own distinct object; this one is many-to-one, covers kinds the
/// inventory table does not, and leaves two kinds with no record at all. Reusing either for the
/// other's purpose would be wrong in both directions.</para>
/// </summary>
public static class RangedAmmoAccuracy {
    /// <summary>The switch has no entry for this kind.</summary>
    public const int NoRecord = -1;

    /// <summary>
    /// Quarrel kind to the object whose accuracy field is added to the shot.
    /// </summary>
    /// <remarks>
    /// <b>Five kinds share one record and two kinds have none.</b> 0, 3, 4, 7 and 9 all read 0x24;
    /// 1 and 8 read 0x25; 2 reads 0x26; <b>5 and 6 fall through the switch entirely</b> and add
    /// nothing. So the ammunition a shooter picks changes accuracy far less than the eight-kind
    /// inventory suggests — most kinds are accuracy-identical.
    ///
    /// <para>Note kinds <b>8 and 9 appear here but not in the inventory table</b>, which stops at 7.
    /// Kind 8 is what the AI passes for a creature's innate shot
    /// (<see cref="MonsterActionChoice.QuarrelType"/>), so this mapping spans attacks that are never
    /// carried as items.</para>
    /// </remarks>
    private static readonly Dictionary<int, int> ByKind = new Dictionary<int, int> {
        { 0, 0x24 }, { 3, 0x24 }, { 4, 0x24 }, { 7, 0x24 }, { 9, 0x24 },
        { 1, 0x25 }, { 8, 0x25 },
        { 2, 0x26 },
    };

    /// <summary>The accuracy record for a kind, or <see cref="NoRecord"/>.</summary>
    public static int RecordFor(int quarrelKind) =>
        ByKind.TryGetValue(quarrelKind, out int objectId) ? objectId : NoRecord;

    /// <summary>Whether this kind contributes any accuracy at all.</summary>
    public static bool HasAccuracyRecord(int quarrelKind) => RecordFor(quarrelKind) != NoRecord;

    /// <summary>
    /// The bonus to feed <see cref="CombatFormulas.RangedHitChance"/>.
    /// </summary>
    /// <param name="quarrelKind">The kind being fired.</param>
    /// <param name="accuracyOfRecord">
    /// The <c>nDefense_or_range_close</c> field of the object <see cref="RecordFor"/> names.
    /// </param>
    /// <remarks>
    /// A kind with no record contributes <b>zero</b>, not a default — the original never touches the
    /// item table for those, so inventing a fallback would give kinds 5 and 6 an accuracy they do
    /// not have.
    /// </remarks>
    public static int BonusFor(int quarrelKind, int accuracyOfRecord) =>
        HasAccuracyRecord(quarrelKind) ? accuracyOfRecord : 0;
}
