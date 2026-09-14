namespace GameData.Resources.Character;

using System;
using System.Collections.Generic;

/// <summary>
/// "Locklear's head swam…" — the dialogs that tell the player a member has caught something:
/// <c>evtcond_pty_dirty_flags_process</c> (EVTCOND.C:362), fed by
/// <c>stat_combatant_apply_condition</c> (STAT.C:355).
/// </summary>
/// <remarks>
/// <b>A flag per member per affliction, not one boolean.</b> Catching something sets global
/// <c>7320 + character*7 + condition</c> and the party-dirty bit. The hourly tick then walks the seven
/// afflictions and, for each, names the active members whose flag is set and counts them, clearing
/// each flag as it reads it. The count is what the dialogs branch on (Var 0: one member, or more).
/// </remarks>
public static class ConditionAnnouncements {
    /// <summary>The first CONDITION global — <c>#define CONDITION(idx) ((idx) + 7320)</c> (GSTATE.H:84).</summary>
    public const int FlagBase = 7320;

    /// <summary>The <c>bPartyDirtyFlags</c> bit a caught affliction sets. Bit 0 is a skill improving.</summary>
    public const int DirtyBit = 2;

    /// <summary>The global recording that one character caught one affliction.</summary>
    public static int FlagFor(int characterIndex, ActorCondition condition) =>
        FlagBase + characterIndex * ActorConditions.Count + (int)condition;

    /// <summary>The dialog announcing an affliction, or null for Drunk and Healing, which have none.</summary>
    public static int? DialogFor(ActorCondition condition) => condition switch {
        ActorCondition.Sick => 0xf6,
        ActorCondition.Plagued => 0x146,
        ActorCondition.Poisoned => 0xf7,
        ActorCondition.Starving => 0x3f,
        ActorCondition.NearDeath => 0x41,
        _ => null,
    };

    /// <summary>One dialog to play and the arguments it names.</summary>
    public readonly struct Announcement {
        public Announcement(int dialogId, int firstActor, int secondActor, int count) {
            DialogId = dialogId;
            FirstActor = firstActor;
            SecondActor = secondActor;
            Count = count;
        }

        public int DialogId { get; }

        /// <summary><c>nEvtArgActor0</c> — the first afflicted member in party order.</summary>
        public int FirstActor { get; }

        /// <summary><c>nEvtArgActor1</c> — the LAST further afflicted member, or -1.</summary>
        public int SecondActor { get; }

        /// <summary><c>nEvtArgCount</c> — how many, which the dialog reads as Var 0.</summary>
        public int Count { get; }
    }

    /// <summary>Read and clear every member's flags, in affliction order, into the dialogs to play.</summary>
    /// <param name="activeParty">Character indices, in party order.</param>
    /// <param name="isSet">Reads a global flag.</param>
    /// <param name="clear">Clears a global flag.</param>
    /// <remarks>
    /// <b>The second actor is the last further member, not the second.</b> The original assigns
    /// <c>nEvtArgActor1</c> on every hit after the first, so with three afflicted the dialog names the
    /// first and the third. Drunk and Healing flags are cleared like the rest and announce nothing.
    /// </remarks>
    public static List<Announcement> Drain(IReadOnlyList<int> activeParty, Func<int, bool> isSet,
        Action<int> clear) {
        var announcements = new List<Announcement>();
        for (var c = 0; c < ActorConditions.Count; c++) {
            int first = -1, second = -1, count = 0;
            foreach (int member in activeParty) {
                int flag = FlagFor(member, (ActorCondition)c);
                if (!isSet(flag)) {
                    continue;
                }
                clear(flag);
                if (first < 0) {
                    first = member;
                } else {
                    second = member;
                }
                count++;
            }
            int? dialog = DialogFor((ActorCondition)c);
            if (count != 0 && dialog.HasValue) {
                announcements.Add(new Announcement(dialog.Value, first, second, count));
            }
        }
        return announcements;
    }
}
