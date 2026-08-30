namespace BetrayalAtKrondor.Tests.Menu;

using GameData.Resources.Menu;
using Xunit;

/// <summary>
/// Which button a click was made with — <c>screen_input_poll_confirm_cancel</c> (SCREEN.C:114).
/// </summary>
public class MenuClickButtonTests {
    [Fact]
    public void PrimaryWinsWhenBothAreHeld() {
        // A single if/else-if with the primary first, so holding both answers primary rather than
        // "both" or the more recent.
        Assert.Equal(MenuClickButton.Primary,
            MenuClickButton.Resolve(primaryHeld: true, secondaryHeld: true));
        Assert.Equal(MenuClickButton.Secondary,
            MenuClickButton.Resolve(primaryHeld: false, secondaryHeld: true));
        Assert.Equal(MenuClickButton.None,
            MenuClickButton.Resolve(primaryHeld: false, secondaryHeld: false));
    }

    [Fact]
    public void NOTHINGHeldIsNotTheSameAsTheSecondaryButton() {
        // *** The trap this type exists to name. *** A caller written as `state != 1` lumps the two
        // together, which is right for a fixed-object click (only reached with a button down) and
        // wrong for anything reachable with none held.
        Assert.False(MenuClickButton.IsActing(MenuClickButton.None));
        Assert.False(MenuClickButton.IsActing(MenuClickButton.Secondary));
        Assert.True(MenuClickButton.IsActing(MenuClickButton.Primary));
        Assert.NotEqual(MenuClickButton.None, MenuClickButton.Secondary);
    }

    [Fact]
    public void TheKEYPADCarriesBothButtons() {
        // Keypad 5 and 0 poll as the left button and keypad + as the right, in the same expression
        // as the mouse. A port reading only real mouse buttons leaves a keyboard-only player unable
        // to click the world at all — which is what SystemInputSource did until these were wired.
        Assert.Contains(0x4c, MenuClickButton.PrimaryScanCodes);   // keypad 5
        Assert.Contains(0x52, MenuClickButton.PrimaryScanCodes);   // keypad 0
        Assert.Contains(0x4e, MenuClickButton.SecondaryScanCodes); // keypad +
    }

    [Fact]
    public void ThePrimaryAndSecondaryCodesDoNotOverlap() {
        // One key cannot be both buttons; an overlap would make Resolve's primary-first rule
        // swallow the secondary entirely.
        foreach (int code in MenuClickButton.PrimaryScanCodes) {
            Assert.DoesNotContain(code, MenuClickButton.SecondaryScanCodes);
        }
    }
}
