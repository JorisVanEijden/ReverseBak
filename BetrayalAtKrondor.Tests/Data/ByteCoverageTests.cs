namespace BetrayalAtKrondor.Tests.Data;

using ResourceExtraction;
using Xunit;

public class ByteCoverageTests {
    [Fact]
    public void DisjointRanges_SumIndependently() {
        var c = new ByteCoverage();
        c.Add(0, 2);
        c.Add(18, 3);
        Assert.Equal(5, c.AuthoredBytes);
        Assert.Equal(new[] { (0, 2), (18, 3) }, c.Ranges);
    }

    [Fact]
    public void OverlappingAndAdjacentRanges_MergeWithoutDoubleCounting() {
        var c = new ByteCoverage();
        c.Add(2, 4);   // [2,6)
        c.Add(6, 4);   // [6,10) adjacent -> merges to [2,10)
        c.Add(4, 2);   // [4,6) fully inside -> no change
        Assert.Equal(8, c.AuthoredBytes);
        Assert.Equal(new[] { (2, 8) }, c.Ranges);
    }
}
