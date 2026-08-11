namespace GameData.Resources.Data;

using GameData.Resources;

using System.Collections.Generic;

public class KeywordList : IResource {
    public KeywordList(string id, Dictionary<int, string> keywords) {
        Id = id;
        Keywords = keywords;
    }

    public Dictionary<int, string> Keywords { get; }

    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }
}