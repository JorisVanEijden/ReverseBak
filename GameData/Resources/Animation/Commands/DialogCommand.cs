namespace GameData.Resources.Animation.Commands;

public class DialogCommand : FrameCommand {
    public int Dialog16Id { get; set; }
    public int Arg2 { get; set; }

    public override string ToString() {
        return $"{nameof(DialogCommand)}({Dialog16Id}, {Arg2});";
    }

    /*
     * when arg1 is 0 and arg2 is 255, draw a border 14,10 to 291,103 and draw dialog.scr background inside the area
     * when arg1 is -1, do some drawing with the bitmap in slot 2
     * when arg1 is 0 and arg2 is between 0 and 20, do some math and something with the image in slot1
     * when arg1 is not 0 or -1, then arg2 determines the type of dialog to show(?) and arg1 +1600000 is the dialog id
     */
}