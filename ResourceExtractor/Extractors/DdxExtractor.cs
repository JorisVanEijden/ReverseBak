namespace ResourceExtractor.Extractors;

using System.Text;

internal class DdxExtractor : ExtractorBase {
    private HashSet<int> _offsets = new();
    private Dictionary<int, DialogEntry> _entries = new();

    public Dialog Extract(string filePath) {
        Log($"Extracting {filePath}");
        _offsets = new HashSet<int>();
        _indent = string.Empty;
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var dialog = new Dialog {
            Name = Path.GetFileNameWithoutExtension(filePath)
        };

        var offsets = new Dictionary<int, int>();

        var numberOfEntries = resourceReader.ReadUInt16();
        for (int i = 0; i < numberOfEntries; i++) {
            var id = resourceReader.ReadUInt16();
            dialog.FileNumber = resourceReader.ReadUInt16();
            var offset = resourceReader.ReadInt32();
            offsets.Add(id, offset);
        }
        foreach (KeyValuePair<int, int> entry in offsets) {
            DialogEntry dialogEntry = ReadDialogEntry(entry.Key, entry.Value, resourceReader);
            dialogEntry.Id = entry.Key;
            dialog.Entries.Add(dialogEntry);
        }

        return dialog;
    }

    private DialogEntry ReadDialogEntry(int id, int offset, BinaryReader resourceReader) {
        _indent += "  ";

        _offsets.Add(offset);
        Log($"[{resourceReader.BaseStream.Position:X8}] Moving to offset {offset:X4} for entry {id:X4}");
        resourceReader.BaseStream.Seek(offset, SeekOrigin.Begin);
        var dialogEntry = new DialogEntry {
            Offset = offset,
        };
        _entries[dialogEntry.Offset] = dialogEntry;

        var dialogEntryField0 = resourceReader.ReadByte();
        Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField0)}: {dialogEntryField0:X2}");
        dialogEntry.Field0 = dialogEntryField0;
        var dialogEntryField1 = resourceReader.ReadUInt16();
        Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField1)}: {dialogEntryField1:X4}");
        dialogEntry.Field1 = dialogEntryField1;
        var dialogEntryField3 = resourceReader.ReadUInt16();
        Log($"[{resourceReader.BaseStream.Position:X8}] {nameof(dialogEntryField3)}: {dialogEntryField3:X4}");
        dialogEntry.Field3 = dialogEntryField3;

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
            var unknown4 = resourceReader.ReadInt16();
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
            dataItem.Field0 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] Field0: {dataItem.Field0:X4}");
            dataItem.Field2 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] Field2: {dataItem.Field2:X4}");
            dataItem.Field4 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] Field4: {dataItem.Field4:X4}");
            dataItem.Field6 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] Field6: {dataItem.Field6:X4}");
            dataItem.Field8 = resourceReader.ReadUInt16();
            Log($"[{resourceReader.BaseStream.Position:X8}] Field8: {dataItem.Field8:X4}");

            dialogEntry.DataItems.Add(dataItem);
        }
        char[] readChars = resourceReader.ReadChars(stringLength);
        int length = stringLength - 1;
        string text = length > 0 ? new string(readChars)[..length] : string.Empty;
        Log($"[{resourceReader.BaseStream.Position:X8}] Text: '{text}'");

        dialogEntry.Text = text;

        for (int i = 0; i < variantCount; i++) {
            var variant = dialogEntry.Variants[i];
            if (variant.Offset == 0) {
                Log($"[{resourceReader.BaseStream.Position:X8}] Skipping variant {i} because it has no offset");
                continue;
            }
            if (variant.Offset < 0) {
                Log($"[{resourceReader.BaseStream.Position:X8}] Skipping variant {i} because it has a negative offset: {variant.Offset:X8}");
                continue;
            }
            if (_entries.TryGetValue(variant.Offset, out var subEntry)) {
                Log($"[{resourceReader.BaseStream.Position:X8}] Skipping variant {i} because it has already been read at offset {variant.Offset:X8}");
                variant.SubEntry = subEntry;
                continue;
            }
            if (dialogEntryField3 == 0x14) {
                Log($"[{resourceReader.BaseStream.Position:X8}] Skipping variant {i} because dialogEntryField3 == 0x14");
                continue;
            }
            Log($"[{resourceReader.BaseStream.Position:X8}] Loading variant {i} of entry {id:X4}");
            var newEntry = ReadDialogEntry(i, variant.Offset, resourceReader);
            variant.SubEntry = newEntry;
        }
        _indent = _indent[2..];
        
        return dialogEntry;
    }
}

public class DialogDataItem {
    public int Field0 { get; set; }
    public int Field2 { get; set; }
    public int Field4 { get; set; }
    public int Field6 { get; set; }
    public int Field8 { get; set; }
}

public class DialogEntryVariant {
    public int Unknown2 { get; set; }
    public int Unknown3 { get; set; }
    public int Unknown4 { get; set; }
    public int Offset { get; set; }
    public DialogEntry SubEntry { get; set; }
}

public class DialogEntry {
    public int? Id { get; set; }
    public List<DialogEntryVariant> Variants { get; set; } = new();
    public string Text { get; set; }
    public int Offset { get; set; }
    public string Unknown0 { get; set; }
    public List<DialogDataItem> DataItems { get; set; } = new();
    public int Unknown1 { get; set; }
    public int Field0 { get; set; }
    public int Field1 { get; set; }
    public int Field3 { get; set; }
}

public class Dialog : IResource {
    public string Name { get; set; }
    public List<DialogEntry> Entries { get; set; } = new();
    public ResourceType Type { get => ResourceType.DDX; }
    public int FileNumber { get; set; }
}