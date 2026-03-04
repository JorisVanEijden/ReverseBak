namespace GameData.Resources.Data;

using GameData.Resources;

using System;

public class StartupGame : IResource {
    public StartupGame(string id, byte[] data) {
        Id = id;
        Data = data ?? Array.Empty<byte>();
    }

    // Raw contents of STARTUP.GAM for now. Can be replaced by strongly-typed fields once format is fully known
    public byte[] Data { get; }

    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }
}
