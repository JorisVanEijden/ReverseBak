namespace ResourceExtraction.Extractors.Dialog;

using GameData.Resources.Content;
using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Actions;
using ResourceExtraction.Imaging;

using System.Collections.Generic;
using System.IO;
using System.Text;

public class DdxExtractor : ExtractorBase<Dialog> {
    public override Dialog Extract(string id, Stream resourceStream) {
        Log($"Extracting {id}");
        Indent = string.Empty;
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        var dialog = new Dialog(id);

        var offsetsIds = new Dictionary<int, uint>();
        ushort numberOfEntries = resourceReader.ReadUInt16();
        for (var i = 0; i < numberOfEntries; i++) {
            uint entryId = resourceReader.ReadUInt32();
            int offset = resourceReader.ReadInt32();
            offsetsIds.Add(offset, entryId);
        }

        while (resourceReader.BaseStream.Position < resourceReader.BaseStream.Length) {
            var offset = (int)resourceReader.BaseStream.Position;
            var dialogEntry = new DialogEntry {
                Offset = offset
            };
            if (offsetsIds.TryGetValue(offset, out uint entryId)) {
                dialogEntry.Id = entryId;
            }

            byte dialogType = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogType)}: {dialogType:X2}");
            dialogEntry.DialogType = (DialogType)dialogType;
            short actorNumber = resourceReader.ReadInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(actorNumber)}: {actorNumber:X4}");
            dialogEntry.ActorNumber = actorNumber;
            ushort dialogEntryFlags = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryFlags)}: {dialogEntryFlags:X4}");
            dialogEntry.Flags = (DialogEntryFlags)dialogEntryFlags;

            byte branchCount = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] BranchCount: {branchCount}");

            byte dialogActionCount = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] DialogActionCount: {dialogActionCount}");

            ushort stringLength = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] StringLength: {stringLength}");
            // A ChoiceMenu entry (flag 0x0400) routes its branches through the
            // keyword/menu path, where globalKey is a keyword-label index rather
            // than a condition — DialogBranchFactory decodes accordingly.
            bool isChoiceMenu = dialogEntry.Flags.HasFlag(DialogEntryFlags.ChoiceMenu);
            for (var i = 0; i < branchCount; i++) {
                Log($"[{resourceReader.BaseStream.Position:X8}] Branch {i}:");
                ushort globalKey = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] GlobalKey: {globalKey:X4}");
                ushort unknown3 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown3: {unknown3:X4}");
                ushort unknown4 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown4: {unknown4:X4}");
                long target = resourceReader.ReadUInt32();
                Log($"[{resourceReader.BaseStream.Position:X8}] Offset: {target:X8}");
                dialogEntry.Branches.Add(
                    DialogBranchFactory.Build(isChoiceMenu, globalKey, unknown3, unknown4, target));
            }
            Log($"[{resourceReader.BaseStream.Position:X8}] Reading {dialogActionCount} data items");
            for (var i = 0; i < dialogActionCount; i++) {
                int actionType = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] ActionType: {actionType}");

                DialogActionBase dialogAction = DialogActionFactory.Build(actionType, resourceReader);

                dialogEntry.Actions.Add(dialogAction);
            }
            char[] readChars = resourceReader.ReadChars(stringLength);
            if (stringLength > 1) {
                dialogEntry.Text = new string(readChars)[..(stringLength - 1)];
                Log($"[{resourceReader.BaseStream.Position:X8}] Text: '{dialogEntry.Text}'");
            }

            dialog.Entries.Add(dialogEntry);
        }

        CanonicalSpace.Apply(dialog);
        StampDialogKeys(dialog);
        return dialog;
    }

    private const long IdBit = 0x80000000;

    /// <summary>Post-parse pass (mirrors the TBL <c>StampContentKeys</c> pattern): give every entry a
    /// stable <c>base:ddx:&lt;file&gt;:&lt;offset&gt;</c> key and de-index every branch/push target from
    /// a raw offset/id into that key space. A same-DDX offset → the target entry's offset key; a
    /// bit-31 global id → <c>base:dialog:&lt;id&gt;</c>; the sentinel 0 → null (no continuation).
    /// De-indexes references #3 (branch <c>TargetOffset</c>) and #4 (<c>PushDialogEntry.Offset</c>).</summary>
    private static void StampDialogKeys(Dialog dialog) {
        string file = Path.GetFileNameWithoutExtension(dialog.Id).ToLowerInvariant();

        foreach (DialogEntry entry in dialog.Entries) {
            entry.Key = ContentKey.ForBase($"ddx:{file}", entry.Offset);

            foreach (var branch in entry.Branches) {
                if (branch.TargetOffset is int off) {
                    branch.TargetKey = off == 0 ? null : ContentKey.ForBase($"ddx:{file}", off);
                } else if (branch.TargetId is int bid) {
                    branch.TargetKey = ContentKey.ForBase("dialog", bid);
                }
            }

            foreach (PushDialogEntryAction push in System.Linq.Enumerable.OfType<PushDialogEntryAction>(entry.Actions)) {
                push.TargetKey = TargetKeyForRaw((uint)push.Offset, file);
            }
        }
    }

    // Same 32-bit encoding as a branch target: bit 31 set → global entry id; else raw file offset
    // (0 = sentinel "no continuation").
    private static string? TargetKeyForRaw(uint raw, string file) {
        if (raw >= IdBit) {
            return ContentKey.ForBase("dialog", (int)(raw - IdBit));
        }
        return raw == 0 ? null : ContentKey.ForBase($"ddx:{file}", (int)raw);
    }
}
