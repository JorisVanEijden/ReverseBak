namespace GameData.Resources.Spells;

using GameData.Resources.Combat;
using System.Collections.Generic;

/// <summary>
/// One lingering spell effect on a combatant — a node in <c>p20activeSpellEffects</c>.
///
/// <para>Field names follow IDA rather than canassa, which calls the spell number <c>nType</c> and
/// the invested cost <c>nSource</c>; both are less accurate than what the code stores.</para>
/// </summary>
public sealed class ActiveSpellEffect {
    /// <summary>The spell that placed this effect. <see cref="ActiveSpellEffectPool.None"/> marks
    /// the slot free — it is the only thing the allocator looks at.</summary>
    public int SpellNumber { get; set; } = ActiveSpellEffectPool.None;

    /// <summary>The power the caster invested, which is what the effect scales from.</summary>
    public int InvestedCost { get; set; }

    /// <summary>How long the effect lasts.</summary>
    public int Duration { get; set; }

    /// <summary>Ticks elapsed. Zeroed when the effect is registered.</summary>
    public int Age { get; set; }

    /// <summary>The effect's byte payload (<c>field_A</c>); its meaning is per-spell.</summary>
    public byte Flag { get; set; }

    /// <summary>Next slot in this actor's chain, or <see cref="ActiveSpellEffectPool.None"/>.</summary>
    public int Next { get; set; } = ActiveSpellEffectPool.None;
}

/// <summary>
/// The twenty lingering spell effects a tactical encounter can hold at once, shared by every
/// combatant on the field and chained per actor.
///
/// <para>Ported from <c>initActiveSpellEffectSlots</c>, <c>getNextFreeActiveSpellEffectSlot</c>,
/// <c>ApplySpellToActor</c> @0x66951 and <c>RemoveActorSpellEffectSlot</c> @0x66a1f (canassa's
/// <c>cspell_status_effect_*</c>). Each actor holds the index of its first effect and the nodes
/// link forward from there; a free node is one whose <see cref="ActiveSpellEffect.SpellNumber"/> is
/// <see cref="None"/>.</para>
///
/// <para><b>This pool is encounter-scoped, not saved.</b> The original allocates it when the combat
/// spell subsystem loads and frees it on unload, so nothing here survives a fight. Do not confuse
/// it with <c>SaveGameActorStatusEffectsData</c> — that is the seven persistent afflictions
/// (Sick, Plagued, Poisoned, …) modelled by <c>ActorConditions</c>. Both get called "status
/// effects" and they are unrelated systems. Time-limited blessings that outlive a fight belong to
/// the separate overworld spell-timer system.</para>
/// </summary>
public sealed class ActiveSpellEffectPool {
    /// <summary>Slots in the pool — the "20" in <c>p20activeSpellEffects</c>.</summary>
    public const int Capacity = 20;

    /// <summary>The empty sentinel, used for both a free slot and the end of a chain.</summary>
    public const int None = -1;

    private readonly ActiveSpellEffect[] _slots = new ActiveSpellEffect[Capacity];

    public ActiveSpellEffectPool() {
        for (var i = 0; i < Capacity; i++) {
            _slots[i] = new ActiveSpellEffect();
        }
    }

    /// <summary>The slot at an index. Callers holding an index from <see cref="Register"/> read the
    /// effect through here.</summary>
    public ActiveSpellEffect this[int slot] =>
        slot >= 0 && slot < Capacity ? _slots[slot] : null;

    /// <summary>Returns every slot to the free state — <c>initActiveSpellEffectSlots</c>.</summary>
    public void Reset() {
        foreach (ActiveSpellEffect slot in _slots) {
            slot.SpellNumber = None;
            slot.Next = None;
            slot.InvestedCost = 0;
            slot.Duration = 0;
            slot.Age = 0;
            slot.Flag = 0;
        }
    }

    /// <summary>
    /// The first free slot, or <see cref="None"/> when all twenty are taken.
    ///
    /// <para>A linear scan for a free <see cref="ActiveSpellEffect.SpellNumber"/> — there is no free
    /// list, so a released slot becomes available purely by being marked.</para>
    /// </summary>
    public int Allocate() {
        for (var i = 0; i < Capacity; i++) {
            if (_slots[i].SpellNumber == None) {
                return i;
            }
        }
        return None;
    }

    /// <summary>
    /// Registers a lingering effect on an actor, appending to the end of its chain —
    /// <c>ApplySpellToActor</c>.
    ///
    /// <para>Despite that name the original computes no damage here; the magnitude is
    /// <see cref="SpellEffectMagnitude"/>'s job. This only records that the effect is present.</para>
    /// </summary>
    /// <returns>The slot used, or <see cref="None"/> if the pool was full — in which case
    /// <b>nothing is recorded and the cast silently has no lingering effect</b>.</returns>
    public int Register(Combatant actor, int spellNumber, int investedCost, int duration,
        byte flag = 0) {
        if (actor == null) {
            return None;
        }

        int slot = actor.ActiveEffectSlot;
        if (slot == None) {
            slot = Allocate();
            actor.ActiveEffectSlot = slot;
        } else {
            while (_slots[slot].Next != None) {
                slot = _slots[slot].Next;
            }
            _slots[slot].Next = Allocate();
            slot = _slots[slot].Next;
        }

        if (slot != None) {
            ActiveSpellEffect effect = _slots[slot];
            effect.SpellNumber = spellNumber;
            effect.InvestedCost = investedCost;
            effect.Duration = duration;
            effect.Flag = flag;
            effect.Age = 0;
            effect.Next = None;
        }
        return slot;
    }

    /// <summary>
    /// Releases one effect — <c>RemoveActorSpellEffectSlot</c>.
    ///
    /// <para><b>Reproduces a defect in the original, deliberately.</b> Removing the actor's
    /// <i>first</i> effect sets the chain head to <see cref="None"/> instead of to the removed
    /// node's successor, so every later effect on that actor is orphaned: still marked in use, no
    /// longer reachable, and never freed for the rest of the encounter. Removing any other effect
    /// unlinks correctly. Confirmed in the disassembly at 0x66a3a, not merely inherited from
    /// canassa.</para>
    ///
    /// <para>It is kept because it is observable play behaviour — a character can lose one buff and
    /// keep another that should have gone with it, and a long fight can exhaust the pool. If the
    /// Unity layer ever wants the corrected version, that should be a deliberate, recorded choice
    /// rather than a silent divergence.</para>
    /// </summary>
    public void Remove(Combatant actor, int slot) {
        if (actor == null || slot < 0 || slot >= Capacity) {
            // The original's bound is `slot <= 20`, which admits one index past the pool and writes
            // out of bounds. Unreachable — Allocate only ever yields 0..19 — so the range is
            // corrected here rather than reproducing a stray write.
            return;
        }

        if (actor.ActiveEffectSlot == slot) {
            actor.ActiveEffectSlot = None; // the defect described above
        } else {
            int current = actor.ActiveEffectSlot;
            if (current != None) {
                while (_slots[current].Next != slot) {
                    current = _slots[current].Next;
                    if (current == None) {
                        // The original walks off the end of an inconsistent chain; stop instead.
                        return;
                    }
                }
                _slots[current].Next = _slots[slot].Next;
            }
        }

        _slots[slot].SpellNumber = None;
    }

    /// <summary>
    /// Drops every effect on an actor — <c>cspell_status_effect_clear_actor</c>.
    ///
    /// <para>Unlike <see cref="Remove"/> this frees the whole chain, so it is the only path that
    /// reliably returns an actor's slots to the pool.</para>
    /// </summary>
    public void ClearActor(Combatant actor) {
        if (actor == null) {
            return;
        }
        for (int slot = actor.ActiveEffectSlot; slot != None; slot = _slots[slot].Next) {
            _slots[slot].SpellNumber = None;
        }
        actor.ActiveEffectSlot = None;
    }

    /// <summary>
    /// The actor's slot holding a given spell, or <see cref="None"/> —
    /// <c>cspell_stat_effect_find_type</c>. This is how the engine asks "is this one already
    /// affected".
    /// </summary>
    public int Find(Combatant actor, int spellNumber) {
        if (actor == null) {
            return None;
        }
        int slot = actor.ActiveEffectSlot;
        if (slot < None || slot >= Capacity) {
            slot = None;
        }
        while (slot != None) {
            if (_slots[slot].SpellNumber == spellNumber) {
                return slot;
            }
            slot = _slots[slot].Next;
        }
        return None;
    }

    /// <summary>
    /// Ages one actor's effects by a round and releases the expired ones —
    /// <c>cspell_actor_tick_status_effects</c>. The arena sweeps every combatant through this.
    ///
    /// <para><b>This is where the head-removal defect bites.</b> The walk captures each node's
    /// successor <i>before</i> releasing it, so when the first effect expires the rest are still
    /// aged and released normally <i>for this round</i> — but the actor's chain head is now -1, so
    /// on the next round the walk starts at nothing. Any effect that survived the round in which
    /// the head expired is stranded: never aged again, never expiring, holding its slot for the
    /// rest of the encounter. That is the mechanism behind the pool exhaustion, and it is why the
    /// leak is invisible until a fight runs long.</para>
    /// </summary>
    /// <returns>
    /// True when the actor must be taken off the field. Only Dannon's Delusions does this: it puts
    /// an illusory combatant on the grid, and on expiry the original fires Final Rest (spell 32) on
    /// it, clears its grid tile and removes it. Those first two are the arena's to perform — this
    /// reports only the verdict.
    /// </returns>
    /// <remarks>
    /// Not modelled: in the 1.02 CD build the original reassigns its <c>actor</c> pointer from
    /// <c>combat_actor_remove</c> mid-loop, so later releases in the same tick act on whichever
    /// combatant shifted down into the removed one's array slot. That is an artifact of removing
    /// from an array in place, not a rule about spells, and reproducing it would mean modelling the
    /// arena's storage rather than its behaviour.
    /// </remarks>
    public bool TickActor(Combatant actor) {
        if (actor == null) {
            return false;
        }

        var actorExpired = false;
        int slot = actor.ActiveEffectSlot;
        while (slot != None) {
            ActiveSpellEffect effect = _slots[slot];
            effect.Duration--;
            int next = effect.Next; // captured before the release can orphan the rest
            if (effect.Duration <= 0) {
                int spellNumber = effect.SpellNumber;
                Remove(actor, slot);
                if (spellNumber == SpellIds.DannonsDelusions) {
                    actorExpired = true;
                }
            }
            slot = next;
        }
        return actorExpired;
    }

    /// <summary>Every effect currently chained to an actor, in order.</summary>
    public IEnumerable<ActiveSpellEffect> EffectsOf(Combatant actor) {
        if (actor == null) {
            yield break;
        }
        for (int slot = actor.ActiveEffectSlot; slot != None; slot = _slots[slot].Next) {
            yield return _slots[slot];
        }
    }

    /// <summary>How many slots are currently in use — for diagnosing the leak above.</summary>
    public int InUse {
        get {
            var count = 0;
            foreach (ActiveSpellEffect slot in _slots) {
                if (slot.SpellNumber != None) {
                    count++;
                }
            }
            return count;
        }
    }
}
