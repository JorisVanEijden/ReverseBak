namespace ResourceExtractor.Extractors.Container;

using System.Text.Json.Serialization;

public class EncounterData {
    public int GlobalDataKey1 { get; set; }
    public int GlobalDataKey2 { get; set; }

    [JsonIgnore]
    public int GdsNumber { get; set; }

    [JsonIgnore]
    public int GdsLetter { get; set; }

    public bool Field_6 { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    [JsonInclude]
    public string GdsFilename {
        get => $"GDS{GdsNumber}{(char)(GdsLetter - 1 + 'A')}.DAT";
    }
}