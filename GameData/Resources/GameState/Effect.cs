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
public class SetVarEffect : Effect {
    public int Var { get; set; }
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
