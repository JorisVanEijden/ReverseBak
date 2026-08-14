namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
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
    public void ReadingTheMapNoteIsSilentAndKeepsTheNote() {
        // Outcome -2: the tail neither spends the item nor prints a result.
        var container = new RuntimeContainer { ContainerType = SaveGameContainerType.Inventory, Capacity = 6 };
        container.Items.Add(new RuntimeItem((byte)NoteMapView.MapNoteItemId, (byte)NoteMapView.RiftMapId, 0));
        var flags = new Dictionary<int, int>();

        ItemUseResult result = InventoryUse.Use(container, 0, -1, Notes(), Context(flags));

        Assert.Equal(ItemUseOutcome.Silent, result.Outcome);
        Assert.Equal(NoteMapView.MapShownDialogId, result.DialogId);
        Assert.Equal(NoteMapView.RiftMapId, result.DialogVar0);
        Assert.False(result.SourceRemoved);
        Assert.Single(container.Items);
    }

    [Fact]
    public void AnyOtherNoteAnswersWithALineAndNoMap() {
        var container = new RuntimeContainer { ContainerType = SaveGameContainerType.Inventory, Capacity = 6 };
        container.Items.Add(new RuntimeItem((byte)(NoteMapView.MapNoteItemId + 1), 0, 0));
        var flags = new Dictionary<int, int>();

        ItemUseResult result = InventoryUse.Use(container, 0, -1, Notes(), Context(flags));

        Assert.Equal(NoteMapView.WrongNoteDialogId, result.DialogId);
        Assert.Empty(flags);
    }

    [Fact]
    public void ReadingItRecordsTheMapAsSeen() {
        var container = new RuntimeContainer { ContainerType = SaveGameContainerType.Inventory, Capacity = 6 };
        container.Items.Add(new RuntimeItem((byte)NoteMapView.MapNoteItemId, 7, 0));
        var flags = new Dictionary<int, int>();

        // Map id 7 has no image, and it is still marked seen.
        ItemUseResult result = InventoryUse.Use(container, 0, -1, Notes(), Context(flags));

        Assert.Equal(NoteMapView.PrefaceDialogId, result.DialogId);
        Assert.Equal(1, flags[NoteMapView.ViewedFlag(7)]);
    }

    private static ObjectInfoSet Notes() => new ObjectInfoSet("O", new List<ObjectInfo> {
        Note((byte)NoteMapView.MapNoteItemId),
        Note((byte)(NoteMapView.MapNoteItemId + 1)),
    });

    private static ObjectInfo Note(byte id) => new ObjectInfo("O") {
        Number = id, Name = "note" + id, ObjectType = ObjectType.Note,
        InventorySlots = 1, MaxAmount = 1,
    };

    private static ItemUseContext Context(Dictionary<int, int> flags) => new ItemUseContext(
        null, 1,
        key => flags.TryGetValue(key, out int v) ? v : 0,
        (key, value) => flags[key] = value,
        _ => 0);

    [Fact]
    public void TheBackgroundIsAnScxDespiteWhatTheSourceAsksFor() {
        // resblit_load_pal_or_stream rewrites the last character to 'x', so every ".scr" call site
        // reads a .SCX.
        Assert.EndsWith(".SCX", NoteMapView.MapBackground);
    }
}
