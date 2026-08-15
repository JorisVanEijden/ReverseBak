namespace BetrayalAtKrondor.Tests.Scene;

using System.Collections.Generic;
using GameData.Resources.Animation;
using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// A scene's animation fields are ADS script ids, and the player selects by tag name — so something
/// has to translate, and the failure mode if nothing does is a blank screen rather than an error.
/// </summary>
public class GdsAnimationTagTests {
    private static List<AnimatorScript> Town() => new() {
        new AnimatorScript { Id = 9, Tag = "CAVALL KEEP" },
        new AnimatorScript { Id = 10, Tag = "ARMANGAR" },
        new AnimatorScript { Id = 11, Tag = "SILDEN(CHEAM)" },
        new AnimatorScript { Id = 13, Tag = "DISPLAY" },
    };

    [Fact]
    public void AnEntryTagResolvesToItsTownsArrivalAnimation() {
        // GDS10A asks g_town for entry 10.
        Assert.Equal("ARMANGAR", GdsSceneRules.AnimationTagFor(Town(), 10));
        Assert.Equal("SILDEN(CHEAM)", GdsSceneRules.AnimationTagFor(Town(), 11));
    }

    [Fact]
    public void TheIdleTagIsTheSharedDisplayLoop() {
        // Both town scenes carry idle 13.
        Assert.Equal("DISPLAY", GdsSceneRules.AnimationTagFor(Town(), 13));
    }

    [Fact]
    public void AnUnknownIdResolvesToNothingRatherThanGuessing() {
        Assert.Null(GdsSceneRules.AnimationTagFor(Town(), 99));
    }

    [Fact]
    public void AndSoDoesAnAbsentAnimationResource() {
        Assert.Null(GdsSceneRules.AnimationTagFor(null, 10));
    }

    [Fact]
    public void ZeroMeansTheSceneHasNoSuchAnimation() {
        // ADS ids start at one, so zero cannot name a script; scenes with no transition carry it.
        Assert.False(GdsSceneRules.HasAnimation(0));
        Assert.True(GdsSceneRules.HasAnimation(1));
        Assert.Null(GdsSceneRules.AnimationTagFor(Town(), 0));
    }
}
