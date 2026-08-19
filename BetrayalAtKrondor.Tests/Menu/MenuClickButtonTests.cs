namespace BetrayalAtKrondor.Tests.Menu;

using GameData.Resources.Menu;
using Xunit;

/// <summary>
/// Which button a click was made with — <c>screen_input_poll_confirm_cancel</c>.
/// </summary>
public class MenuClickButtonTests {
    [Fact]
    public void PrimaryWinsWhenBothAreHeld() {
        // A single if/else-if with the primary first: holding both answers primary, not "both" and
        // not the more recent.
        Assert.Equal(MenuClickButton.Primary,
            MenuClickButton.Resolve(primaryHeld: true, secondaryHeld: true));
    }

    [Fact]
    public void NothingHeldIsItsOwnAnswer() {
        Assert.Equal(MenuClickButton.None,
            MenuClickButton.Resolve(primaryHeld: false, secondaryHeld: false));
        Assert.Equal(MenuClickButton.Secondary,
            MenuClickButton.Resolve(primaryHeld: false, secondaryHeld: true));
    }

    [Fact]
    public void NOTHINGHeldIsNotTheSameAsTheSecondaryButton() {
        // Callers written as "state != 1" lump them together, which is right for them because they
        // are only reached with a button down. A caller that can be reached with none held must not
        // copy that shape.
        Assert.False(MenuClickButton.IsActing(MenuClickButton.None));
        Assert.False(MenuClickButton.IsActing(MenuClickButton.Secondary));
        Assert.True(MenuClickButton.IsActing(MenuClickButton.Primary));
        Assert.NotEqual(MenuClickButton.None, MenuClickButton.Secondary);
    }

    [Fact]
    public void TheKEYPADSubstitutesForBothMouseButtons() {
        // Polled in the same expression as the mouse rather than translated earlier, so a
        // keyboard-only player has both buttons. Reading only real mouse buttons removes that.
        Assert.Contains(0x4c, MenuClickButton.PrimaryScanCodes);   // keypad 5
        Assert.Contains(0x52, MenuClickButton.PrimaryScanCodes);   // keypad 0
        Assert.Contains(0x4e, MenuClickButton.SecondaryScanCodes); // keypad +
        Assert.Empty(System.Linq.Enumerable.Intersect(
            MenuClickButton.PrimaryScanCodes, MenuClickButton.SecondaryScanCodes));
    }
}
