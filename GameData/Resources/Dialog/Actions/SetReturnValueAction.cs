namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Action type 21. Terminates the dialog (sets the engine's internal
/// <c>done_</c> flag) and stores <see cref="Value"/> as the value the
/// outer <c>ExecuteDialog</c> call returns to its caller. How menu-style
/// dialogs propagate an integer choice back to the game code that opened
/// them.
/// </summary>
public class SetReturnValueAction : DialogActionBase {
    public int Value { get; set; }
    public int Field2 { get; set; }
    public int Field4 { get; set; }
    public int Field6 { get; set; }
}
