namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BetrayalAtKrondor.Tests.Content;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Which spells fly a projectile at all — the gate that "has a target" is not.
/// </summary>
/// <remarks>
/// <b>Most spells fly nothing.</b> <c>Spell_RunAnimationEffect</c> @0x6782f switches on
/// <c>spellData.animationEffectType</c> across 20 cases and only case 3 calls
/// <c>Spell_ApplyHitWithProjectile</c>. Gating on the presence of a target actor instead — which the
/// port did until 2026-09-02 — gives a whistling missile to every aimed cast, Candle Glow included.
/// </remarks>
public class SpellProjectileArmTests {
    [Fact]
    public void OnlyTheProjectileAnimationFlies() {
        Assert.True(CombatEffectSprite.FliesProjectile(CombatEffectSprite.ProjectileAnimationType));
        foreach (int other in new[] { -1, 0, 1, 2, 4, 5, 15, 17, 19 }) {
            Assert.False(CombatEffectSprite.FliesProjectile(other));
        }
    }

    /// <summary>A cast needs BOTH a destination actor and a projectile animation to sound.</summary>
    [Theory]
    [InlineData(true, CombatEffectSprite.ProjectileAnimationType, true)]
    [InlineData(false, CombatEffectSprite.ProjectileAnimationType, false)]
    [InlineData(true, 2, false)]
    [InlineData(false, 2, false)]
    public void TheCuesNeedBothConditions(bool hasTarget, int animation, bool expected) {
        Assert.Equal(expected, SpellProjectileSound.Flies(hasTarget, animation));
    }

    private static string? SpellsCsv() {
        string? root = GeneratedCorpus.FindDir("DAT");
        string path = root == null ? null! : Path.Combine(root, "DAT", "spells.csv");
        return root != null && File.Exists(path) ? path : null;
    }

    /// <summary>Exactly three shipped spells fly, and the "default" sprite belongs to one of them.</summary>
    /// <remarks>
    /// The mapping's third arm was documented as "every other spell". It is not: with the animation
    /// gate applied, the arm has a single user — <b>The Fetters of Rime</b>. Pinned against the
    /// shipped table so that a data change which adds a flyer is noticed rather than absorbed.
    /// </remarks>
    [Fact]
    public void ThreeShippedSpellsFlyAProjectile() {
        string? csv = SpellsCsv();
        if (csv == null) {
            return;
        }

        string[] lines = File.ReadAllLines(csv);
        string[] header = lines[0].Split(',');
        int idCol = System.Array.IndexOf(header, "Id");
        int nameCol = System.Array.IndexOf(header, "Name");
        int animCol = System.Array.IndexOf(header, "AnimationEffectType");
        Assert.True(idCol >= 0 && nameCol >= 0 && animCol >= 0, "spells.csv lost a column.");

        var flyers = new List<(int Id, string Name)>();
        foreach (string line in lines.Skip(1)) {
            string[] cells = line.Split(',');
            if (cells.Length <= animCol
                || !int.TryParse(cells[animCol], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int anim)) {
                continue;
            }
            if (CombatEffectSprite.FliesProjectile(anim)) {
                flyers.Add((int.Parse(cells[idCol], CultureInfo.InvariantCulture), cells[nameCol]));
            }
        }

        Assert.Equal(3, flyers.Count);
        Assert.Contains(flyers, f => f.Name == "Flamecast");
        Assert.Contains(flyers, f => f.Name == "Bane of Black Slayers");
        Assert.Contains(flyers, f => f.Name == "The Fetters of Rime");

        // ...and the third is the only user of the "default" sprite.
        List<(int Id, string Name)> generic = flyers
            .Where(f => CombatEffectSprite.ForSpell(f.Id) == CombatEffectSprite.GenericSpell)
            .ToList();
        Assert.Single(generic);
        Assert.Equal("The Fetters of Rime", generic[0].Name);
    }
}
