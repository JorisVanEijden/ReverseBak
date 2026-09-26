namespace GameData.Resources.Combat;

using GameData.Resources.Animation;
using GameData.Resources.Spells;

/// <summary>The one-shot sequences a combat cast plays — <c>cspell_invoke_effect</c>'s drawing arms
/// plus the spell-id special cases in <c>cspell_resolve_cast</c>.</summary>
public enum SpellVisualKind {
    None,
    /// <summary>The world (not the HUD) jumps toward a palette colour, holds, and fades back
    /// (CSPELL.C:336).</summary>
    PaletteFlash,
    /// <summary>Four lightning frames on the target, then a flash toward colour 3 (CSPELL.C:609).</summary>
    StormFlash,
    /// <summary>25 sparks with gravity and one bounce at the target (WORLDFX.C:281).</summary>
    SparkBurst,
    /// <summary>25 motes spiralling in on the target (WORLDFX.C:137).</summary>
    FluxVortex,
    /// <summary>Two expanding rings around a palette flash (CSPELL.C:828, WORLDFX.C:92).</summary>
    ParticleBlast,
    /// <summary>The target sinks through the ground, 2 px a frame (CSPELL.C:628, WORLDFX.C:485).</summary>
    Sink,
    /// <summary>The whirlwind spins on the target for 100 frames — Winds of Eortis on an immune
    /// target (CSPELL.C:824, WORLDFX.C:256).</summary>
    Whirlwind,
    /// <summary>The whirlwind flies from caster to target, growing out of the ground over the first
    /// quarter — Winds of Eortis's ordinary path, shape 4 at speed 100 (CSPELL.C:653-669,
    /// WORLDHIT.C:585).</summary>
    WhirlwindFlight,
    /// <summary>One Evil Seek hop: shapes 5, 3, 3 fly source to target at random speeds, then the
    /// target flickers white/red for four frames (CSPELL.C:456-484). Raised per hop by the chain.</summary>
    HopBurst,
    /// <summary>Stepping onto crystal ground: the crystal run lights up as a crackling beam for ten
    /// frames while the walker flashes white, cue 0x45 each frame — the terrain-3 arm's short cine
    /// (CMBTAI.C:221-225, CACTOR.C:1078, beam WORLDFX.C:539).</summary>
    CrystalZap,
    /// <summary>Strength Drain: shape 0x26 rolls to the target, which turns white, and rolls back
    /// (CSPELL.C:441-454).</summary>
    Rebound,
}

/// <summary>What a combat actor shows every frame while a lingering spell sits on it — the per-frame
/// arms of <c>CACTOR.C:903-1076</c>, which switch on the status's spell record.</summary>
public enum LingeringVisualKind {
    None,
    /// <summary>1–3 jagged bolts from the actor up to the sky, re-drawn every frame
    /// (WORLDFX.C:376, CACTOR.C:939/1063).</summary>
    Lightning,
    /// <summary>A 280x280x400 box around the actor whose edges cycle through the 0xD0 band
    /// (WORLDFX.C:417).</summary>
    WireBox,
    /// <summary>Six fresh twinkles a frame around the body (WORLDFX.C:36).</summary>
    Sparkle,
    /// <summary>A coloured glow behind the sprite plus 0xD0 sparkles (WORLDFX.C:519, CACTOR.C:1056).</summary>
    Halo,
}

/// <summary>A one-shot visual: what to play, and in which palette colour.</summary>
/// <param name="Colour">A palette index; 0 on a spark burst means "random per spark" (WORLDFX.C:217).</param>
/// <param name="Spread">The spark burst's velocity spread, in world units per frame.</param>
/// <param name="Tint">The remap the struck target is drawn through while the burst plays (1 red,
/// 3 white; 0 none) — <c>cspell_apply_hit_at</c>'s knockback frame, held 10 frames (CSPELL.C:411-435).</param>
public readonly record struct SpellVisual(SpellVisualKind Kind, int Colour = 0, int Spread = 0, int Tint = 0) {
    public static readonly SpellVisual None = new(SpellVisualKind.None);
}

/// <summary>A lingering visual. <paramref name="Colour"/> is a palette index for the sparkle, or the
/// remap table (1 red, 2 green, 3 white, 4 blue) for a halo.</summary>
public readonly record struct LingeringVisual(LingeringVisualKind Kind, int Colour = 0);

/// <summary>
/// Which visual a cast plays, and what a lingering effect looks like — engine-free, so the choice is
/// testable and the Unity side only draws.
/// </summary>
/// <remarks>
/// <b>The per-frame look belongs to the STATUS's spell, not the spell being cast.</b> The original
/// adds a status, renders N frames and removes it; the renderer then looks up the status's record.
/// That is why Fetters of Rime shows Grief's white halo (it registers Grief on hit) and why a
/// Nightfingers steal sparkles. <see cref="Lingering"/> is keyed the same way.
/// </remarks>
public static class SpellVisuals {
    /// <summary>A combat VFX frame: <c>world_render_with_overlay</c> sets the frame countdown to 14
    /// and busy-waits it out, and the countdown drops once per IRQ0 (WORLDHIT.C:479-496,
    /// TIMER.ASM). About 59 ms, so every combat effect runs at ~17 fps.</summary>
    public const int FrameIrqTicks = 0xe;

    /// <summary>The IRQ0 period the frame countdown runs on — 4x faster than the animation tick.</summary>
    public static double IrqSeconds => GameTick.SecondsPerTick / GameTick.Irq0sPerTick;

    /// <summary>Seconds per combat VFX frame.</summary>
    public static double FrameSeconds => FrameIrqTicks * IrqSeconds;

    /// <summary>Colour index the storm's closing flash tints toward (CSPELL.C:625).</summary>
    public const int StormFlashColour = 3;

    /// <summary>Firestorm forces its sparks to 0xD0 whatever its record says (CSPELL.C:524).</summary>
    public const int FirestormColour = 0xd0;

    /// <summary>The colour every halo's sparkles use (CACTOR.C:1058).</summary>
    public const int HaloSparkleColour = 0xd0;

    /// <summary>The one-shot visual for a cast that landed.</summary>
    /// <param name="magnitude">The cast's delivered magnitude; widens a projectile's spark burst.</param>
    /// <remarks>
    /// Spell-id cases first, because <c>cspell_resolve_cast</c> handles them before the tail and
    /// several set <c>pSpell = 0</c> so no kind arm runs. Kind 3's flight is drawn by
    /// <c>ProjectileFlight</c>; this adds the impact burst that follows it.
    /// </remarks>
    public static SpellVisual OneShot(int spellId, Spell spell, int magnitude) {
        // Evil Seek's hops are raised by the chain itself, one per victim, with their own source.
        if (spellId == SpellIds.StrengthDrain) {
            return new SpellVisual(SpellVisualKind.Rebound);
        }
        if (spell == null) {
            return SpellVisual.None;
        }
        int colour = spell.EffectSubject;
        switch (spell.AnimationEffectType) {
            case 2:
                return new SpellVisual(SpellVisualKind.PaletteFlash, colour);
            case 3:
                return new SpellVisual(SpellVisualKind.SparkBurst, colour, ProjectileSpread(spellId, magnitude),
                    ProjectileTint(spellId));
            case 4:
                return new SpellVisual(SpellVisualKind.StormFlash);
            case 9:
                return new SpellVisual(SpellVisualKind.FluxVortex, colour);
            case 12:
                return new SpellVisual(SpellVisualKind.Whirlwind);
            case 13:
                return new SpellVisual(SpellVisualKind.ParticleBlast, colour);
            case 16:
                return new SpellVisual(SpellVisualKind.Sink);
            case 19:
                return new SpellVisual(SpellVisualKind.SparkBurst, FirestormColour, (magnitude >> 2) + 10,
                    ProjectileTint(spellId));
            default:
                // 0 is a fade no shipped spell uses; 5-8, 14, 15 have no dispatcher arm and only
                // draw while a status sits on the actor (see Lingering); 11, 17, 18 are walks,
                // summons and a flee with no drawing of their own.
                return SpellVisual.None;
        }
    }

    /// <summary>Spark spread for a projectile's impact — <c>cspell_apply_hit_at</c> (CSPELL.C:411-425).</summary>
    public static int ProjectileSpread(int spellId, int magnitude) => spellId switch {
        SpellIds.Flamecast => 0x23,
        SpellIds.BaneOfBlackSlayers => (magnitude >> 2) + 0x14,
        _ => (magnitude >> 2) + 10,
    };

    /// <summary>The struck target's tint on impact: red for Flamecast, white for everything else
    /// (CSPELL.C:411-425).</summary>
    public static int ProjectileTint(int spellId) => spellId == SpellIds.Flamecast ? 1 : 3;

    /// <summary>Frames the impact tint holds — <c>knockbackTimer = 10</c> (CSPELL.C:435).</summary>
    public const int ImpactTintFrames = 10;

    /// <summary>What an actor carrying a lingering effect of this spell shows each frame.</summary>
    /// <remarks>Kinds 3/19 (burst particles), 9/13 (orbit), 12 and 16 also have per-frame arms, but
    /// only ever while their one-shot runs — they are drawn by <see cref="OneShot"/>'s sequence. Kind
    /// 8's shake and 14's pose are unreachable: neither spell adds its own status.</remarks>
    public static LingeringVisual Lingering(Spell spell) {
        if (spell == null) {
            return default;
        }
        return spell.AnimationEffectType switch {
            4 => new LingeringVisual(LingeringVisualKind.Lightning),
            5 => new LingeringVisual(LingeringVisualKind.WireBox),
            6 => new LingeringVisual(LingeringVisualKind.Sparkle, spell.EffectSubject),
            15 => new LingeringVisual(LingeringVisualKind.Halo, spell.EffectSubject),
            _ => default,
        };
    }
}
