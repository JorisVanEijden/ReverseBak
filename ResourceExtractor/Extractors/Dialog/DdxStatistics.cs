namespace ResourceExtractor.Extractors.Dialog;

using ResourceExtractor.Resources.Dialog;
using ResourceExtractor.Resources.Dialog.Actions;

using System.Text;

internal class DdxStatistics {
    private readonly List<SetTextVariableAction> _items = [];

    public void Add(Dialog ddx) {
        foreach (DialogEntry entry in ddx.Entries.Values) {
            IEnumerable<SetTextVariableAction> actions = entry.DialogActions.OfType<SetTextVariableAction>();
            foreach (SetTextVariableAction action in actions) {
                _items.Add(action);
            }
        }
    }

    public string Dump() {
        var sb = new StringBuilder();
        foreach (SetTextVariableAction item in _items) {
            sb.AppendLine($"{item.Slot}\t{item.Source}\t{item.Unknown}");
        }
        string dump = sb.ToString();
        File.WriteAllText("data.csv", dump);
        return dump;
    }
}