namespace GameData.Resources.Combat;

using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System;

/// <summary>
/// Wearing out the gear you fight with — <c>cbstat_damage_equipped_items</c>
/// (canassa <c>COMBAT/STATS/CBSTAT.C:439</c>).
///
/// <para>Every attack wears something: the attacker's weapon on a swing or thrust, the defender's
/// armour when it is hit, the crossbow when it is shot. The amounts were modelled as constants on
/// <see cref="CombatFormulas"/> in August and <b>nothing applied them</b>, so no item in the game
/// had ever degraded.</para>
/// </summary>
/// <remarks>
/// <b>THE "AMOUNTS" ARE NOT WEAR POINTS — THEY ARE MULTIPLIERS.</b>
/// <c>condition -= break_amount * severity / 256</c>, where <c>break_amount</c> is rolled from the
/// ITEM's own <see cref="ObjectInfo.MaxWearPerDegrade"/>. So
/// <see cref="CombatFormulas.WeaponWearOnSwing"/> (256) is "one item-sized bite",
/// <see cref="CombatFormulas.WeaponWearOnThrust"/> (128) is half of one, and
/// <see cref="CombatFormulas.ArmorWearOnRangedHit"/> (512) is two. Read as points, a swing would
/// destroy any item in the game on its first use.
/// </remarks>
public static class ItemDegradation {
    /// <summary>The divisor the severity is expressed in — the same 1/256 the stat engine uses.</summary>
    public const int SeverityUnit = 0x100;

    /// <summary>
    /// <b>Asking for a sword also wears a STAFF.</b>
    /// </summary>
    /// <remarks>
    /// <c>altcategory = (category == 1) ? 3 : category</c>, and the item matches on either. A staff
    /// is a melee weapon everywhere else in combat too — it is what the swing cue asks about to
    /// pick wood-on-wood — so a port that matched only the requested category would leave a
    /// spellcaster's staff pristine for ever.
    /// </remarks>
    public static bool CategoryMatches(ObjectType requested, ObjectType itemType) =>
        itemType == requested
        || (requested == ObjectType.Sword && itemType == ObjectType.Staff);

    /// <summary>
    /// Whether this equipped item is a candidate at all: right category, and degradable.
    /// </summary>
    /// <remarks>
    /// <b>Equipped only.</b> A spare sword in the pack is untouched however hard its owner fights.
    /// </remarks>
    public static bool Wears(ObjectType requested, ObjectInfo info, ItemFlags itemFlags) =>
        info != null
        && (itemFlags & ItemFlags.Equipped) != 0
        && (info.Flags & ObjectFlags.Degradable) != 0
        && CategoryMatches(requested, info.ObjectType);

    /// <summary>
    /// The bite taken out of an item's condition: <c>roll(1..MaxWearPerDegrade) * severity / 256</c>.
    /// </summary>
    /// <param name="maxWearPerDegrade">The item's own <see cref="ObjectInfo.MaxWearPerDegrade"/>.</param>
    /// <param name="severity">One of <see cref="CombatFormulas"/>' four wear constants.</param>
    /// <param name="rnd"><c>RND(n)</c> — a value in <c>[0, n)</c>.</param>
    /// <remarks>
    /// <b>A max of 1 or less does NOT roll.</b> The original guards with
    /// <c>amount &gt; 1 ? RND(amount - 1) + 1 : 1</c>, so a one-point item always takes exactly one
    /// — calling <c>RND(0)</c> instead is a division by zero in most implementations and a silent
    /// zero in the rest, which would make those items indestructible.
    /// </remarks>
    public static int WearAmount(int maxWearPerDegrade, int severity, Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }
        int bite = maxWearPerDegrade > 1 ? rnd(maxWearPerDegrade - 1) + 1 : 1;
        return bite * severity / SeverityUnit;
    }

    /// <summary>What one wear event did to an item.</summary>
    public readonly struct Result {
        public Result(int condition, ItemFlags flags, bool snapped, bool broke) {
            Condition = condition;
            Flags = flags;
            Snapped = snapped;
            Broke = broke;
        }

        /// <summary>The item's condition afterwards.</summary>
        public int Condition { get; }

        /// <summary>Its flags afterwards.</summary>
        public ItemFlags Flags { get; }

        /// <summary>
        /// A crossbow gave way outright — the one wear event with a sound of its own.
        /// </summary>
        public bool Snapped { get; }

        /// <summary>The item is now <see cref="ItemFlags.Broken"/>.</summary>
        public bool Broke { get; }
    }

    /// <summary>
    /// The cue a snapping crossbow plays.
    /// </summary>
    /// <remarks>
    /// <b>The same id as the both-staves parry clang</b> (see <c>MeleeSwingSound</c>), which is the
    /// original's doing rather than ours — one wooden crack serving two events. Stated so nobody
    /// "fixes" the duplication by giving one of them a different sound.
    /// </remarks>
    public const int SnapSoundId = 0x43;

    /// <summary>
    /// <b>The item is marked on every qualifying attack, even when it does not wear.</b>
    /// </summary>
    /// <remarks>
    /// On the CD build — <b>which is ours</b> — a matching equipped item gets
    /// <see cref="ItemFlags.Unknown4"/> set BEFORE the degrade roll, so it is stamped whether or not
    /// the roll passes. The floppy gates the whole branch on the roll and sets that bit only
    /// alongside <see cref="ItemFlags.Repairable"/> when it does wear. Taking the floppy reading
    /// leaves the bit clear on items the CD build marks.
    /// </remarks>
    public const ItemFlags UsedInAnger = ItemFlags.Unknown4;

    /// <summary>
    /// Applies one wear event to a candidate item.
    /// </summary>
    /// <param name="info">The item's type record.</param>
    /// <param name="condition">Its condition now.</param>
    /// <param name="flags">Its flags now.</param>
    /// <param name="severity">One of <see cref="CombatFormulas"/>' four wear constants.</param>
    /// <param name="rnd"><c>RND(n)</c> — a value in <c>[0, n)</c>.</param>
    /// <remarks>
    /// <b>The degrade CHANCE is per item type, and most attacks wear nothing.</b>
    /// <see cref="ObjectInfo.DegradeChancePercent"/> gates it, so gear lasts through many fights
    /// rather than melting; a port that wore on every hit would have the party re-equipping
    /// constantly.
    ///
    /// <para><b>A crossbow can SNAP rather than wear down.</b> Category 2 only: if the worn
    /// condition falls to or below <c>RND(50)</c> it goes to zero at once with
    /// <see cref="SnapSoundId"/>. So a bow near the end of its life is not merely weak — it is
    /// living on a coin toss, and that is the game's one audible item failure.</para>
    ///
    /// <para><b>The floor is applied AFTER the snap, so it cannot save a snapped bow</b> — the
    /// original writes 0, then clamps to <see cref="ObjectInfo.MinimumQuality"/>, then tests for
    /// broken. An item whose floor is above zero therefore snaps to its floor and is not flagged
    /// broken, which is faithful and looks like a bug from either end alone.</para>
    /// </remarks>
    public static Result Apply(ObjectInfo info, int condition, ItemFlags flags, int severity,
        Func<int, int> rnd) {
        if (info == null) {
            throw new ArgumentNullException(nameof(info));
        }
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }

        // Marked first and unconditionally: the CD build's arm.
        ItemFlags marked = flags | UsedInAnger;

        if (rnd(100) >= info.DegradeChancePercent) {
            return new Result(condition, marked, snapped: false, broke: false);
        }

        int worn = condition - WearAmount(info.MaxWearPerDegrade, severity, rnd);

        var snapped = false;
        if (info.ObjectType == ObjectType.Crossbow && worn <= rnd(0x32)) {
            worn = 0;
            snapped = true;
        }

        marked |= ItemFlags.Repairable;

        if (worn < info.MinimumQuality) {
            worn = info.MinimumQuality;
        }
        if (worn <= 0) {
            marked |= ItemFlags.Broken;
        }

        return new Result(worn, marked, snapped, (marked & ItemFlags.Broken) != 0);
    }
}
