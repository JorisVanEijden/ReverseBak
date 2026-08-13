namespace BetrayalAtKrondor.Tests.Audio;

using GameData.Resources.Audio;
using Xunit;

/// <summary>
/// audio_music_play. The sentinels are the part a port gets wrong: -999 asks, -1 silences, and
/// re-requesting what is already playing deliberately does nothing.
/// </summary>
public class MusicPlaybackTests {
    [Fact]
    public void MinusNineNineNineAsksWithoutChangingAnything() {
        MusicPlayback.MusicChange change = MusicPlayback.Resolve(MusicPlayback.QueryOnly, 1026);

        Assert.Equal(MusicPlayback.MusicAction.None, change.Action);
        Assert.Equal(1026, change.PreviousTrack);
    }

    [Fact]
    public void RequestingTheTrackAlreadyPlayingIsNotARestart() {
        // The guard sits above the fade, so walking back into a zone whose music is already going
        // does not interrupt it. Restarting here would stutter the music at every zone boundary.
        MusicPlayback.MusicChange change = MusicPlayback.Resolve(1026, 1026);

        Assert.Equal(MusicPlayback.MusicAction.None, change.Action);
    }

    [Fact]
    public void MinusOneIsSilenceRatherThanAQuery() {
        MusicPlayback.MusicChange change = MusicPlayback.Resolve(MusicPlayback.NoTrack, 1026);

        Assert.Equal(MusicPlayback.MusicAction.Stop, change.Action);
        Assert.Equal(1026, change.PreviousTrack);
    }

    [Fact]
    public void ADifferentTrackSwitches() {
        MusicPlayback.MusicChange change = MusicPlayback.Resolve(1042, 1026);

        Assert.Equal(MusicPlayback.MusicAction.Switch, change.Action);
        Assert.Equal(1026, change.PreviousTrack);
    }

    [Fact]
    public void ThePreviousTrackComesBackInEveryCase() {
        // This return value is the whole save-and-restore idiom: a screen stashes it on entry and
        // hands it back on exit.
        Assert.Equal(7, MusicPlayback.Resolve(MusicPlayback.QueryOnly, 7).PreviousTrack);
        Assert.Equal(7, MusicPlayback.Resolve(7, 7).PreviousTrack);
        Assert.Equal(7, MusicPlayback.Resolve(9, 7).PreviousTrack);
        Assert.Equal(7, MusicPlayback.Resolve(MusicPlayback.NoTrack, 7).PreviousTrack);
    }

    [Fact]
    public void SavingAndRestoringRoundTrips() {
        const int world = 1026;
        int saved = MusicPlayback.Resolve(MusicPlayback.QueryOnly, world).PreviousTrack;

        MusicPlayback.MusicChange intoLocation = MusicPlayback.Resolve(1042, world);
        Assert.Equal(MusicPlayback.MusicAction.Switch, intoLocation.Action);

        MusicPlayback.MusicChange back = MusicPlayback.Resolve(saved, 1042);
        Assert.Equal(MusicPlayback.MusicAction.Switch, back.Action);
        Assert.Equal(world, saved);
    }

    [Fact]
    public void WithNoSoundDriverEveryRequestIsInert() {
        MusicPlayback.MusicChange change =
            MusicPlayback.Resolve(1042, 1026, hasSoundDriver: false);

        Assert.Equal(MusicPlayback.MusicAction.None, change.Action);
        Assert.Equal(1026, change.PreviousTrack);
    }

    [Fact]
    public void TheFirstTrackOfASessionStartsWithoutAFade() {
        Assert.False(MusicPlayback.NeedsFadeOut(MusicPlayback.NoTrack));
        Assert.True(MusicPlayback.NeedsFadeOut(1026));
    }

    [Fact]
    public void MusicOffStillDoesTheBookkeeping() {
        // The original stops, loads and records the new track even with music disabled; it just
        // never starts it. So the track a later query reports is the one that WOULD be playing.
        Assert.False(MusicPlayback.IsAudible(false));
        Assert.Equal(MusicPlayback.MusicAction.Switch, MusicPlayback.Resolve(1042, 1026).Action);
    }
}
