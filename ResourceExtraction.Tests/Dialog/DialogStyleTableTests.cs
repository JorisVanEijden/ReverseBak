namespace ResourceExtraction.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

public class DialogStyleTableTests {
    [Fact]
    public void Row2_DefaultArea_IsCanonical() {                 // VGA (13, 11, 294, 101)
        DialogStyle style = DialogStyleTable.Get(2);
        Assert.Equal(65, style.DefaultArea.Left);
        Assert.Equal(66, style.DefaultArea.Top);
        Assert.Equal(1470, style.DefaultArea.Width);
        Assert.Equal(606, style.DefaultArea.Height);
    }

    [Fact]
    public void Row6_DefaultArea_IsCanonical() {                 // VGA (25, 21, 270, 160)
        DialogStyle style = DialogStyleTable.Get(6);
        Assert.Equal(125, style.DefaultArea.Left);
        Assert.Equal(126, style.DefaultArea.Top);
        Assert.Equal(1350, style.DefaultArea.Width);
        Assert.Equal(960, style.DefaultArea.Height);
    }
}
