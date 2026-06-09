namespace GameData.Resources.Cursor;

/// <summary>Engine-independent set of cursor frames extracted from POINTER.BMX / POINTERG.BMX.
/// Carries hotspot metadata only; cursor pixels load through the normal BMX archive path.</summary>
public class CursorSet(string id) : IResource {
    public ResourceType Type => ResourceType.CURSOR;
    public string Id { get; } = id;
    public string? SourceFile { get; set; }
    public List<CursorImage> Images { get; set; } = [];
}
