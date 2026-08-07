namespace GameData.Resources.Data;

/// <summary>
/// Attribute-index → attribute-record lookup over a saved actor record.
///
/// <para><b>The index order is fixed by the executable, not by our enum.</b>
/// <c>GetAttributeFromActor</c> (0x42fca) reaches an attribute by pointer arithmetic —
/// <c>actor + 8 + index * 5</c> over the packed {max, current, currentEffective, experience,
/// modifier} quintuples — and the 15-byte display-name table at 0x37930 is indexed with the very
/// same number (<c>imul ax, 0Fh</c>). <c>ExeStringManifest</c> declares the catalog keys
/// (<c>base:uistring:attribute.*</c>) in that same order, and <c>DialogSlotContext.AttributeKeys</c>
/// repeats it. So the order below is a property of KRONDOR.EXE's data layout; that
/// <c>ActorAttribute</c> happens to agree with it for its first 16 members is a consequence, not the
/// source of truth. Reordering that enum must NOT reorder this — the executable's table would not
/// have moved.</para>
///
/// <para><see cref="ActorAttribute.HealthStaminaCombo"/> (17th) is deliberately absent: it is a
/// derived pseudo-attribute the original computes by summing Health and Stamina (0x42fea), never a
/// stored record, and it has no name in the display table.</para>
/// </summary>
public static class ActorAttributeValues {
    /// <summary>The 16 attributes the executable stores and names. Not 17 — see the type remarks.</summary>
    public const int Count = 16;

    /// <summary>
    /// The attribute record at <paramref name="index"/>, or <c>null</c> when the actor is null or
    /// the index falls outside <c>0..15</c>. Out of range yields null rather than throwing: the
    /// index arrives from a dialog global the save file supplies, so a bad value is bad data to be
    /// rendered harmlessly, not a programming error to abort on.
    /// </summary>
    public static SaveGameAttributeValuesData At(SaveGameActorData actor, int index) {
        if (actor == null) {
            return null;
        }
        return index switch {
            0 => actor.Health,
            1 => actor.Stamina,
            2 => actor.Speed,
            3 => actor.Strength,
            4 => actor.Defense,
            5 => actor.AccuracyCrossbow,
            6 => actor.AccuracyMelee,
            7 => actor.AccuracyCasting,
            8 => actor.Assessment,
            9 => actor.Armorcraft,
            10 => actor.Weaponcraft,
            11 => actor.Barding,
            12 => actor.Haggling,
            13 => actor.Lockpick,
            14 => actor.Scouting,
            15 => actor.Stealth,
            _ => null,
        };
    }

    /// <summary>
    /// The <b>maximum</b> (not current, not effective) value of attribute <paramref name="index"/>,
    /// or 0 when the actor or index is absent.
    ///
    /// <para>Maximum is what dialog text-variable kind 27 shows: <c>PopulateDialogSlotText</c>
    /// case 27 (0x48cc5) pushes the <c>Maximum</c> enumerator as <c>GetAttributeFromActor</c>'s
    /// <c>whichValue</c>, and that branch (0x43021) returns <c>attributeValues.max_</c> verbatim
    /// with no active-effect modifiers applied.</para>
    /// </summary>
    public static int MaximumOf(SaveGameActorData actor, int index) => At(actor, index)?.Maximum ?? 0;
}
