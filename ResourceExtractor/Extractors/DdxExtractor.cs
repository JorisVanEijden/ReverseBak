namespace ResourceExtractor.Extractors;

using System.Text;

internal class DdxExtractor : ExtractorBase {
    public Dialog Extract(string filePath) {
        Log($"Extracting {filePath}");
        _indent = string.Empty;
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var dialog = new Dialog {
            Name = Path.GetFileNameWithoutExtension(filePath)
        };

        var offsetsIds = new Dictionary<int, uint>();
        var numberOfEntries = resourceReader.ReadUInt16();
        for (int i = 0; i < numberOfEntries; i++) {
            var id = resourceReader.ReadUInt32();
            var offset = resourceReader.ReadInt32();
            offsetsIds.Add(offset, id);
        }

        while (resourceReader.BaseStream.Position < resourceReader.BaseStream.Length) {
            int offset = (int)resourceReader.BaseStream.Position;
            var dialogEntry = new DialogEntry {
                Offset = offset,
            };
            if (offsetsIds.TryGetValue(offset, out uint id)) {
                dialogEntry.Id = id;
            }

            var dialogEntryField0 = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField0)}: {dialogEntryField0:X2}");
            dialogEntry.DialogEntry_Field0 = dialogEntryField0;
            var dialogEntryField1 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField1)}: {dialogEntryField1:X4}");
            dialogEntry.DialogEntry_Field1 = dialogEntryField1;
            var dialogEntryField3 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField3)}: {dialogEntryField3:X4}");
            dialogEntry.DialogEntry_Field3 = dialogEntryField3;

            var variantCount = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] VariantCount: {variantCount}");

            var dataItemCount = resourceReader.ReadByte();
            Log($"[{resourceReader.BaseStream.Position:X8}] DataItemCount: {dataItemCount}");

            var stringLength = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] StringLength: {stringLength}");
            for (int i = 0; i < variantCount; i++) {
                Log($"[{resourceReader.BaseStream.Position:X8}] Variant {i}:");
                var unknown2 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown2: {unknown2:X4}");
                var unknown3 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown3: {unknown3:X4}");
                var unknown4 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Unknown4: {unknown4:X4}");
                var variantOffset = resourceReader.ReadInt32();
                Log($"[{resourceReader.BaseStream.Position:X8}] Offset: {variantOffset:X8}");
                var variant = new DialogEntryVariant {
                    Unknown2 = unknown2,
                    Unknown3 = unknown3,
                    Unknown4 = unknown4,
                    Offset = variantOffset
                };
                dialogEntry.Variants.Add(variant);
            }
            Log($"[{resourceReader.BaseStream.Position:X8}] Reading {dataItemCount} data items");
            for (int i = 0; i < dataItemCount; i++) {
                var dataItem = new DialogDataItem();
                dataItem.DataItem_Field0 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Field0: {dataItem.DataItem_Field0:X4}");
                dataItem.DataItem_Field2 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Field2: {dataItem.DataItem_Field2:X4}");
                dataItem.DataItem_Field4 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Field4: {dataItem.DataItem_Field4:X4}");
                dataItem.DataItem_Field6 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Field6: {dataItem.DataItem_Field6:X4}");
                dataItem.DataItem_Field8 = resourceReader.ReadUInt16();
                Log($"[{resourceReader.BaseStream.Position:X8}] Field8: {dataItem.DataItem_Field8:X4}");

                dialogEntry.DataItems.Add(dataItem);
            }
            char[] readChars = resourceReader.ReadChars(stringLength);
            int length = stringLength - 1;
            string text = length > 0 ? new string(readChars)[..length] : string.Empty;
            Log($"[{resourceReader.BaseStream.Position:X8}] Text: '{text}'");

            dialogEntry.Text = text;

            dialog.Entries[dialogEntry.Offset] = dialogEntry;
        }

        foreach (DialogEntry entry in dialog.Entries.Values) {
            foreach (DialogEntryVariant variant in entry.Variants) {
                if (dialog.Entries.TryGetValue(variant.Offset, out DialogEntry? dialogEntry)) {
                    dialogEntry.Referer = entry.Offset;
                }
            }
        }

        return dialog;
    }
}

public class DialogDataItem {
    public int DataItem_Field0 { get; set; }
    public int DataItem_Field2 { get; set; }
    public int DataItem_Field4 { get; set; }
    public int DataItem_Field6 { get; set; }
    public int DataItem_Field8 { get; set; }
    public string FileName { get; set; }
}

public class DialogEntryVariant {
    public int Unknown2 { get; set; }
    public int Unknown3 { get; set; }
    public int Unknown4 { get; set; }
    public int Offset { get; set; }
}

public class DialogEntry {
    public int Offset { get; set; }
    public uint Id { get; set; }
    public string Text { get; set; }
    public int DialogEntry_Field0 { get; set; }
    public int DialogEntry_Field1 { get; set; }
    public int DialogEntry_Field3 { get; set; }
    public List<DialogDataItem> DataItems { get; set; } = new();
    public List<DialogEntryVariant> Variants { get; set; } = new();
    public int Referer { get; set; }
}

public class Dialog : IResource {
    public ResourceType Type { get => ResourceType.DDX; }
    public string Name { get; set; }
    public Dictionary<int, DialogEntry> Entries { get; set; } = new();
}