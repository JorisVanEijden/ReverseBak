namespace ResourceExtraction;

using System;
using System.Collections.Generic;
using System.Text;
using GameData;
using GameData.Resources.Character;
using GameData.Resources.Data;
using GameData.Resources.World;

/// <summary>Result of writing a save: the full SAVE##.GAM bytes + which body bytes we authored.</summary>
public readonly record struct SaveGameWriteResult(byte[] Bytes, SaveCoverage Coverage);


/// <summary>
/// Writes a byte-interchangeable SAVE##.GAM (100-byte header + patched TEMP.GAM body). Preserve-and-
/// patch: copies the backing body and overwrites only the fields we model, so unmodeled regions stay
/// byte-identical to what a real engine wrote. Symmetric to <see cref="Extractors.SaveGameExtractor"/>.
/// </summary>
public static class SaveGameWriter {
    private const int DosCodePage = 437;

    public static SaveGameWriteResult Write(
        byte[] backingBody, in SaveGameFields fields,
        string name, short headerWorldX, short headerWorldY, short mapIcon,
        IReadOnlyList<DirtyContainerEdit> containerEdits = null,
        IReadOnlyList<DirtyActorEdit> actorEdits = null,
        IReadOnlyList<DirtyCombatantEdit> combatantEdits = null,
        IReadOnlyList<SaveGameTimerData> timers = null,
        EncounterVisitTable automapVisits = null,
        EncounterObjectStates encounterActorStates = null,
        IReadOnlyList<GameData.Resources.Character.ActorStatModifiers.Slot> statModifiers = null,
        short? lastSeenStepSpeed = null,
        short? lastSeenGridStride = null,
        IReadOnlyDictionary<int, int> globalFlagEdits = null,
        IReadOnlyList<DirtyRosterActorEdit> rosterActorEdits = null) {
        if (backingBody is null) {
            throw new ArgumentNullException(nameof(backingBody));
        }
        if (backingBody.Length != SaveGameOffsets.BodySize) {
            throw new ArgumentException(
                $"Backing body must be {SaveGameOffsets.BodySize} bytes, was {backingBody.Length}.",
                nameof(backingBody));
        }

        // Patch a copy of the backing body; record every authored range.
        byte[] body = (byte[])backingBody.Clone();
        var coverage = new ByteCoverage();

        void PatchI16(int offset, short value) {
            BitConverter.GetBytes(value).CopyTo(body, offset);
            coverage.Add(offset, sizeof(short));
        }
        void PatchI32(int offset, int value) {
            BitConverter.GetBytes(value).CopyTo(body, offset);
            coverage.Add(offset, sizeof(int));
        }
        void PatchU8(int offset, byte value) {
            body[offset] = value;
            coverage.Add(offset, 1);
        }

        // The active party. Size and members are written together because they are one fact: a size
        // that disagrees with the array is a party the engine reads past the end of.
        if (fields.ActiveParty != null) {
            if (fields.ActiveParty.Length > SaveGameOffsets.ActivePartySlots) {
                throw new ArgumentException(
                    $"The active party holds at most {SaveGameOffsets.ActivePartySlots} characters, "
                    + $"was {fields.ActiveParty.Length}.", nameof(fields));
            }
            PatchU8(SaveGameOffsets.ActivePartySize, (byte)fields.ActiveParty.Length);
            for (var slot = 0; slot < fields.ActiveParty.Length; slot++) {
                PatchU8(SaveGameOffsets.ActivePartyMembers + slot, fields.ActiveParty[slot]);
            }
            // Slots past the party's size are LEFT ALONE, not zeroed: the engine reads only the
            // first `size` of them, and zeroing would claim character 0 sits in the spare slots.
        }

        PatchI16(SaveGameOffsets.Chapter, fields.Chapter);
        PatchI32(SaveGameOffsets.PartyGold, fields.PartyGold);
        PatchI32(SaveGameOffsets.GameTime, fields.GameTime);
        PatchI32(SaveGameOffsets.TimeSnapshot, fields.TimeSnapshot);
        // Which spell palette effects are running. Derived from the timer pool every tick, but the
        // original stores it too — so a save restored mid-effect shows it at once instead of waiting
        // for the clock to move, which on a stationary party is a long time.
        PatchI16(SaveGameOffsets.PaletteEventMask, fields.PaletteEventMask);
        // Offsets 14, 15 and 17 — cross-checked against canassa's gstate.inc. Offset 16 between
        // them is rsvd_10, a genuine reserved byte, so it stays in passthrough rather than being
        // written as a zero we cannot justify.
        PatchU8(SaveGameOffsets.PartyDeathState, fields.PartyDeathState);
        PatchU8(SaveGameOffsets.ChapterTransitionPending, fields.ChapterTransitionPending);
        PatchU8(SaveGameOffsets.PreviousZone, fields.PreviousZone);
        PatchU8(SaveGameOffsets.CurrentZone, fields.CurrentZone);
        PatchU8(SaveGameOffsets.WorldX, fields.WorldX);
        PatchU8(SaveGameOffsets.WorldY, fields.WorldY);
        PatchI32(SaveGameOffsets.PositionX, fields.PositionX);
        PatchI32(SaveGameOffsets.PositionY, fields.PositionY);
        PatchI32(SaveGameOffsets.PositionZ, fields.PositionZ);
        PatchI16(SaveGameOffsets.Rotation, fields.Rotation);
        if (fields.MapCameraZ.HasValue) {
            PatchI32(SaveGameOffsets.MapCameraZ, fields.MapCameraZ.Value);
        }

        // *** THE CHANGE-DETECTOR BASELINE, AND IT IS ONLY WRITTEN WHEN THE CALLER HAS ONE. ***
        // The body is cloned, so leaving these alone already round-trips whatever was there. They
        // are patchable because the baseline has to MOVE: the roaming-encounter reset fires on the
        // step speed having grown since the game last looked, and then stores the new value —
        // whether or not it fired. Never storing it would re-fire the reset on every single apply.
        if (lastSeenStepSpeed.HasValue) {
            PatchI16(SaveGameOffsets.LastSeenStepSpeed, lastSeenStepSpeed.Value);
        }
        if (lastSeenGridStride.HasValue) {
            PatchI16(SaveGameOffsets.LastSeenGridStride, lastSeenGridStride.Value);
        }

        // The dungeon automap's marks. The block is written whole rather than per-mark because
        // that is how the original does it too (gstate_temp_file_write_at over the entire 0x668),
        // and because a mark is a bit in a bitmap the party keeps adding to — there is no smaller
        // unit to patch. Body offset, not the 0xb3b a save FILE shows: see EncounterVisitTable.
        if (automapVisits != null && automapVisits.Save(body, EncounterVisitTable.BodyOffset)) {
            coverage.Add(EncounterVisitTable.BodyOffset, EncounterVisitTable.SaveSize);
        }

        // What happened to each encounter's actors — the block that makes a killed roaming group
        // stay killed. Written whole for the same reason the automap is: the original writes the
        // ref pair's 0x1a4 span in one go, and a removal is a 12-byte record inside a run of them
        // rather than a field with a patchable address of its own.
        if (encounterActorStates != null
            && encounterActorStates.Save(body, EncounterObjectStates.BodyOffset)) {
            coverage.Add(EncounterObjectStates.BodyOffset, EncounterObjectStates.SaveSize);
        }

        // The eight timed stat modifiers per party member. Written whole, like the two blocks
        // above, because a slot is a 14-byte record in a fixed table rather than a field with an
        // address of its own.
        //
        // *** THIS EXISTS BECAUSE READING A STAT CAN FREE A SLOT. *** An expired modifier is zeroed
        // by the read that notices it (GameSession.PartyEffectsFor); without writing the block back
        // that expiry is lost, the slot returns on the next load and expires again forever — and,
        // worse, keeps occupying one of the eight against a new modifier.
        if (statModifiers != null
            && GameData.Resources.Character.ActorStatModifiers.Save(
                statModifiers, body, GameData.Resources.Character.ActorStatModifiers.BodyOffset)) {
            coverage.Add(GameData.Resources.Character.ActorStatModifiers.BodyOffset,
                GameData.Resources.Character.ActorStatModifiers.BlockSize);
        }

        // *** THE STORY FLAGS. *** Until 2026-08-25 nothing wrote these at all — the session kept
        // them in an in-memory overlay and a save simply dropped it, so every flag a dialog set
        // (618 low writes and 144 high ones across the shipped corpus: who you have spoken to, what
        // you have been told) lasted only until you saved. See TASK-210.
        //
        // The edits are applied ONTO the bytes already in the body rather than replacing them: the
        // overlay holds only what changed this session, and the rest is the loaded save's own state.
        if (globalFlagEdits != null && globalFlagEdits.Count > 0) {
            var low = new byte[SaveGameOffsets.GlobalFlagsSize];
            var high = new byte[SaveGameOffsets.GlobalFlags2Size];
            Array.Copy(body, SaveGameOffsets.GlobalFlags, low, 0, low.Length);
            Array.Copy(body, SaveGameOffsets.GlobalFlags2, high, 0, high.Length);

            var wrote = false;
            foreach (KeyValuePair<int, int> edit in globalFlagEdits) {
                // A "flag" that is really a game-state field is not ours to write here — it has its
                // own home in the header fields above (GameStateEventFields).
                if (GameData.Resources.GameState.GlobalFlagLayout.TryWrite(
                        low, high, edit.Key, edit.Value != 0)) {
                    wrote = true;
                }
            }

            if (wrote) {
                low.CopyTo(body, SaveGameOffsets.GlobalFlags);
                high.CopyTo(body, SaveGameOffsets.GlobalFlags2);
                coverage.Add(SaveGameOffsets.GlobalFlags, SaveGameOffsets.GlobalFlagsSize);
                coverage.Add(SaveGameOffsets.GlobalFlags2, SaveGameOffsets.GlobalFlags2Size);
            }
        }

        if (containerEdits != null) {
            foreach (DirtyContainerEdit edit in containerEdits) {
                // The header goes down first so NumberOfItems below stays authoritative — a
                // claimed ground bag rewrites zone/chapter band/world item id/x/y/residence, and
                // every other edit leaves HeaderBytes null and patches items only.
                if (edit.HeaderBytes != null) {
                    for (int i = 0; i < edit.HeaderBytes.Length; i++) {
                        PatchU8(edit.BodyOffset + i, edit.HeaderBytes[i]);
                    }
                }
                PatchU8(edit.BodyOffset + ContainerGeometry.NumberOfItemsOffset, edit.NumberOfItems);
                int arrayOff = edit.BodyOffset + ContainerGeometry.ItemArrayOffset;
                for (int i = 0; i < edit.LiveItemBytes.Length; i++) {
                    PatchU8(arrayOff + i, edit.LiveItemBytes[i]);
                }
                if (edit.ShopOffset >= 0 && edit.ShopBytes != null) {
                    for (int i = 0; i < edit.ShopBytes.Length; i++) {
                        PatchU8(edit.BodyOffset + edit.ShopOffset + i, edit.ShopBytes[i]);
                    }
                }
                if (edit.TimestampOffset >= 0) {
                    PatchI32(edit.BodyOffset + edit.TimestampOffset, edit.Timestamp);
                }
            }
        }

        // Live party state. Without this, anything that changes an actor at runtime — upkeep,
        // healing, skill advancement — would be applied and then silently lost on save.
        // The combat block: one 22-byte CombatantState per actor, 1730 of them. Patched per slot
        // like every other edit, so an untouched slot stays byte-identical to what the engine wrote.
        if (combatantEdits != null) {
            foreach (DirtyCombatantEdit edit in combatantEdits) {
                if (edit.ActorSlot < 0 || edit.ActorSlot >= SaveGameOffsets.CombatSlotCount) {
                    throw new ArgumentOutOfRangeException(nameof(combatantEdits),
                        $"Actor slot {edit.ActorSlot} is outside the {SaveGameOffsets.CombatSlotCount} combat records.");
                }
                if (edit.Record == null) {
                    continue;
                }

                int at = SaveGameOffsets.CombatDataOffset
                    + (edit.ActorSlot * Extractors.CombatRecordWriter.RecordSize);
                byte[] record = Extractors.CombatRecordWriter.ToBytes(edit.Record);
                record.CopyTo(body, at);
                coverage.Add(at, record.Length);
            }
        }

        if (rosterActorEdits != null) {
            foreach (DirtyRosterActorEdit edit in rosterActorEdits) {
                if (edit.ActorSlot < 0 || edit.ActorSlot >= SaveGameOffsets.RosterActorCount) {
                    throw new ArgumentOutOfRangeException(nameof(rosterActorEdits),
                        $"Actor slot {edit.ActorSlot} is outside the {SaveGameOffsets.RosterActorCount}-entry actor table.");
                }
                if (edit.Stats == null) {
                    continue;
                }

                // The same attribute quintuples as a party record — identical layout, different
                // section. The original writes the WHOLE 95 bytes back for a surviving enemy
                // (SaveEncounterNpcsToTempGam, IDA 0x63265); patching the attribute block is the
                // same picture while combat changes nothing outside it.
                int recordOffset = SaveGameOffsets.RosterActors
                    + edit.ActorSlot * SaveGameOffsets.PartyActorStride
                    + SaveGameOffsets.ActorAttributesInRecord;
                int attributes = Math.Min(edit.Stats.Length, SaveGameOffsets.ActorAttributeCount);
                for (int i = 0; i < attributes; i++) {
                    ActorStat stat = edit.Stats[i];
                    if (stat == null) {
                        continue;
                    }
                    int at = recordOffset + i * SaveGameOffsets.ActorAttributeStride;
                    PatchU8(at + 0, stat.Max);
                    PatchU8(at + 1, stat.Base);
                    PatchU8(at + 2, stat.Effective);
                    PatchU8(at + 3, stat.Experience);
                    PatchU8(at + 4, unchecked((byte)stat.Modifier));
                }
            }
        }

        if (actorEdits != null) {
            foreach (DirtyActorEdit edit in actorEdits) {
                if (edit.CharacterIndex < 0 || edit.CharacterIndex >= SaveGameOffsets.PartyActorCount) {
                    throw new ArgumentOutOfRangeException(nameof(actorEdits),
                        $"Character index {edit.CharacterIndex} is outside the six party records.");
                }

                if (edit.Stats != null) {
                    int recordOffset = SaveGameOffsets.PartyActors
                        + edit.CharacterIndex * SaveGameOffsets.PartyActorStride
                        + SaveGameOffsets.ActorAttributesInRecord;
                    int attributes = Math.Min(edit.Stats.Length, SaveGameOffsets.ActorAttributeCount);
                    for (int i = 0; i < attributes; i++) {
                        ActorStat stat = edit.Stats[i];
                        if (stat == null) {
                            continue;
                        }
                        int at = recordOffset + i * SaveGameOffsets.ActorAttributeStride;
                        PatchU8(at + 0, stat.Max);
                        PatchU8(at + 1, stat.Base);
                        PatchU8(at + 2, stat.Effective);
                        PatchU8(at + 3, stat.Experience);
                        PatchU8(at + 4, unchecked((byte)stat.Modifier));
                    }
                }

                if (edit.KnownSpells != null) {
                    int spellsOffset = SaveGameOffsets.PartyActors
                        + edit.CharacterIndex * SaveGameOffsets.PartyActorStride
                        + SaveGameOffsets.ActorKnownSpellsInRecord;
                    int words = Math.Min(edit.KnownSpells.Length, SaveGameOffsets.ActorKnownSpellWords);
                    for (int i = 0; i < words; i++) {
                        PatchI16(spellsOffset + i * 2, unchecked((short)edit.KnownSpells[i]));
                    }
                }

                if (edit.Conditions != null) {
                    int ranksOffset = SaveGameOffsets.ActorStatusEffects
                        + edit.CharacterIndex * SaveGameOffsets.ActorStatusEffectsStride;
                    for (int i = 0; i < SaveGameOffsets.ActorStatusEffectCount; i++) {
                        PatchU8(ranksOffset + i, (byte)edit.Conditions[(ActorCondition)i]);
                    }
                }
            }
        }

        // Pending timers. Without this a temporary dialog flag or a scheduled clear would be
        // dropped on save and the flag would stay set for good — the corpse-flavour flag 8127 is
        // the shipped example.
        if (timers != null) {
            int live = Math.Min(timers.Count, SaveGameOffsets.TimerSlots);
            PatchI16(SaveGameOffsets.TimerPoolCount, (short)live);
            for (int i = 0; i < SaveGameOffsets.TimerSlots; i++) {
                int at = SaveGameOffsets.TimerPool + i * SaveGameOffsets.TimerStride;
                if (i < live) {
                    SaveGameTimerData t = timers[i];
                    PatchU8(at + 0, (byte)t.Type);
                    PatchU8(at + 1, (byte)t.Flag);
                    PatchI16(at + 2, t.Key);
                    PatchI32(at + 4, t.Time);
                } else {
                    // Blank the unused slots so a shorter pool cannot leave a stale timer behind
                    // the count for something else to pick up.
                    for (int b = 0; b < SaveGameOffsets.TimerStride; b++) {
                        PatchU8(at + b, 0);
                    }
                }
            }
        }

        // 100-byte header, then the patched body.
        byte[] output = new byte[SaveGameOffsets.HeaderSize + SaveGameOffsets.BodySize];
        WriteFixedLengthString(output, SaveGameOffsets.HeaderName, SaveGameOffsets.HeaderNameLength, name);
        BitConverter.GetBytes(fields.Chapter).CopyTo(output, SaveGameOffsets.HeaderChapter);
        BitConverter.GetBytes(headerWorldX).CopyTo(output, SaveGameOffsets.HeaderWorldX);
        BitConverter.GetBytes(headerWorldY).CopyTo(output, SaveGameOffsets.HeaderWorldY);
        BitConverter.GetBytes(mapIcon).CopyTo(output, SaveGameOffsets.HeaderMapIcon);
        BitConverter.GetBytes(SaveGame.SupportedVersion).CopyTo(output, SaveGameOffsets.HeaderVersion);
        Buffer.BlockCopy(body, 0, output, SaveGameOffsets.HeaderSize, body.Length);

        var cov = new SaveCoverage(SaveGameOffsets.BodySize, coverage.AuthoredBytes, coverage.Ranges);
        return new SaveGameWriteResult(output, cov);
    }

    // NUL-padded fixed-length CP437 field (mirror of the reader's ReadFixedLengthString).
    private static void WriteFixedLengthString(byte[] dest, int offset, int length, string value) {
        byte[] encoded = Encoding.GetEncoding(DosCodePage).GetBytes(value ?? string.Empty);
        int n = Math.Min(encoded.Length, length);
        Array.Copy(encoded, 0, dest, offset, n);
        // remaining bytes stay 0 (NUL) — dest is fresh.
    }
}
