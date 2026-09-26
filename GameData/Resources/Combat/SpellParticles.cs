namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>A particle position in original world units, relative to the tile centre: X/Y on the
/// ground, Z up.</summary>
public readonly record struct ParticlePoint(float X, float Y, float Z);

/// <summary>
/// The combat spell particle systems of <c>WORLDFX.C</c>, stepped one original combat frame at a
/// time (<see cref="SpellVisuals.FrameSeconds"/>). Engine-free: the Unity side only draws the
/// points, so the motion rules are testable here.
/// </summary>
/// <remarks><c>rnd(n)</c> is the original's <c>RND(n)</c>: uniform in <c>[0, n)</c>.</remarks>
public static class SpellParticles {
    /// <summary><c>VFX_PARTICLE_COUNT</c>.</summary>
    public const int Count = 25;

    /// <summary>A full turn in the original's angle units (a 16-bit heading).</summary>
    public const double FullTurn = 0x10000;

    private static int Range(Func<int, int> rnd, int lo, int hi) => lo + rnd(hi - lo + 1);

    /// <summary>
    /// The damage spark burst — <c>worldfx_combat_damage_ptcl_burst</c> and its update
    /// (WORLDFX.C:281-374): 25 sparks from z=250, gravity 6 a frame, one bounce at half speed if
    /// they land faster than 72, otherwise they die on the ground.
    /// </summary>
    public sealed class SparkBurst {
        private readonly int[] _x = new int[Count], _y = new int[Count], _z = new int[Count];
        private readonly int[] _vx = new int[Count], _vy = new int[Count], _vz = new int[Count];

        public SparkBurst(int spread, Func<int, int> rnd) {
            spread = Math.Max(1, spread);
            for (var i = 0; i < Count; i++) {
                _z[i] = 0xfa;
                _vx[i] = rnd(spread) - (spread >> 1);
                _vy[i] = rnd(spread) - (spread >> 1);
                _vz[i] = Range(rnd, 10, 69);
            }
        }

        /// <summary>Sparks still in the air (a spark with z &lt; 0 is spent).</summary>
        public IEnumerable<ParticlePoint> Points {
            get {
                for (var i = 0; i < Count; i++) {
                    if (_z[i] > -1) {
                        yield return new ParticlePoint(_x[i], _y[i], _z[i]);
                    }
                }
            }
        }

        /// <summary>Each live spark's ground shadow — a 1-px dot in pen 0x18 at z=0 under it
        /// (<c>worldfx_render_star</c>, WORLDFX.C:209-212).</summary>
        public IEnumerable<ParticlePoint> Shadows {
            get {
                for (var i = 0; i < Count; i++) {
                    if (_z[i] > -1) {
                        yield return new ParticlePoint(_x[i], _y[i], 0);
                    }
                }
            }
        }

        /// <summary>The shadow pen.</summary>
        public const int ShadowPen = 0x18;

        /// <summary>Advance one frame; false once every spark is spent.</summary>
        public bool Step() {
            var active = false;
            for (var i = 0; i < Count; i++) {
                if (_z[i] <= -1) {
                    continue;
                }
                active = true;
                _x[i] += _vx[i];
                _y[i] += _vy[i];
                _z[i] += _vz[i];
                _vz[i] -= 6;
                if (_z[i] <= 0) {
                    if (_vz[i] > -0x48) {
                        _z[i] = -1;
                    } else {
                        _z[i] = 0;
                        _vz[i] = -_vz[i] >> 1;
                    }
                }
            }
            return active;
        }
    }

    /// <summary>
    /// Motes in polar form around the target — the flux vortex (WORLDFX.C:73-171) and the particle
    /// blast ring (WORLDFX.C:92-135) share one pool and one draw.
    /// </summary>
    public sealed class Orbit {
        private readonly int[] _radius = new int[Count];
        private readonly short[] _angle = new short[Count];
        private readonly int[] _height = new int[Count];
        private readonly Func<int, int> _rnd;
        private int _blastRadius;
        private readonly bool _blast;

        private Orbit(Func<int, int> rnd, bool blast) {
            _rnd = rnd;
            _blast = blast;
        }

        /// <summary>The vortex: radius 250..399, heights 0..354, spiralling in until every mote is
        /// within 75.</summary>
        public static Orbit Vortex(Func<int, int> rnd) {
            var o = new Orbit(rnd, blast: false);
            for (var i = 0; i < Count; i++) {
                o._radius[i] = Range(rnd, 250, 399);
                o._angle[i] = (short)(i == 0 ? rnd(1600) : o._angle[i - 1] + rnd(4000));
                o._height[i] = rnd(355);
            }
            return o;
        }

        /// <summary>The blast: a ring at height 300 whose radius doubles every frame from 2 until it
        /// passes 600 — ten frames.</summary>
        public static Orbit Blast(Func<int, int> rnd) {
            var o = new Orbit(rnd, blast: true) { _blastRadius = 1 };
            for (var i = 0; i < Count; i++) {
                o._radius[i] = 1 + 0x4b;
                o._angle[i] = (short)(i == 0 ? rnd(0x640) : o._angle[i - 1] + rnd(4000));
                o._height[i] = 300;
            }
            return o;
        }

        /// <summary>Motes still drawn: radius above 75.</summary>
        public IEnumerable<ParticlePoint> Points {
            get {
                for (var i = 0; i < Count; i++) {
                    if (_radius[i] > 0x4b) {
                        double a = _angle[i] / FullTurn * 2 * Math.PI;
                        yield return new ParticlePoint(
                            (float)(_radius[i] * Math.Cos(a)), (float)(_radius[i] * Math.Sin(a)), _height[i]);
                    }
                }
            }
        }

        /// <summary>
        /// A "zap" this frame: each drawn mote within 200 has a 1-in-50 chance of striking instead of
        /// being drawn — the target flashes red and cue 4 plays (WORLDFX.C:196-205).
        /// </summary>
        public bool RollZap() {
            for (var i = 0; i < Count; i++) {
                if (_radius[i] > 0x4b && _radius[i] < 200 && _rnd(50) == 0) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Advance one frame; false when the effect has finished.</summary>
        public bool Step() {
            if (_blast) {
                if (_blastRadius >= 600) {
                    return false;
                }
                _blastRadius *= 2;
                for (var i = 0; i < Count; i++) {
                    _radius[i] = _blastRadius + 0x4b;
                }
                return true;
            }
            var active = false;
            for (var i = 0; i < Count; i++) {
                // RNDR(1300, 1599) is rolled per mote, in the caller's argument list.
                int spin = Range(_rnd, 1300, 1599);
                if (_radius[i] <= 0x4b) {
                    continue;
                }
                _radius[i] -= Range(_rnd, 1, 5);
                _angle[i] = (short)(_angle[i] + spin + ((0xfa - _radius[i]) << 4));
                _height[i] = Math.Min(0x15e, Math.Max(0x64, _height[i] + 0x14 - _rnd(0x28)));
                active = true;
            }
            return active;
        }
    }

    /// <summary>Six fresh twinkles around the body — <c>worldfx_sparkle_burst</c> (WORLDFX.C:36):
    /// x ±150, y 0..149, z 0..349; one in four is drawn as a "+".</summary>
    public static IEnumerable<(ParticlePoint At, bool Cross)> Sparkles(Func<int, int> rnd) {
        for (var i = 0; i < 6; i++) {
            var at = new ParticlePoint(rnd(300) - 150, rnd(150), rnd(350));
            yield return (at, rnd(4) == 0);
        }
    }

    /// <summary>
    /// One lightning bolt from the actor's chest (z=250) straight up — <c>worldfx_render_flame_at_actor</c>
    /// (WORLDFX.C:376). The original walks screen pixels in 10 px rises, each ending ±10 px sideways;
    /// here the rise is expressed as a fraction of the bolt's height so the renderer picks the scale.
    /// </summary>
    /// <returns>Vertices as (sideways offset in screen px, fraction of the way up, 0..1).</returns>
    public static List<(int SidewaysPx, float Up)> Bolt(Func<int, int> rnd, int segments) {
        var v = new List<(int, float)> { (3 - rnd(6), 0f) };
        for (var s = 1; s <= segments; s++) {
            v.Add((10 - rnd(20), s / (float)segments));
        }
        return v;
    }

    /// <summary>Bolts drawn this frame: 1 + RND2(2) before the sprite and RND2(2) after
    /// (CACTOR.C:939-945, 1063-1067).</summary>
    public static int BoltCount(Func<int, int> rnd) => 1 + rnd(2) + rnd(2);

    /// <summary>The wire box's eight corners (WORLDFX.C:411), ±140 around the tile, 0..400 high.</summary>
    public static readonly ParticlePoint[] BoxCorners = {
        new(-140, 140, 0), new(140, 140, 0), new(140, 140, 400), new(-140, 140, 400),
        new(-140, -140, 400), new(-140, -140, 0), new(140, -140, 0), new(140, -140, 400),
    };

    /// <summary>The box's twelve edges, as index pairs into <see cref="BoxCorners"/>.</summary>
    public static readonly (int A, int B)[] BoxEdges = {
        (6, 1), (2, 7), (0, 5), (3, 4), (0, 1), (0, 3), (2, 3), (2, 1), (4, 5), (4, 7), (6, 7), (6, 5),
    };

    /// <summary>The box's pen at a given age: it walks 0xD0..0xD6 and the front edges run one
    /// ahead (WORLDFX.C:442, 461).</summary>
    public static int BoxColour(int age, bool front) => (age % 7) + (front ? 0xd1 : 0xd0);
}
