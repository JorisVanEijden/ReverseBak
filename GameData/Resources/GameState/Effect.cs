namespace GameData.Resources.GameState;

using System.Collections.Generic;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(SetFlagEffect), nameof(SetFlagEffect))]
[JsonDerivedType(typeof(SetVarEffect), nameof(SetVarEffect))]
[JsonDerivedType(typeof(SetFlagsEffect), nameof(SetFlagsEffect))]
[JsonDerivedType(typeof(SetFlagBitsEffect), nameof(SetFlagBitsEffect))]
[JsonDerivedType(typeof(RawGlobalWriteEffect), nameof(RawGlobalWriteEffect))]
#endif

/// <summary>
/// A mutation of game state, decoded from a raw global write. Shared by dialog
/// actions and DEF trigger entries. Open union. See
/// <c>docs/specs/global-value-destructuring.md</c>.
/// </summary>
public abstract class Effect;

/// <summary>Set flag <see cref="Flag"/> (raw global key) to <see cref="Set"/>; <see cref="ForTicks"/> non-null = temporary.</summary>
public class SetFlagEffect : Effect {
    public int Flag { get; set; }
    public bool Set { get; set; }
    public uint? ForTicks { get; set; }
}

/// <summary>Set named variable <see cref="Var"/> (= key − 30000) to <see cref="Value"/>.</summary>
/// <remarks>
/// <b><see cref="Var"/> is a decoded variable index and nothing else.</b> It used to double as a raw
/// key for unconfirmed direct writes, with only a range check on <see cref="GlobalKey"/> telling the
/// two apart — an overload that made <c>30000 + Var</c>, the obvious way to apply this, silently
/// write to the wrong global. Those writes are <see cref="RawGlobalWriteEffect"/> now, so the
/// ambiguity is gone from the type rather than managed inside it.
/// </remarks>
public class SetVarEffect : Effect {
    public int Var { get; set; }
    public int Value { get; set; }

    /// <summary>First key of the confirmed named-variable range.</summary>
    public const int VarRangeBase = 30000;

    /// <summary>How many named variables that range holds.</summary>
    public const int VarRangeCount = 30;

    /// <summary>The save-state key this write lands on.</summary>
    public int GlobalKey => VarRangeBase + Var;
}

/// <summary>
/// Fallback for an unconfirmed direct write: <c>SetGlobalValue(Key, Value)</c> on a raw key.
/// </summary>
/// <remarks>
/// <b>The counterpart to <see cref="RawGlobalCondition"/></b>, which the effect vocabulary was
/// missing — an asymmetry between the two halves of one spec. Reads had an honest carrier for
/// "a key we have not confirmed"; writes borrowed <see cref="SetVarEffect"/> for it instead.
///
/// <para><b>The key is absolute, not an offset.</b> That is the whole point of the type: nothing has
/// to work out which of two meanings the number carries.</para>
///
/// <para>Rare in shipped data — exactly one instance (key 56277) across the generated tree — but the
/// rarity is why the overload survived unnoticed, not a reason to keep it.</para>
/// </remarks>
public class RawGlobalWriteEffect : Effect {
    public int Key { get; set; }
    public int Value { get; set; }
}

/// <summary>A masked multi-flag write expanded to a list of per-flag writes.</summary>
public class SetFlagsEffect : Effect {
    public List<FlagState> Flags { get; set; } = [];
}

/// <summary>Fallback for an unconfirmed flag group write: bit positions as a list, never a mask word.</summary>
public class SetFlagBitsEffect : Effect {
    public int Group { get; set; }
    public List<BitState> Bits { get; set; } = [];
}
