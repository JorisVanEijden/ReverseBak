namespace GameData.Resources.GameState;

using System.Collections.Generic;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(SetFlagEffect), nameof(SetFlagEffect))]
[JsonDerivedType(typeof(SetVarEffect), nameof(SetVarEffect))]
[JsonDerivedType(typeof(SetFlagsEffect), nameof(SetFlagsEffect))]
[JsonDerivedType(typeof(SetFlagBitsEffect), nameof(SetFlagBitsEffect))]
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

/// <summary>Set named variable <see cref="Var"/> (= key − 30000, or raw key when unconfirmed) to <see cref="Value"/>.</summary>
/// <remarks>
/// <b><see cref="Var"/> CARRIES TWO DIFFERENT THINGS and nothing on the object says which.</b> The
/// decoder emits <c>key - 30000</c> for the confirmed named-variable range (30000..30029) and the
/// RAW key for any unconfirmed direct write, so applying it needs
/// <see cref="GlobalKey"/> rather than a bare addition either way.
///
/// <para>The clean fix is a distinct raw-write effect, mirroring <c>RawGlobalCondition</c> on the
/// condition side — the effect vocabulary is missing that counterpart and overloads this type
/// instead. That change touches the extractor and every generated file, so it is recorded rather
/// than made here.</para>
/// </remarks>
public class SetVarEffect : Effect {
    public int Var { get; set; }
    public int Value { get; set; }

    /// <summary>First key of the confirmed named-variable range.</summary>
    public const int VarRangeBase = 30000;

    /// <summary>How many named variables that range holds.</summary>
    public const int VarRangeCount = 30;

    /// <summary>
    /// The save-state key this write lands on.
    /// </summary>
    /// <remarks>
    /// <b>Disambiguated by range, which works because the two forms cannot overlap in practice.</b>
    /// A decoded variable is 0..29; a raw key that reached the fallback is outside 1..8499 and
    /// outside the variable range, so it is far above 29. Across the whole shipped tree there are 39
    /// of these — 38 decoded (vars 0, 4, 14, 15, 16, 17) and exactly one raw (56277) — and no value
    /// is ambiguous between the two readings.
    ///
    /// <para>The one theoretical collision is key 0: it is excluded from the flag range by
    /// <c>key >= 1</c> and would fall through as <c>Var = 0</c>, indistinguishable from variable 0.
    /// It does not occur in the shipped data. If it ever does, this returns 30000 for it — which is
    /// the reason to make the extractor emit a distinct type rather than to add a guess here.</para>
    /// </remarks>
    public int GlobalKey =>
        Var >= 0 && Var < VarRangeCount ? VarRangeBase + Var : Var;
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
