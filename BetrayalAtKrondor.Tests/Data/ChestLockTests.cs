namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using Xunit;

public class ChestLockTests {
    private static SaveGameContainerLockData Lock(byte flags, byte difficulty, byte puzzle, byte trap) =>
        new SaveGameContainerLockData(flags, difficulty, puzzle, trap);

    [Fact] public void NullLockData_IsOpen() =>
        Assert.Equal(ChestLockState.Open, ChestLock.Resolve(null));

    [Fact] public void TrappedFlag_IsTrapped() =>
        Assert.Equal(ChestLockState.Trapped, ChestLock.Resolve(Lock(0x04, 0, 0, 30)));

    [Fact] public void PuzzleChest_IsPuzzle() =>
        Assert.Equal(ChestLockState.Puzzle, ChestLock.Resolve(Lock(0, 0, 35, 0)));

    [Fact] public void TrappedBeatsPuzzle() =>
        Assert.Equal(ChestLockState.Trapped, ChestLock.Resolve(Lock(0x04, 0, 35, 0)));

    [Fact] public void PresentNoTrapNoPuzzle_IsLocked() =>
        Assert.Equal(ChestLockState.Locked, ChestLock.Resolve(Lock(0, 17, 0, 0)));
}
