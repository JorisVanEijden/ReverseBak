namespace ResourceExtractor.Resources.Dialog;

using System.Text;

internal class DdxStatistics {
    private readonly List<DialogDataItem> _dataItems = new();

    public void Add(Dialog ddx) {
        foreach (DialogEntry entry in ddx.Entries.Values) {
            if (entry.DialogEntry_Field3 != 16) continue;
            foreach (DialogDataItem dataItem in entry.DataItems) {
                dataItem.FileName = ddx.Name;
                _dataItems.Add(dataItem);
            }
        }
    }

    public string? Dump() {
        var sb = new StringBuilder();
        foreach (DialogDataItem dataItem in _dataItems) {
            sb.AppendLine($"type{dataItem.DataItem_Field0:D2},{dataItem.DataItem_Field2:D5},{dataItem.DataItem_Field4:D5},{dataItem.DataItem_Field6:D5},{dataItem.DataItem_Field8:D5},{dataItem.FileName}");
        }
        string dump = sb.ToString();
        File.WriteAllText("dataitemdump.csv", dump);
        return dump;
    }
}