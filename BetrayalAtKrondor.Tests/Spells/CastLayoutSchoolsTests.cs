namespace BetrayalAtKrondor.Tests.Spells;

using System;
using System.IO;
using System.Linq;
using System.Text;
using global::GameData.Resources.Menu;
using global::GameData.Resources.Spells;
using global::ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// The cast screen's two REQ layouts, against the shipped files.
/// </summary>
/// <remarks>
/// <b>The layouts are not cosmetic variants — they are what makes a spell reachable.</b> Both files
/// carry the same seven action ids, so nothing about the dispatch changes between them; what differs
/// is which school buttons are <c>Disabled</c>. SPELL.DAT lights schools 0-3 and stones over 4 and 5,
/// REQ_CAST.DAT does the exact opposite, and between them they cover the six exactly once.
///
/// <para>So a port that loads the field layout in a fight does not merely look wrong: it offers the
/// field's two schools to a combatant and puts every combat spell behind a disabled stone. That is
/// what this port did until the two spell lists were compared side by side with the same caster
/// (TASK-367) — the screen looked entirely plausible on its own.</para>
///
/// <para>Read from the shipped REQs rather than restated as constants, because the point of the test
/// is that <see cref="CastMenuSelection.LayoutFor"/> agrees with the data. Two literal sets would
/// agree with each other and prove nothing.</para>
/// </remarks>
public class CastLayoutSchoolsTests {
    static CastLayoutSchoolsTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static UserInterface? Load(string fileName) {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string path = Path.Combine(dir, "OriginalGame", fileName);
            if (File.Exists(path)) {
                using FileStream stream = File.OpenRead(path);
                return new UserInterfaceExtractor().Extract(fileName, stream);
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    private static UserInterface? Load(bool combat) =>
        Load(CastMenuSelection.LayoutFor(combat).ToUpperInvariant());

    /// <summary>The schools whose buttons this layout leaves interactive.</summary>
    private static int[] EnabledSchools(UserInterface ui) =>
        ui.MenuEntries
            .Where(entry => entry.Disabled == 0
                && CastMenuSelection.SchoolForAction(entry.ActionId) >= 0)
            .Select(entry => CastMenuSelection.SchoolForAction(entry.ActionId))
            .OrderBy(school => school)
            .ToArray();

    [Fact]
    public void TheCombatLayoutOffersTheFirstFourSchools() {
        UserInterface? ui = Load(combat: true);
        if (ui == null) {
            return;
        }

        Assert.Equal(new[] { 0, 1, 2, 3 }, EnabledSchools(ui));
    }

    [Fact]
    public void TheFieldLayoutOffersTheOtherTwo() {
        UserInterface? ui = Load(combat: false);
        if (ui == null) {
            return;
        }

        Assert.Equal(new[] { 4, 5 }, EnabledSchools(ui));
    }

    /// <summary>Between them the two layouts reach every school, and neither reaches another's.</summary>
    /// <remarks>
    /// This is the invariant that makes the discriminator load-bearing. If the sets overlapped, the
    /// wrong layout would still offer some of the right spells and the defect would show up as a
    /// short list rather than a wrong one — which is far harder to notice.
    /// </remarks>
    [Fact]
    public void TogetherTheyPartitionTheSixSchools() {
        UserInterface? combat = Load(combat: true);
        UserInterface? field = Load(combat: false);
        if (combat == null || field == null) {
            return;
        }

        int[] inCombat = EnabledSchools(combat);
        int[] inField = EnabledSchools(field);

        Assert.Empty(inCombat.Intersect(inField));
        Assert.Equal(Enumerable.Range(0, CastRingLayout.CategoryCount),
            inCombat.Concat(inField).OrderBy(school => school));
    }

    /// <summary>The exit is live in both — there is always a way out of a cast.</summary>
    [Fact]
    public void BothLayoutsLeaveTheExitLive() {
        foreach (bool combat in new[] { true, false }) {
            UserInterface? ui = Load(combat);
            if (ui == null) {
                continue;
            }

            UiElement exit = Assert.Single(ui.MenuEntries
                .Where(entry => entry.ActionId == CastMenuSelection.ExitActionId));
            Assert.Equal(0, exit.Disabled);
        }
    }

    /// <summary>
    /// A fight will not let you hand the cast to somebody else.
    /// </summary>
    /// <remarks>
    /// SPELL.DAT disables the three party click areas that REQ_CAST.DAT leaves live — the data
    /// saying the caster is whoever's turn it is, matching the combat inventory refusing to switch
    /// members. A port that swapped the layout but kept the field's caster-switch wired would let a
    /// player cast with a character who is not acting.
    /// </remarks>
    [Fact]
    public void OnlyTheFieldLayoutLetsTheCasterBeSwitched() {
        UserInterface? combat = Load(combat: true);
        UserInterface? field = Load(combat: false);
        if (combat == null || field == null) {
            return;
        }

        Assert.All(PartySlotEntries(combat), entry => Assert.NotEqual(0, entry.Disabled));
        Assert.All(PartySlotEntries(field), entry => Assert.Equal(0, entry.Disabled));
    }

    private static UiElement[] PartySlotEntries(UserInterface ui) =>
        ui.MenuEntries
            .Where(entry => CastMenuSelection.PartySlotForAction(entry.ActionId) >= 0)
            .ToArray();
}
