namespace GameData.Resources.Animation;

public record AnimatorScript {
    public int Id { get; set; }
    public string Tag { get; set; } = null!; // set by AdsExtractor's initializer / the JSON reader
    public string Script { get; set; } = null!;
    /// <remarks><b>Deliberately callerless.</b> Serialized into the generated ADS JSON for readers of that output.</remarks>
    public List<AdsScriptCall>? CommandsDebug { get; set; }
}