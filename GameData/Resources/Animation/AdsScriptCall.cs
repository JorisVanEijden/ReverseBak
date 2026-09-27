namespace GameData.Resources.Animation;

using System.Collections.Generic;

public class AdsScriptCall {
    public string Function { get; set; } = null!; // always set by AdsScriptBuilder's initializer
    public List<string> Arguments { get; set; } = null!;
}