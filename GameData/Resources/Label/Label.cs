namespace GameData.Resources.Label;

using GameData.Resources.Layout;

public class Label {
    public int Offset { get; set; }
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public LabelAttributes Attributes { get; set; }
    public LabelRole Role { get; set; }
    public string? Text { get; set; }
    public LayoutHint Layout { get; set; } = new();
}