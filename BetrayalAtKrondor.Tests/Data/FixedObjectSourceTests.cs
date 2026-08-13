namespace BetrayalAtKrondor.Tests.Data;

using GameData;
using GameData.Resources.Data;
using Xunit;

/// <summary>
/// The two-pass fixed-object lookup: the save's copy shadows OBJFIXED.DAT, and everything the
/// player has never touched still resolves from the shipped file.
/// </summary>
public class FixedObjectSourceTests {
    private static SaveGameContainerData Container(int zone, int x, int y,
        int minChapter = 1, int maxChapter = 9, byte capacity = 0) =>
        new SaveGameContainerData(
            new SaveGameContainerLocationData(zone, minChapter, maxChapter, 1, x, y, 0),
            SaveGameContainerType.FixedWorldItem,
            numberOfItems: 0,
            capacity: capacity,
            dataTypes: 0,
            items: new SaveGameInventoryItemData[0],
            lockData: null,
            dialogData: null,
            shopData: null,
            encounterData: null,
            timestamp: null,
            globalStateIndex: null);

    private static SaveGameZoneContainerStateData Save(params SaveGameContainerData[] containers) =>
        new SaveGameZoneContainerStateData(new[] {
            new SaveGameZoneContainerEntryData(
                (short)(containers.Length > 0 ? containers[0].Location.Zone : 1),
                containers.Length,
                containers),
        });

    private static FixedObjectSet Shipped(params SaveGameContainerData[] containers) {
        var set = new FixedObjectSet("OBJFIXED.DAT");
        set.Containers.AddRange(containers);
        return set;
    }

    [Fact]
    public void TheShippedFileAnswersWhenTheSaveHasNothing() {
        // The case that matters most: almost every object in the world is untouched, so save-only
        // lookup finds nothing at all.
        SaveGameZoneContainerStateData save = Save();
        FixedObjectSet shipped = Shipped(Container(2, 100, 200));

        SaveGameContainerData? found =
            ContainerLocator.FindContainerAtLocation(save, shipped, 2, 100, 200, chapter: 1);

        Assert.NotNull(found);
    }

    [Fact]
    public void TheSaveShadowsTheShippedFile() {
        SaveGameContainerData saved = Container(2, 100, 200, capacity: 7);
        SaveGameZoneContainerStateData save = Save(saved);
        FixedObjectSet shipped = Shipped(Container(2, 100, 200, capacity: 3));

        SaveGameContainerData? found =
            ContainerLocator.FindContainerAtLocation(save, shipped, 2, 100, 200, 1);

        Assert.Same(saved, found);
        Assert.Equal(7, found!.Capacity);
    }

    [Fact]
    public void TheMatchIsExactNotNearest() {
        FixedObjectSet shipped = Shipped(Container(2, 100, 200));

        Assert.Null(ContainerLocator.FindContainerAtLocation(Save(), shipped, 2, 101, 200, 1));
        Assert.Null(ContainerLocator.FindContainerAtLocation(Save(), shipped, 2, 100, 199, 1));
    }

    [Fact]
    public void AZoneMismatchDoesNotResolve() {
        FixedObjectSet shipped = Shipped(Container(2, 100, 200));

        Assert.Null(ContainerLocator.FindContainerAtLocation(Save(), shipped, 3, 100, 200, 1));
    }

    [Fact]
    public void TheChapterBandGatesTheShippedCopyToo() {
        // One slot can hold different objects at different points in the story.
        FixedObjectSet shipped = Shipped(Container(2, 100, 200, minChapter: 3, maxChapter: 5));

        Assert.Null(ContainerLocator.FindContainerAtLocation(Save(), shipped, 2, 100, 200, 2));
        Assert.NotNull(ContainerLocator.FindContainerAtLocation(Save(), shipped, 2, 100, 200, 3));
        Assert.NotNull(ContainerLocator.FindContainerAtLocation(Save(), shipped, 2, 100, 200, 5));
        Assert.Null(ContainerLocator.FindContainerAtLocation(Save(), shipped, 2, 100, 200, 6));
    }

    [Fact]
    public void WithoutTheShippedSourceItBehavesAsBefore() {
        // Null keeps the old save-only behaviour, so existing callers are unchanged.
        Assert.Null(ContainerLocator.FindContainerAtLocation(Save(), null, 2, 100, 200, 1));
    }

    [Fact]
    public void TheRealFileParsesIntoTheSaveModel() {
        // Skip-if-absent, like the other game-data tests. This is the check that matters: OBJFIXED
        // records really are the save's records, so one parser reads both.
        string? path = FindGameFile("OBJFIXED.DAT");
        if (path == null) {
            return;
        }

        using System.IO.FileStream stream = System.IO.File.OpenRead(path);
        FixedObjectSet set = new ResourceExtraction.Extractors.ObjFixedExtractor()
            .Extract("OBJFIXED.DAT", stream);

        Assert.NotEmpty(set.Containers);
        Assert.All(set.Containers, c => {
            Assert.InRange(c.Location.Zone, 0, 32);
            Assert.InRange(c.Location.MinChapter, 0, 15);
            Assert.InRange(c.Location.MaxChapter, 0, 15);
            Assert.True(c.Location.MinChapter <= c.Location.MaxChapter,
                $"chapter band inverted at ({c.Location.X}, {c.Location.Y})");
        });
    }

    private static string? FindGameFile(string name) {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = System.IO.Path.Combine(dir.FullName, "OriginalGame", name);
            if (System.IO.File.Exists(candidate)) {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    [Fact]
    public void TheSetFindsByLocationOnItsOwn() {
        FixedObjectSet shipped = Shipped(Container(1, 5, 6), Container(2, 100, 200));

        Assert.NotNull(shipped.FindAtLocation(2, 100, 200, 1));
        Assert.Null(shipped.FindAtLocation(2, 100, 201, 1));
    }
}
