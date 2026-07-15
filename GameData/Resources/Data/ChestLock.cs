namespace GameData.Resources.Data;

/// <summary>Lock state of a container, faithful to handle_Container (@0x77284): no LockData →
/// open; lock_trapped flag → trapped; a puzzle-chest id → puzzle; else a plain difficulty lock.</summary>
public enum ChestLockState { Open, Locked, Trapped, Puzzle }

public static class ChestLock {
    /// <summary>lockFlags.lock_trapped bit (IDA).</summary>
    public const byte TrappedFlag = 0x04;

    public static ChestLockState Resolve(SaveGameContainerLockData? lockData) {
        if (lockData == null) {
            return ChestLockState.Open;
        }
        if ((lockData.Flags & TrappedFlag) != 0) {
            return ChestLockState.Trapped;
        }
        if (lockData.PuzzleChest != 0) {
            return ChestLockState.Puzzle;
        }
        return ChestLockState.Locked;
    }
}
