namespace GameData.Resources.Data;

using System.Collections.Generic;

/// <summary>
/// How much of a written save body was authored from our model vs. copied from the backing buffer.
/// Drive <see cref="PassthroughBytes"/> toward 0 as more of the format is modeled.
/// </summary>
public readonly record struct SaveCoverage(
    int TotalBodyBytes,
    int AuthoredBytes,
    IReadOnlyList<(int Offset, int Length)> AuthoredRanges) {
    public int PassthroughBytes => TotalBodyBytes - AuthoredBytes;
    public double PercentAuthored => TotalBodyBytes == 0 ? 0.0 : (double)AuthoredBytes / TotalBodyBytes * 100.0;
}
