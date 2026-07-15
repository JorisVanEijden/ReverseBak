namespace GameData.Resources.World;

/// <summary>A resolved slot-bitmap reference: which Z##SLOT{SlotFile}.BMX and which image in it.</summary>
public readonly record struct SlotBitmapRef(int SlotFile, int LocalImage);
