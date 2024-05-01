namespace GameData.Resources.Dialog.Actions;

public class GiveItemAction : DialogActionBase {
    public int ObjectId { get; set; }
    public int Actor { get; set; }
    public int Amount { get; set; }
    public int Cost { get; set; }
}