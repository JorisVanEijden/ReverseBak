namespace BetrayalAtKrondor.Tests.Combat;

using System;
using System.Linq;
using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>TASK-117: which visual a cast plays, and the WORLDFX.C particle rules.</summary>
public class SpellVisualsTests {
    private static Spell Kind(int kind, int subject = 0) =>
        new("t") { AnimationEffectType = kind, EffectSubject = subject };

    [Theory]
    [InlineData(2, SpellVisualKind.PaletteFlash)]
    [InlineData(3, SpellVisualKind.SparkBurst)]
    [InlineData(4, SpellVisualKind.StormFlash)]
    [InlineData(9, SpellVisualKind.FluxVortex)]
    [InlineData(12, SpellVisualKind.Whirlwind)]
    [InlineData(13, SpellVisualKind.ParticleBlast)]
    [InlineData(16, SpellVisualKind.Sink)]
    [InlineData(19, SpellVisualKind.SparkBurst)]
    [InlineData(5, SpellVisualKind.None)]  // lingering only
    [InlineData(6, SpellVisualKind.None)]  // lingering only
    [InlineData(7, SpellVisualKind.None)]  // Eagle Wing: no handler anywhere
    [InlineData(-1, SpellVisualKind.None)]
    public void EachDispatcherArmPicksItsVisual(int kind, SpellVisualKind expected) =>
        Assert.Equal(expected, SpellVisuals.OneShot(0, Kind(kind), 0).Kind);

    [Fact]
    public void TheArmsColourIsTheRecordsEffectSubject() {
        Assert.Equal(0xaf, SpellVisuals.OneShot(0, Kind(2, 0xaf), 0).Colour);
        // Firestorm forces 0xD0 whatever its record says.
        Assert.Equal(0xd0, SpellVisuals.OneShot(0, Kind(19, 5), 0).Colour);
    }

    [Fact]
    public void SpellIdCasesWinOverTheKind() {
        Assert.Equal(SpellVisualKind.Rebound, SpellVisuals.OneShot(SpellIds.StrengthDrain, Kind(-1), 0).Kind);
        // Evil Seek's hops come from the chain, one per victim, not from the cast.
        Assert.Equal(SpellVisualKind.None, SpellVisuals.OneShot(SpellIds.EvilSeek, Kind(-1), 0).Kind);
    }

    [Fact]
    public void ProjectileSpreadFollowsTheSpellIdNotTheKind() {
        Assert.Equal(35, SpellVisuals.ProjectileSpread(SpellIds.Flamecast, 100));
        Assert.Equal(100 / 4 + 20, SpellVisuals.ProjectileSpread(SpellIds.BaneOfBlackSlayers, 100));
        Assert.Equal(100 / 4 + 10, SpellVisuals.ProjectileSpread(SpellIds.FettersOfRime, 100));
    }

    [Theory]
    [InlineData(4, LingeringVisualKind.Lightning)]
    [InlineData(5, LingeringVisualKind.WireBox)]
    [InlineData(6, LingeringVisualKind.Sparkle)]
    [InlineData(15, LingeringVisualKind.Halo)]
    [InlineData(8, LingeringVisualKind.None)] // unreachable: neither spell adds its own status
    [InlineData(3, LingeringVisualKind.None)]
    public void LingeringLookFollowsTheStatusSpellsKind(int kind, LingeringVisualKind expected) =>
        Assert.Equal(expected, SpellVisuals.Lingering(Kind(kind)).Kind);

    [Fact]
    public void ACombatFrameIsFourteenIrqTicksAbout59ms() =>
        Assert.InRange(SpellVisuals.FrameSeconds, 0.058, 0.060);

    [Fact]
    public void SparksFallBounceOnceAndDie() {
        var rng = new Random(1);
        var burst = new SpellParticles.SparkBurst(35, n => rng.Next(n));
        Assert.Equal(SpellParticles.Count, burst.Points.Count());
        int frames = 0;
        while (burst.Step()) {
            frames++;
            Assert.All(burst.Points, p => Assert.True(p.Z >= 0));
            Assert.True(frames < 200, "a burst must end");
        }
        Assert.Empty(burst.Points);
        Assert.InRange(frames, 5, 60);
    }

    [Fact]
    public void TheVortexClosesInAndEnds() {
        var rng = new Random(2);
        var vortex = SpellParticles.Orbit.Vortex(n => rng.Next(n));
        float Widest() => vortex.Points.Max(p => MathF.Sqrt(p.X * p.X + p.Y * p.Y));
        float start = Widest();
        Assert.InRange(start, 250, 400);
        int frames = 0;
        while (vortex.Step()) {
            frames++;
            Assert.All(vortex.Points, p => Assert.InRange(p.Z, 100, 350));
        }
        Assert.Empty(vortex.Points);
        Assert.InRange(frames, 35, 330); // 175..324 units at 1..5 a frame
    }

    [Fact]
    public void TheBlastRingDoublesForTenFrames() {
        var blast = SpellParticles.Orbit.Blast(_ => 0);
        int frames = 0;
        while (blast.Step()) {
            frames++;
        }
        Assert.Equal(10, frames);
    }

    [Fact]
    public void TheBoxPenCyclesTheD0Band() {
        Assert.Equal(0xd0, SpellParticles.BoxColour(0, front: false));
        Assert.Equal(0xd1, SpellParticles.BoxColour(0, front: true));
        Assert.Equal(0xd0, SpellParticles.BoxColour(7, front: false));
        Assert.Equal(12, SpellParticles.BoxEdges.Length);
    }
}
