namespace GameData.Resources.Character;

using GameData.Resources.Data;

/// <summary>
/// One actor attribute as the engine holds it at runtime — the mutable counterpart of the
/// read-only <see cref="SaveGameAttributeValuesData"/> the save game parses into.
///
/// <para>Field-for-field this is the DOS <c>StatSlot</c> (canassa <c>SRC/CHAR/STAT.C</c>):</para>
/// <list type="bullet">
/// <item><see cref="Base"/> — <c>base</c>, the stored value skills advance through.</item>
/// <item><see cref="Max"/> — <c>max</c>; a stat whose Max is 0 does not exist for this actor and
/// is inert (both read and write short-circuit).</item>
/// <item><see cref="Experience"/> — <c>frac</c>, the sub-unit remainder that carries between
/// changes. This is what makes skill use accumulate: a change worth less than one whole point
/// is banked here instead of being lost.</item>
/// <item><see cref="Modifier"/> — <c>perm_mod</c>, the permanent bonus recomputed from equipped
/// items. Signed.</item>
/// <item><see cref="Effective"/> — <c>cached</c>, the last value a read computed. The engine
/// writes it as a side effect of reading; nothing should treat it as an input.</item>
/// </list>
/// </summary>
public sealed class ActorStat {
    public ActorStat() { }

    public ActorStat(SaveGameAttributeValuesData saved) {
        Max = saved.Maximum;
        Base = saved.Current;
        Effective = saved.CurrentEffective;
        Experience = saved.Experience;
        Modifier = unchecked((sbyte)saved.Modifier);
    }

    /// <summary>The stored value (<c>base</c>), 0..255.</summary>
    public byte Base { get; set; }

    /// <summary>The ceiling this actor's value may reach (<c>max</c>). 0 = the stat is inert.</summary>
    public byte Max { get; set; }

    /// <summary>Banked sub-unit progress (<c>frac</c>), 0..255.</summary>
    public byte Experience { get; set; }

    /// <summary>Permanent equipment bonus (<c>perm_mod</c>), signed.</summary>
    public sbyte Modifier { get; set; }

    /// <summary>Last computed read (<c>cached</c>); an output of <see cref="StatEngine.Get"/>.</summary>
    public byte Effective { get; set; }

    public SaveGameAttributeValuesData ToSaveData() =>
        new SaveGameAttributeValuesData(Max, Base, Effective, Experience, unchecked((byte)Modifier));
}
