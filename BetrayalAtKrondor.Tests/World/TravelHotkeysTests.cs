namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The travel HUD's letters, which are not a key table but a scancode coincidence.
/// </summary>
/// <remarks>
/// REQ_MAIN's action ids ARE DOS scancodes, so the original dispatches the scancode the keyboard
/// produced straight into menupage_run. These pin the pairs against the ids the REQ actually carries
/// — if one drifts, the letter silently starts pressing a different button (TASK-584).
/// </remarks>
public class TravelHotkeysTests {
    [Theory]
    [InlineData('M', 50)]   // the local map
    [InlineData('E', 18)]   // encamp
    [InlineData('C', 46)]   // cast spell
    [InlineData('R', 19)]   // follow road
    [InlineData('O', 24)]   // options
    [InlineData('B', 48)]   // bookmark
    public void ALetterNamesTheActionWhoseIdIsItsScancode(char letter, int actionId) {
        Assert.Equal(actionId, TravelHotkeys.ActionFor(letter));
    }

    [Fact]
    public void TheCaseOfTheLetterDoesNotMatter() {
        // The adapter emits lower case; a human writing the table reaches for upper.
        Assert.Equal(TravelHotkeys.ActionFor('M'), TravelHotkeys.ActionFor('m'));
    }

    [Theory]
    [InlineData('A')]
    [InlineData('Z')]
    [InlineData('1')]
    [InlineData(' ')]
    public void ALetterTheHudHasNoButtonForNamesNothing(char letter) {
        // menupage_run fires only actions its page carries, so an unmatched scancode must do
        // nothing rather than fall through to a neighbouring id.
        Assert.Equal(TravelHotkeys.NoAction, TravelHotkeys.ActionFor(letter));
    }

    [Fact]
    public void TheMapLetterIsTheONEActionTheMapScreenAlsoCloses_On() {
        // The toggle only works because both pages carry an entry for the same id: the travel loop
        // opens on 0x32 (WORLDLP.C:316) and the map screen closes on 0x32 (MAP.C:398).
        Assert.Equal(GameData.Resources.World.LocalMapScreen.MapAction.Close,
            GameData.Resources.World.LocalMapScreen.ActionFor(TravelHotkeys.ActionFor('M')));
    }
}
