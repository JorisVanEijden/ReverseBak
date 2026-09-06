namespace BetrayalAtKrondor.Tests.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using global::GameData.Resources.Spells;
using global::ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// Pins SPELLS.DAT's field ORDER against the real shipped file.
/// </summary>
/// <remarks>
/// <b>Why this shape of test.</b> <see cref="SpellExtractor"/> reads the record straight down with no
/// field tags — <c>MinimumCost, MaximumCost, IsMartial, TargetingType, EffectSubject,
/// AnimationEffectType, ObjectId, Calculation, Damage, Duration</c> — so inserting, removing or
/// reordering a single read shifts EVERY field after it, and does so silently. Nothing would throw;
/// spells would simply acquire other spells' numbers.
///
/// <para>The names are the anchor that makes this detectable. They come from a separate offset table
/// and the trailing string block, so they survive a shift in the numeric reads. Asserting a name
/// TOGETHER WITH its numbers is therefore a real check rather than the extractor agreeing with
/// itself.</para>
///
/// <para><b>The kind==5 pair below is the point.</b> On 2026-09-06 an analysis read the original's
/// <c>nEffect_kind</c> gate as this model's <see cref="Spell.TargetingType"/>. It is
/// <see cref="Spell.AnimationEffectType"/> — one field later — and the two select DIFFERENT SPELLS,
/// which sent the analysis after the wrong three. Both sets are asserted here so the distinction is
/// executable rather than a paragraph in a note.
/// See docs/re-notes/2026-09-05-spelldef-field-mapping.md.</para>
/// </remarks>
public class SpellRecordLayoutTests {
    static SpellRecordLayoutTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static SpellList? LoadShippedSpells() {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string path = Path.Combine(dir, "OriginalGame", "SPELLS.DAT");
            if (File.Exists(path)) {
                using FileStream stream = File.OpenRead(path);
                return new SpellExtractor().Extract("SPELLS.DAT", stream);
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null; // shipped data absent (CI)
    }

    [Fact]
    public void FieldOrderHoldsAgainstTheShippedFile() {
        SpellList? spells = LoadShippedSpells();
        if (spells == null) {
            return;
        }

        Assert.Equal(45, spells.Spells.Count);

        // Names anchor the ids; a shifted numeric read cannot move them.
        Assert.Equal("Dragon's Breath", spells.Spells[0].Name);
        Assert.Equal("Flamecast", spells.Spells[4].Name);
        Assert.Equal("Hocho's Haven", spells.Spells[6].Name);
        Assert.Equal("Mirrorwall", spells.Spells[14].Name);

        // One whole row, so a shift by one in EITHER direction breaks something here.
        Spell flamecast = spells.Spells[4];
        Assert.Equal(1, flamecast.MinimumCost);
        Assert.Equal(20, flamecast.MaximumCost);
        Assert.True(flamecast.IsMartial);
        Assert.Equal(0, flamecast.TargetingType);
        Assert.Equal(200, flamecast.EffectSubject);
        Assert.Equal(3, flamecast.AnimationEffectType);
        Assert.Equal(-1, flamecast.ObjectId);
        Assert.Equal(3, flamecast.Damage);
        Assert.Equal(0, flamecast.Duration);
    }

    [Fact]
    public void TargetingTypeAndAnimationEffectTypeSelectDifferentSpells() {
        SpellList? spells = LoadShippedSpells();
        if (spells == null) {
            return;
        }

        // nSpell_kind == 5 — decides how EffectSubject is read (a tile-effect id here).
        List<int> targetingFive = spells.Spells
            .Where(kv => kv.Value.TargetingType == 5).Select(kv => kv.Key).OrderBy(i => i).ToList();

        // nEffect_kind == 5 — what the original's combat renderer switches on for the actor's
        // wireframe box (combat_actor_rndr_stat_vfx_pre, CACTOR.C:909).
        List<int> effectKindFive = spells.Spells
            .Where(kv => kv.Value.AnimationEffectType == 5).Select(kv => kv.Key).OrderBy(i => i)
            .ToList();

        Assert.Equal(new[] { 14, 29, 40 }, targetingFive);      // Mirrorwall, Gambit, Asphyxiation
        Assert.Equal(new[] { 6, 14 }, effectKindFive);          // Hocho's Haven, Mirrorwall

        // The overlap is Mirrorwall alone. If these two ever come out equal, the extractor is
        // reading one field twice and the distinction this guards has silently collapsed.
        Assert.NotEqual(targetingFive, effectKindFive);
    }
}
