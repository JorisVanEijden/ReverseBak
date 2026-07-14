namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using Xunit;

public class ZoneTableInteractionTests {
    [Fact]
    public void StampInteraction_SetsCorpseEntry_AndLeavesOthersNull() {
        var table = new ZoneTable("Z01");
        var corpse = new ZoneTableEntry { Index = 1, Name = "dbody2" };
        corpse.Dat.EntityType = WorldEntityType.Corpse;
        var tree = new ZoneTableEntry { Index = 2, Name = "tree" };
        tree.Dat.EntityType = (WorldEntityType)3;
        table.Entries.Add(corpse);
        table.Entries.Add(tree);

        ZoneTableExtractor.StampInteraction(table);

        Assert.Equal("container", corpse.Behavior);
        Assert.NotNull(corpse.Interaction);
        Assert.True(corpse.Interaction!.OpensLoot);
        Assert.Null(tree.Behavior);
        Assert.Null(tree.Interaction);
    }
}
