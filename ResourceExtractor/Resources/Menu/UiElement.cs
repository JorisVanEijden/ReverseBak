namespace ResourceExtractor.Resources.Menu;

using System.Text.Json.Serialization;

public class UiElement {
    public ElementType ElementType { get; set; }
    public int ActionId { get; set; }
    public bool Visible { get; set; }
    public int Unknown5 { get; set; } // 0, 32, 144 or 169
    public bool Unknown7 { get; set; }
    public bool Unknown9 { get; set; } // Only used in editor
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Unknown13 { get; set; } // always -1

    [JsonIgnore]
    public short LabelOffset { get; init; }

    public int Unknown17 { get; set; } // used in REQ_TELE.DAT for teleport destinations
    public int Icon { get; set; }
    public int Unknown1B { get; set; } // 0, 1, 2, 4 Flags?
    public int Unknown1D { get; set; } // 0, 1, 2, 3
    public int Unknown1F { get; set; } // Only used in REQ_PUZL, and there it's 18
    public string Label { get; set; }
}