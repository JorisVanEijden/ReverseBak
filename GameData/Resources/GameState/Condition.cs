namespace GameData.Resources.GameState;

using System.Collections.Generic;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(FlagCondition), nameof(FlagCondition))]
[JsonDerivedType(typeof(HasItemCondition), nameof(HasItemCondition))]
[JsonDerivedType(typeof(HasNoteCondition), nameof(HasNoteCondition))]
[JsonDerivedType(typeof(SpellTimerActiveCondition), nameof(SpellTimerActiveCondition))]
[JsonDerivedType(typeof(RandomCondition), nameof(RandomCondition))]
[JsonDerivedType(typeof(VarCondition), nameof(VarCondition))]
[JsonDerivedType(typeof(PartyCondition), nameof(PartyCondition))]
[JsonDerivedType(typeof(InChapters), nameof(InChapters))]
[JsonDerivedType(typeof(AllOf), nameof(AllOf))]
[JsonDerivedType(typeof(AnyOf), nameof(AnyOf))]
[JsonDerivedType(typeof(RawGlobalCondition), nameof(RawGlobalCondition))]
[JsonDerivedType(typeof(RawFlagBits), nameof(RawFlagBits))]
#endif

/// <summary>
/// A predicate over game state, decoded from a raw global key + range. Shared by
/// every carrier (dialog branch, GDS hotspot gate, DEF trigger gate). Open
/// union: new operation kinds are added as new subtypes. See
/// <c>docs/specs/global-value-destructuring.md</c>.
/// </summary>
public abstract class Condition;

/// <summary>Global key 0–8499 / a single global_flags2 bit: flag must equal <see cref="Set"/>.</summary>
public class FlagCondition : Condition {
    public int Flag { get; set; }
    public bool Set { get; set; }
}

/// <summary>Range 50000+: party carries at least <see cref="AtLeast"/> (and at most <see cref="AtMost"/>) of item <see cref="Item"/> (= key − 50000).</summary>
public class HasItemCondition : Condition {
    public int Item { get; set; }
    public int AtLeast { get; set; }
    public int? AtMost { get; set; }
}

/// <summary>Range 51000+: party is carrying note <see cref="Note"/> (= key − 51000).</summary>
public class HasNoteCondition : Condition {
    public int Note { get; set; }
}

/// <summary>Range 52000+: spell timer <see cref="Timer"/> (= key − 52000) is active.</summary>
public class SpellTimerActiveCondition : Condition {
    public int Timer { get; set; }
}

/// <summary>Range 53000+: <c>random(0..Bound)</c> (Bound = key − 53000) falls in [<see cref="Min"/>, <see cref="Max"/>].</summary>
public class RandomCondition : Condition {
    public int Bound { get; set; }
    public int Min { get; set; }
    public int? Max { get; set; }
}

/// <summary>Range 30000–30029: named variable <see cref="Var"/> (= key − 30000) in [<see cref="Min"/>, <see cref="Max"/>].</summary>
public class VarCondition : Condition {
    public int Var { get; set; }
    public int Min { get; set; }
    public int? Max { get; set; }
}

/// <summary>Range 40001–40013: party condition check <see cref="Check"/> (= key − 40000) in [<see cref="Min"/>, <see cref="Max"/>].</summary>
public class PartyCondition : Condition {
    public int Check { get; set; }
    public int Min { get; set; }
    public int? Max { get; set; }
}

/// <summary>Current chapter is one of <see cref="Chapters"/> (8 also covers 9+).</summary>
public class InChapters : Condition {
    public List<int> Chapters { get; set; } = [];
}

/// <summary>All of <see cref="Conditions"/> must hold.</summary>
public class AllOf : Condition {
    public List<Condition> Conditions { get; set; } = [];
}

/// <summary>At least one of <see cref="Conditions"/> must hold.</summary>
public class AnyOf : Condition {
    public List<Condition> Conditions { get; set; } = [];
}

/// <summary>Fallback for an unconfirmed single key: <c>GetGlobalValue(Key)</c> in [<see cref="Min"/>, <see cref="Max"/>].</summary>
public class RawGlobalCondition : Condition {
    public int Key { get; set; }
    public int Min { get; set; }
    public int? Max { get; set; }
}

/// <summary>Fallback for an unconfirmed flag group: bit positions as a list, never a mask word.</summary>
public class RawFlagBits : Condition {
    public int Group { get; set; }
    public List<BitState> Bits { get; set; } = [];
}
