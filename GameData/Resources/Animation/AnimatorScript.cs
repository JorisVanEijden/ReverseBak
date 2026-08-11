namespace GameData.Resources.Animation;

public record AnimatorScript {
    public int Id { get; set; }
    public string Tag { get; set; }
    public string Script { get; set; }
    public List<AdsScriptCall> CommandsDebug { get; set; }
}