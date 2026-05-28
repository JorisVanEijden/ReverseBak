namespace GameData.Resources.Dialog.Branches;

/// <summary>
/// A branch of a <c>ChoiceMenu</c> entry (entry flag <c>0x0400</c>). The engine
/// decides which choices appear via <c>EvaluateDialogCondition</c>, and the raw
/// <c>unknown3</c>/<c>unknown4</c> bytes are unused (constant <c>1</c>/<c>-1</c>
/// in the data). <see cref="Keyword"/> is the menu label index — the
/// <c>KEYWORD.DAT</c> entry at <c>Keyword - 1</c>.
/// </summary>
public class KeywordChoiceBranch : DialogBranchBase {
    public int Keyword { get; set; }
}
