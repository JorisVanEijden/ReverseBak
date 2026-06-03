namespace GameData.Resources.Layout;

/// <summary>9-slice margins in canonical pixels for a stretchable background graphic. All-zero = no slicing.</summary>
public record struct NineSlice(int Left, int Top, int Right, int Bottom);
