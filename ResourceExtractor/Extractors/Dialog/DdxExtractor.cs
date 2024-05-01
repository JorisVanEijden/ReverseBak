namespace ResourceExtractor.Extractors.Dialog;

using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Actions;

using System.Text;

internal class DdxExtractor : ExtractorBase {
    public Dialog Extract(string filePath) {
        Log($"Extracting {filePath}");
        Indent = string.Empty;
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var dialog = new Dialog {
            Name = Path.GetFileNameWithoutExtension(filePath)
        };

        var offsetsIds = new Dictionary<int, uint>();
        ushort numberOfEntries = resourceReader.ReadUInt16();
        for (int i = 0; i < numberOfEntries; i++) {
            uint id = resourceReader.ReadUInt32();
            int offset = resourceReader.ReadInt32();
            offsetsIds.Add(offset, id);
        }

        while (resourceReader.BaseStream.Position < resourceReader.BaseStream.Length) {
            int offset = (int)resourceReader.BaseStream.Position;
            var dialogEntry = new DialogEntry {
                Offset = offset
            };
            if (offsetsIds.TryGetValue(offset, out uint id)) {
                dialogEntry.Id = id;
            }

            byte dialogEntryField0 = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField0)}: {dialogEntryField0:X2}");
            dialogEntry.DialogEntry_Field0 = dialogEntryField0;
            ushort dialogEntryField1 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField1)}: {dialogEntryField1:X4}");
            dialogEntry.DialogEntry_Field1 = dialogEntryField1;
            ushort dialogEntryField3 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField3)}: {dialogEntryField3:X4}");
            dialogEntry.DialogEntry_Field3 = dialogEntryField3;

            byte branchCount = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] BranchCount: {branchCount}");

            byte dialogActionCount = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] DialogActionCount: {dialogActionCount}");

            ushort stringLength = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] StringLength: {stringLength}");
            for (int i = 0; i < branchCount; i++) {
                Log($"[{resourceReader.BaseStream.Position:X8}] Branch {i}:");
                ushort unknown2 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown2: {unknown2:X4}");
                ushort unknown3 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown3: {unknown3:X4}");
                short unknown4 = resourceReader.ReadInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown4: {unknown4:X4}");
                long target = resourceReader.ReadUInt32();
                Log($"[{resourceReader.BaseStream.Position:X8}] Offset: {target:X8}");
                var branch = new DialogEntryBranch {
                    GlobalKey = unknown2,
                    Unknown3 = unknown3,
                    Unknown4 = unknown4,
                };
                if (target >= 0x80000000) {
                    branch.TargetId = (int)(target - 0x80000000);
                } else {
                    branch.TargetOffset = (int)target;
                }
                dialogEntry.Branches.Add(branch);
            }
            Log($"[{resourceReader.BaseStream.Position:X8}] Reading {dialogActionCount} data items");
            for (int i = 0; i < dialogActionCount; i++) {
                int actionType = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] ActionType: {actionType}");

                DialogActionBase dialogAction = DialogActionFactory.Build(actionType, resourceReader);

                dialogEntry.Actions.Add(dialogAction);
            }
            char[] readChars = resourceReader.ReadChars(stringLength);
            int length = stringLength - 1;
            string text = length > 0 ? new string(readChars)[..length] : string.Empty;
            Log($"[{resourceReader.BaseStream.Position:X8}] Text: '{text}'");

            dialogEntry.Text = text;

            dialog.Entries[dialogEntry.Offset] = dialogEntry;
        }

        foreach (DialogEntry entry in dialog.Entries.Values) {
            foreach (DialogEntryBranch branch in entry.Branches) {
                if (branch.TargetOffset.HasValue && dialog.Entries.TryGetValue(branch.TargetOffset.Value, out DialogEntry? dialogEntry)) {
                    dialogEntry.Referer = entry.Offset;
                }
                else if (branch.TargetId.HasValue && dialog.Entries.TryGetValue(branch.TargetId.Value, out dialogEntry)) {
                    dialogEntry.Referer = entry.Offset;
                }
            }
        }

        return dialog;
    }
}