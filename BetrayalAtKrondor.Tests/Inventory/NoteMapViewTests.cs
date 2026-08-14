namespace BetrayalAtKrondor.Tests.Inventory;

using GameData.Resources.Inventory;
using Xunit;

/// <summary>
/// Using a note to look at a map. The category is broad and the behaviour is narrow — one item, one
/// map, one zone that marks your position.
/// </summary>
public class NoteMapViewTests {
    [Fact]
    public void ExactlyOneNoteShowsAnythingAtAll() {
        // "Using a Note displays a rift map" is true of one item, not of the category.
        Assert.True(NoteMapView.ShowsAMap(NoteMapView.MapNoteItemId));
        Assert.False(NoteMapView.ShowsAMap(NoteMapView.MapNoteItemId + 1));
        Assert.False(NoteMapView.ShowsAMap(0));
    }

    [Fact]
    public void OnlyOneMapIdHasAnImageBehindIt() {
        Assert.True(NoteMapView.HasImage(NoteMapView.RiftMapId));
        Assert.False(NoteMapView.HasImage(0));
        Assert.False(NoteMapView.HasImage(NoteMapView.RiftMapId + 1));
    }

    [Fact]
    public void TheMapIsMarkedViewedEvenWhenThereWasNothingToShow() {
        // The write sits outside the test on the map id.
        Assert.True(NoteMapView.MarksViewedEvenWithNoImage);
    }

    [Fact]
    public void TheViewedFlagIsPerMap() {
        Assert.Equal(6500, NoteMapView.ViewedFlag(0));
        Assert.Equal(6532, NoteMapView.ViewedFlag(NoteMapView.RiftMapId));
        Assert.NotEqual(NoteMapView.ViewedFlag(1), NoteMapView.ViewedFlag(2));
    }

    [Fact]
    public void ThePrefaceLinePlaysOnlyTheFirstTime() {
        Assert.True(NoteMapView.NeedsPreface(0));
        Assert.False(NoteMapView.NeedsPreface(1));
    }

    [Fact]
    public void OnlyOneZoneMarksWhereYouAre() {
        // Everywhere else the map is drawn with no marker, so a port that always draws it shows a
        // confident position the original never claims.
        Assert.True(NoteMapView.ShowsMarker(NoteMapView.MarkerZone));
        Assert.False(NoteMapView.ShowsMarker(1));
        Assert.False(NoteMapView.ShowsMarker(10));
    }

    [Fact]
    public void TheTwoAxesUseDifferentScales() {
        // The map is not square to the world; one divisor for both drifts further out the further
        // you are from the origin.
        long origin = 640000;
        int acrossOneStep = NoteMapView.MapX(origin + 0x8f7) - NoteMapView.MapX(origin);
        int downOneStep = NoteMapView.MapY(origin) - NoteMapView.MapY(origin + 0x8f7);

        Assert.Equal(1, acrossOneStep);
        Assert.NotEqual(1, downOneStep);
    }

    [Fact]
    public void TheRowsRunOppositeToTheWorld() {
        long origin = 640000;

        Assert.True(NoteMapView.MapY(origin + 100000) < NoteMapView.MapY(origin));
        Assert.True(NoteMapView.MapX(origin + 100000) > NoteMapView.MapX(origin));
    }

    [Fact]
    public void TheOriginLandsOnTheMapsAnchor() {
        Assert.Equal(0x90, NoteMapView.MapX(640000));
        Assert.Equal(0xc0, NoteMapView.MapY(640000));
    }

    [Fact]
    public void TheMarkerIsCentredOnTheComputedPoint() {
        (int x, int y) = NoteMapView.MarkerTopLeft(640000, 640000);

        Assert.Equal(0x90 - (NoteMapView.MarkerWidth / 2), x);
        Assert.Equal(0xc0 - (NoteMapView.MarkerHeight / 2), y);
    }

    [Fact]
    public void TheBackgroundIsAnScxDespiteWhatTheSourceAsksFor() {
        // resblit_load_pal_or_stream rewrites the last character to 'x', so every ".scr" call site
        // reads a .SCX.
        Assert.EndsWith(".SCX", NoteMapView.MapBackground);
    }
}
