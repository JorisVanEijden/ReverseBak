namespace GameData.Resources.Character;

using GameData.Resources.Text;

/// <summary>
/// The display names of an actor's attributes and afflictions, by the number the engine indexes
/// them with.
/// </summary>
/// <remarks>
/// <b>The numbering is the executable's, and it is shared.</b> Both tables are arithmetically
/// indexed in the original — attributes at a 15-byte stride, conditions at 23 — by the same number
/// that walks the actor record, so an ordering restated per caller can disagree with the data
/// without anything noticing (an indexed table has no xrefs to contradict). One list here, which
/// the extractor's manifest lifted into the catalog under these keys, so a translation or an
/// override reaches every consumer at once.
/// </remarks>
public static class ActorLabels {
    /// <summary>The catalog key for an attribute's name, or empty when the number names none.</summary>
    public static string AttributeKey(int attributeNumber) =>
        attributeNumber >= 0 && attributeNumber < AttributeKeys.Length
            ? "base:uistring:attribute." + AttributeKeys[attributeNumber]
            : "";

    /// <summary>An attribute's display name, empty when the number names none.</summary>
    public static string AttributeName(int attributeNumber) => Lookup(AttributeKey(attributeNumber));

    /// <summary>The catalog key for an affliction's name, or empty when the number names none.</summary>
    public static string ConditionKey(int conditionNumber) =>
        conditionNumber >= 0 && conditionNumber < ConditionKeys.Length
            ? "base:uistring:condition." + ConditionKeys[conditionNumber]
            : "";

    /// <summary>An affliction's display name, empty when the number names none.</summary>
    public static string ConditionName(int conditionNumber) => Lookup(ConditionKey(conditionNumber));

    /// <inheritdoc cref="ConditionName(int)"/>
    public static string ConditionName(ActorCondition condition) => ConditionName((int)condition);

    private static string Lookup(string key) => key.Length == 0 ? "" : UiStrings.Get(key);

    /// <summary>The 16 attributes the sheet can name, in the executable's order.</summary>
    private static readonly string[] AttributeKeys = {
        "health", "stamina", "speed", "strength", "defense",
        "accy_crossbow", "accy_melee", "accy_casting", "assessment", "armorcraft",
        "weaponcraft", "barding", "haggling", "lockpick", "scouting", "stealth",
    };

    /// <summary>The seven afflictions, in <see cref="ActorCondition"/>'s order.</summary>
    private static readonly string[] ConditionKeys = {
        "sick", "plagued", "poisoned", "drunk", "healing", "starving", "near_death",
    };
}
