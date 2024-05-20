namespace ResourceExtractor.Extractors.Dialog;

using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Actions;

using System.Text;

internal class DdxExtractor : ExtractorBase {
    public Dialog Extract(string filePath, FileStream fileStream) {
        Log($"Extracting {filePath}");
        Indent = string.Empty;
        using var resourceReader = new BinaryReader(fileStream, Encoding.GetEncoding(DosCodePage));

        var dialog = new Dialog(Path.GetFileName(filePath));

        var offsetsIds = new Dictionary<int, uint>();
        ushort numberOfEntries = resourceReader.ReadUInt16();
        for (var i = 0; i < numberOfEntries; i++) {
            uint id = resourceReader.ReadUInt32();
            int offset = resourceReader.ReadInt32();
            offsetsIds.Add(offset, id);
        }

        while (resourceReader.BaseStream.Position < resourceReader.BaseStream.Length) {
            var offset = (int)resourceReader.BaseStream.Position;
            var dialogEntry = new DialogEntry {
                Offset = offset
            };
            if (offsetsIds.TryGetValue(offset, out uint id)) {
                dialogEntry.Id = id;
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
            for (var i = 0; i < branchCount; i++) {
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

        return dialog;
    }
}