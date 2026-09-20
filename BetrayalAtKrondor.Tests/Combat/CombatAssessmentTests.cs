namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using System.Linq;
using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// What Inspect reveals — <c>combatenc_anim_actor_stat_rolls</c> (CBENC.C:307).
/// </summary>
public class CombatAssessmentTests {
    private static int Value(ActorAttribute a) => 40 + (int)a;

    private static IReadOnlyList<CombatAssessment.Row> AllRows() =>
        CombatAssessment.RowsFor(targetCanShoot: true, targetCanCast: true);

    [Fact]
    public void TheROLLISAGAINSTTheInspectorsAssessment_NotAnythingTheTargetHas() {
        // stat_actor_get(g_current_actor, 8, 0): the skill belongs to whoever is looking. This is
        // the only thing Assessment does in combat, and it had no consumer at all before this.
        Assert.Equal(ActorAttribute.Assessment, CombatAssessment.RollAttribute);
        Assert.True(CombatAssessment.Reveals(roll: 30, inspectorAssessment: 60));
        Assert.False(CombatAssessment.Reveals(roll: 61, inspectorAssessment: 60));
        // `if (roll > stat) return;` -- equality reveals.
        Assert.True(CombatAssessment.Reveals(roll: 60, inspectorAssessment: 60));
    }

    [Fact]
    public void APoorAssessorStillLearnsSomething() {
        // The whole set is re-offered until a row survives, so Assessment 1 is slow rather than
        // blind. Dropping the retry leaves a low-skill inspect showing nothing and spending a turn.
        var rolls = new Queue<int>();
        for (var sweep = 0; sweep < 8 * 3; sweep++) {
            rolls.Enqueue(99);      // three whole sweeps reveal nothing
        }

        IReadOnlyList<(CombatAssessment.Row Row, int Value, int X, int Y)> revealed =
            CombatAssessment.Reveal(AllRows(), inspectorAssessment: 1, Value,
                _ => rolls.Count > 0 ? rolls.Dequeue() : 0);

        Assert.NotEmpty(revealed);
    }

    [Fact]
    public void APerfectAssessorIsStillCappedAtSixRowsOnTheCDBuild() {
        // Eight rows are offered and three fit a column; the third column would start at 0x10e,
        // which the CD guard refuses. So two columns of three is the ceiling.
        IReadOnlyList<(CombatAssessment.Row Row, int Value, int X, int Y)> revealed =
            CombatAssessment.Reveal(AllRows(), inspectorAssessment: 100, Value, _ => 0);

        Assert.Equal(8, AllRows().Count);
        Assert.Equal(6, revealed.Count);
        Assert.Equal(2, revealed.Select(r => r.X).Distinct().Count());
        Assert.Equal(3, revealed.Select(r => r.Y).Distinct().Count());
        Assert.Null(CombatAssessment.PositionOf(6));
    }

    [Fact]
    public void TheColumnsFillTopToBottomThenRight() {
        Assert.Equal((CombatAssessment.FirstColumnX, CombatAssessment.FirstRowY),
            CombatAssessment.PositionOf(0));
        Assert.Equal(
            (CombatAssessment.FirstColumnX, CombatAssessment.FirstRowY + CombatAssessment.RowStep),
            CombatAssessment.PositionOf(1));
        // Fourth row starts the second column at the first row's y again.
        Assert.Equal(
            (CombatAssessment.FirstColumnX + CombatAssessment.ColumnStep, CombatAssessment.FirstRowY),
            CombatAssessment.PositionOf(3));
    }

    [Fact]
    public void TwoRowsAreABSENTRatherThanHiddenWhenTheTargetCannotUseThem() {
        // A creature that cannot shoot never offers a Missile row, even to a perfect assessor --
        // so an empty slot is information about the target, not about the roll.
        IReadOnlyList<CombatAssessment.Row> plain =
            CombatAssessment.RowsFor(targetCanShoot: false, targetCanCast: false);

        Assert.Equal(6, plain.Count);
        Assert.DoesNotContain(plain, r => r.Attribute == ActorAttribute.AccuracyCrossbow);
        Assert.DoesNotContain(plain, r => r.Attribute == ActorAttribute.AccuracyCasting);
        Assert.Contains(plain, r => r.Attribute == ActorAttribute.AccuracyMelee);
    }

    [Fact]
    public void OnlyTheSkillRowsGetAPercentSign() {
        // The pools and the two physical numbers are plain; the four accuracies and Defence are
        // percentages -- the line drawer's `flag` argument, 0 for the first four and 1 after.
        foreach (CombatAssessment.Row row in AllRows()) {
            bool expectPercent = row.Attribute != ActorAttribute.Health
                && row.Attribute != ActorAttribute.Stamina
                && row.Attribute != ActorAttribute.Speed
                && row.Attribute != ActorAttribute.Strength;
            Assert.Equal(expectPercent, row.Percent);
        }
    }

    [Fact]
    public void TheMisspeltLabelIsTheShippedOne() {
        // "Missle:" is misspelt in the binary. Correcting it would be a visible divergence.
        Assert.Contains(AllRows(), r => r.Label == "Missle:");
    }
}

/// <summary>
/// The panel's text — the rows go INSIDE record 0x84, in the blank lines it ends with.
/// </summary>
/// <remarks>
/// TASK-591: the port awaited 0x84 and 0x85 and neither blocks (0x84 is SkipWait, 0x85 is
/// text-less), so the rows were published and cleared inside one frame and 0x84's page was left
/// on screen with no facts and nothing to dismiss it. Composing one page is the fix.
/// </remarks>
public class CombatAssessmentPageTests {
    // Record 0x84's own shape: prose, then the three whitespace-only lines the rows are drawn into.
    private const string Opening = "\t@ studied his opponent.\n\tHe was convinced of certain facts:\n\t \n\t \n\t ";

    private static HudPanelLine Label(string text, int column, int row) =>
        new HudPanelLine(text,
            CombatAssessment.FirstColumnX + (column * CombatAssessment.ColumnStep),
            CombatAssessment.FirstRowY + (row * CombatAssessment.RowStep));

    private static HudPanelLine Value(string text, int column, int row) =>
        new HudPanelLine(text,
            CombatAssessment.FirstColumnX + (column * CombatAssessment.ColumnStep)
            + CombatAssessment.ValueOffsetX,
            CombatAssessment.FirstRowY + (row * CombatAssessment.RowStep));

    [Fact]
    public void TheBlankLinesAreReplacedByTheRows_NotAppendedAfterThem() {
        // The three trailing "\t " lines are the slot, so keeping them would push the numbers
        // three lines below where the original prints them.
        string text = CombatAssessment.ComposePageText(
            Opening, new[] { Label("Health:", 0, 0), Value("26", 0, 0) });

        string[] lines = text.Split('\n');
        Assert.Equal("\tHealth:  26", lines[^1]);
        Assert.DoesNotContain(lines, l => string.IsNullOrWhiteSpace(l));
    }

    [Fact]
    public void ARowsLabelAndValueLandOnOneLine_AndASecondColumnJoinsIt() {
        // The shipped layout is three rows per column and a second column ColumnStep to the right;
        // grouping by y rebuilds it without a second copy of that rule.
        string text = CombatAssessment.ComposePageText(Opening, new[] {
            Label("Health:", 0, 0), Value("26", 0, 0),
            Label("Melee:", 1, 0), Value("45%", 1, 0),
            Label("Stamina:", 0, 1), Value("33", 0, 1),
        });

        string[] lines = text.Split('\n');
        Assert.Equal("\tHealth:  26  Melee:  45%", lines[^2]);
        Assert.Equal("\tStamina:  33", lines[^1]);
    }

    [Fact]
    public void RowsAreOrderedByPositionRatherThanByArrival() {
        // Reveal hands them over in offer order; the page must read top-to-bottom, left-to-right.
        string text = CombatAssessment.ComposePageText(Opening, new[] {
            Label("Stamina:", 0, 1), Value("33", 0, 1),
            Label("Health:", 0, 0), Value("26", 0, 0),
        });

        string[] lines = text.Split('\n');
        Assert.Equal("\tHealth:  26", lines[^2]);
        Assert.Equal("\tStamina:  33", lines[^1]);
    }

    [Fact]
    public void AnInspectionThatRevealedNothingKeepsTheProseAndAddsNoBlankTail() {
        // Reveal cannot normally return empty -- it re-offers the set until a row survives -- but
        // the page must not render a ragged empty box if it ever does.
        string text = CombatAssessment.ComposePageText(Opening, new HudPanelLine[0]);

        Assert.EndsWith("certain facts:", text);
    }
}
