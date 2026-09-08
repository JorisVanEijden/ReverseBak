namespace GameData.Resources.Spells;

using System;

/// <summary>
/// The CUES a spell's effect arm makes — <c>cspell_invoke_effect</c> (CSPELL.C:786) switched on
/// <c>nEffect_kind</c>, which the port calls <see cref="Spell.AnimationEffectType"/>.
/// </summary>
/// <remarks>
/// <b>The audio of these arms does not need their visuals.</b> Cue 69 proved the shape: the sound
/// belongs to the EVENT, and the event is ported even when the picture is not. Two arms are pure
/// sequence and are modelled here; the rest of TASK-117's arms are drawing work.
///
/// <para><b>Both need a STOP, which is why this could not be wired earlier.</b> Each starts a cue
/// once and ends it explicitly — a fire-and-forget play cannot express that, and
/// <c>MenuSoundService</c> had no stop until this went in.</para>
/// </remarks>
public static class SpellEffectArmSound {
    /// <summary>Skyfire's arm — <c>cspell_storm_flash_sequence</c> (CSPELL.C:609).</summary>
    public const int StormFlashKind = 4;

    /// <summary>Mind Melt's arm — the particle-blast-and-flash case (CSPELL.C:836).</summary>
    public const int ParticleBlastKind = 13;

    /// <summary><c>sound_static</c> (0x11). Started ONCE and held across the whole sequence.</summary>
    public const int StaticCue = 0x11;

    /// <summary><c>sound_thunder</c> (0x15). One crack per flash.</summary>
    public const int ThunderCue = 0x15;

    /// <summary><c>sound_logostar</c> (6). Mind Melt's opening.</summary>
    public const int LogostarCue = 6;

    /// <summary>Flashes in a storm sequence — four, and the loop is a <c>do/while</c>.</summary>
    public const int StormFlashCount = 4;

    /// <summary>Exclusive bound of the gap between flashes: <c>RND(0x28)</c> ticks.</summary>
    public const int FlashGapTickBound = 0x28;

    /// <summary>Whether this effect kind has a sound sequence modelled here.</summary>
    public static bool HasSequence(int animationEffectType) =>
        animationEffectType == StormFlashKind || animationEffectType == ParticleBlastKind;

    /// <summary>Ticks to wait before the next flash.</summary>
    /// <remarks>
    /// Re-rolled per flash, so the storm is deliberately irregular rather than metronomic. Zero is
    /// a legal roll — two cracks can land together.
    /// </remarks>
    public static int FlashGapTicks(Func<int, int> rnd) =>
        rnd == null ? 0 : rnd(FlashGapTickBound);
}
