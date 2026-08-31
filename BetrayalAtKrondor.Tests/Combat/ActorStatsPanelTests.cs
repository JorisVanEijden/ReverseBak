namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using System.Linq;
using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The arena's default panel — <c>combat_actor_draw_stats_panel</c> (CACTOR.C:1608).
/// </summary>
public class ActorStatsPanelTests {
    private static readonly int[] Values = { 41, 33, 7, 25 };

    [Fact]
    public void ItDrawsTheFourPOOLSAndPhysicalNumbers_NoSkills() {
        // Attributes 0..3 and nothing else: it is a status readout, not a character sheet. A port
        // that "helpfully" added Defence or an accuracy would be inventing a row.
        Assert.Equal(
            new[] {
                ActorAttribute.Health, ActorAttribute.Stamina,
                ActorAttribute.Speed, ActorAttribute.Strength,
            },
            ActorStatsPanel.Rows.Select(r => r.Attribute));
    }

    [Fact]
    public void EachStatIsALabelAndAValueOnTheSameRow() {
        IReadOnlyList<HudPanelLine> lines = ActorStatsPanel.Lines("Locklear", Values);

        Assert.Equal(1 + ActorStatsPanel.Rows.Length * 2, lines.Count);
        for (var row = 0; row < ActorStatsPanel.Rows.Length; row++) {
            HudPanelLine label = lines.Single(l => l.Text == ActorStatsPanel.Rows[row].Label);
            HudPanelLine value = lines.Single(l => l.Text == Values[row].ToString());
            Assert.Equal(label.Y, value.Y);
            Assert.Equal(ActorStatsPanel.LabelX, label.X);
            Assert.Equal(ActorStatsPanel.ValueX, value.X);
        }
    }

    [Fact]
    public void TheValueCOLUMNIsNotTheShootPanels() {
        // 0x85 here against 0x99 there. Sharing one constant would move this column 20px right.
        Assert.NotEqual(ShootTargetPanel.ValueX, ActorStatsPanel.ValueX);
        Assert.Equal(0x85, ActorStatsPanel.ValueX);
    }

    [Fact]
    public void TheNameIsCentredAndTheFirstRowSitsFurtherBelowItThanTheRowsAreApart() {
        // y += 0xc after the name, then 0xa between stats -- the name gets more clearance than the
        // rows give each other.
        IReadOnlyList<HudPanelLine> lines = ActorStatsPanel.Lines("Gorath", Values);
        HudPanelLine name = lines[0];

        Assert.Equal(HudPanelAlign.Centre, name.Align);
        Assert.Equal(ShootTargetPanel.CentreX, name.X);
        Assert.Equal(ActorStatsPanel.NameY, name.Y);
        Assert.True(ActorStatsPanel.RowTop(0) - name.Y > ActorStatsPanel.RowStep);
        Assert.Equal(ActorStatsPanel.RowTop(0) + ActorStatsPanel.RowStep, ActorStatsPanel.RowTop(1));
    }

    [Fact]
    public void NothingIsDrawnWithoutAFullSetOfValues() {
        // The original's guard is the character slot; ours is the data that guard implies. Drawing
        // a partial panel would leave rows labelled with no number beside them.
        Assert.Empty(ActorStatsPanel.Lines(null, Values));
        Assert.Empty(ActorStatsPanel.Lines("Owyn", null));
        Assert.Empty(ActorStatsPanel.Lines("Owyn", new[] { 1, 2 }));
    }
}
