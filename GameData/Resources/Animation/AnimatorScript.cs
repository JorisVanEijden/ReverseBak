namespace GameData.Resources.Animation;

public record AnimatorScript {
    public int Id { get; set; }
    public string Tag { get; set; }
    public string Script { get; set; }
    /// <remarks><b>Deliberately callerless.</b> Serialized into the generated ADS JSON for readers of that output.</remarks>
    public List<AdsScriptCall> CommandsDebug { get; set; }
}