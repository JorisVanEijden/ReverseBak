namespace GameData.Resources.World;

public class ZoneDefinition : IResource
{
    public ZoneDefinition(string id) { Id = id; }
    public short ZoneLocation { get; set; }
    public short ZonePointer { get; set; }
    public uint DefaultCameraZ { get; set; }
    public ushort DefaultCameraPitch { get; set; }
    public ushort Flags { get; set; }
    public byte SkyColor { get; set; }
    public byte GroundColor { get; set; }
    public uint MapMinZ { get; set; }
    public uint CameraZPosition { get; set; }
    public uint MapMaxZ { get; set; }
    public uint MapZoomStep { get; set; }
    public short RmpResourceCount { get; set; }
    public short SpriteFogDivisor { get; set; }
    public uint SpriteFogNearDistance { get; set; }
    public uint Unused26 { get; set; }
    public short PolygonFogDivisor { get; set; }
    public uint PolygonFogNearDistance { get; set; }
    public uint FarClipDistance { get; set; }
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
