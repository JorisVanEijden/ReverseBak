namespace GameData.Resources.Character;

using GameData.Resources.Data;
using System;

/// <summary>
/// One actor's seven affliction ranks, 0..100 each — the mutable counterpart of the read-only
/// <see cref="SaveGameActorStatusEffectsData"/> the save game parses into.
///
/// <para>Rank is an intensity, not a boolean: 0 means the actor is free of it, and everything above
/// that scales both how fast it worsens and how hard it bites. The DOS engine keeps this as
/// <c>abActorStatusRanks[partySlot][7]</c>, a per-party-slot row — non-party actors have no row at
/// all, which is why afflictions simply do not apply to them.</para>
/// </summary>
public sealed class ActorConditions {
    /// <summary>Number of afflictions the engine tracks.</summary>
    public const int Count = 7;

    /// <summary>The highest any rank can reach.</summary>
    public const int MaxRank = 100;

    private readonly byte[] _ranks = new byte[Count];

    public ActorConditions() { }

    public ActorConditions(SaveGameActorStatusEffectsData saved) {
        if (saved == null) {
            return;
        }
        _ranks[(int)ActorCondition.Sick] = saved.Sick;
        _ranks[(int)ActorCondition.Plagued] = saved.Plagued;
        _ranks[(int)ActorCondition.Poisoned] = saved.Poisoned;
        _ranks[(int)ActorCondition.Drunk] = saved.Drunk;
        _ranks[(int)ActorCondition.Healing] = saved.Healing;
        _ranks[(int)ActorCondition.Starving] = saved.Starving;
        _ranks[(int)ActorCondition.NearDeath] = saved.NearDeath;
    }

    /// <summary>Rank of one affliction, 0..100. Setting clamps into range.</summary>
    public int this[ActorCondition condition] {
        get {
            int index = (int)condition;
            if (index < 0 || index >= Count) {
                throw new ArgumentOutOfRangeException(nameof(condition));
            }
            return _ranks[index];
        }
        set {
            int index = (int)condition;
            if (index < 0 || index >= Count) {
                throw new ArgumentOutOfRangeException(nameof(condition));
            }
            _ranks[index] = (byte)(value < 0 ? 0 : value > MaxRank ? MaxRank : value);
        }
    }

    /// <summary>Is this affliction present at all?</summary>
    public bool Has(ActorCondition condition) => this[condition] > 0;

    /// <summary>Is the actor free of every affliction?</summary>
    public bool None {
        get {
            for (int i = 0; i < Count; i++) {
                if (_ranks[i] != 0) {
                    return false;
                }
            }
            return true;
        }
    }

    public SaveGameActorStatusEffectsData ToSaveData() =>
        new SaveGameActorStatusEffectsData(
            _ranks[(int)ActorCondition.Sick],
            _ranks[(int)ActorCondition.Plagued],
            _ranks[(int)ActorCondition.Poisoned],
            _ranks[(int)ActorCondition.Drunk],
            _ranks[(int)ActorCondition.Healing],
            _ranks[(int)ActorCondition.Starving],
            _ranks[(int)ActorCondition.NearDeath]);
}
