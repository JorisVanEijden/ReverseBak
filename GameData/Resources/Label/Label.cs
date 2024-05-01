namespace GameData.Resources.Label;

public class Label {
    public int Offset { get; set; }
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public LabelAttributes Attributes { get; set; }
    public int ColorIndex { get; set; }
    public int ShadowColorIndex { get; set; }
    public string? Text { get; set; }
}