namespace BetrayalAtKrondor.Tests.Object;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

public class ObjectInfoSetTests {
    [Fact] public void GetById_ReturnsMatchingNumber() {
        var a = new ObjectInfo("x") { Number = 80, Name = "Picklocks", InventorySlots = 1, MaxAmount = 5 };
        var set = new ObjectInfoSet("OBJINFO.DAT", new List<ObjectInfo> { a });
        Assert.Same(a, set.GetById(80));
        Assert.Null(set.GetById(999));
    }
}
