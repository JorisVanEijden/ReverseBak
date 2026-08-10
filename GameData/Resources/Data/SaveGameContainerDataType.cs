namespace GameData.Resources.Data;

using System;

/// <summary>
/// The container record's flag byte — the DOS <c>Actor::flags</c> (ACTOR.H). Two concerns share
/// it: the low six bits are the <c>SUBREC_MASK</c> presence mask selecting which optional
/// subrecords follow the item array (so they fix the record's serialized size and are immutable
/// once authored), and the high two bits are mutable runtime state.
/// </summary>
[Flags]
public enum SaveGameContainerDataType : byte {
    Lock = 0x01,          // SUBREC_PARAMS, 4 bytes
    Dialog = 0x02,        // SUBREC_INTERACT_MSG, 6 bytes
    Shop = 0x04,          // SUBREC_EVENT_STATE, 16 bytes
    Encounter = 0x08,     // SUBREC_HOTSPOT_ACTION, 9 bytes
    Timestamp = 0x10,     // SUBREC_LAST_TOUCH, 4 bytes
    GlobalState = 0x20,   // 2 bytes

    /// <summary>
    /// Mutable state, not a subrecord: the container holds at least one item whose OBJINFO record
    /// carries flag 0x02. Recomputed over the whole item array on every content change
    /// (CMBINV.C:810, which canassa names <c>cmbinv_recompute_has_weapon_flag</c> — a guess the
    /// data contradicts: the 21 objects with that flag are notes, journals, traps and quest tokens,
    /// with not one weapon among them).
    ///
    /// <para>Its one consumer is the ground-bag recycler: <c>actorspawn_enc_location</c> ORs
    /// 0x80000000 into such a bag's last-touch time, sorting it last, so the bag holding a quest
    /// item is the last one whose contents get destroyed. "Protected" names that effect, which is
    /// all the evidence supports.</para>
    /// </summary>
    HoldsProtectedItem = 0x40,

    /// <summary>ACTOR_SELF_SPAWN — mutable state, not a subrecord. The record spawns itself into
    /// the visible pool, and <c>actorspawn_destroy_and_persist</c> returns it to
    /// <see cref="SaveGameContainerType.Free"/> once it is emptied. Every free ground-bag pool
    /// record ships with this set.</summary>
    SelfSpawn = 0x80,
}
