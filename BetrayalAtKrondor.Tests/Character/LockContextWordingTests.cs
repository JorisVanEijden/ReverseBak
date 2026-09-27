namespace BetrayalAtKrondor.Tests.Character;

using System;
using System.IO;
using System.Linq;
using System.Text;
using global::GameData.Resources.Character;
using global::GameData.Resources.Dialog;
using global::GameData.Resources.GameState;
using global::ResourceExtraction.Extractors.Dialog;
using Xunit;

/// <summary>
/// Each <see cref="LockPicking.LockContext"/> makes the lock prompt (dialog 79) say its own line.
/// </summary>
/// <remarks>
/// <b>The context is only a Var 0 value, so its NAME is the whole claim.</b> Until 2026-09-27 the
/// values were named from canassa — 0 Person, 2 Container — and a locked chest was prompted with
/// the abandoned-building line. Asserting the numbers could not catch that; walking the shipped
/// record to the text each value reaches does.
/// </remarks>
public class LockContextWordingTests {
    static LockContextWordingTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private const string DialogFile = "DIAL_Z00.DDX";
    private const int LockPrompt = PicklockWorkingSet.AskToOpenDialog;

    private static Dialog? LoadShippedDialogs() {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string path = Path.Combine(dir, "OriginalGame", DialogFile);
            if (File.Exists(path)) {
                using FileStream stream = File.OpenRead(path);
                return new DdxExtractor().Extract(DialogFile, stream);
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    [Theory]
    [InlineData(LockPicking.LockContext.Container, "lid refused to budge")]
    [InlineData(LockPicking.LockContext.Door, "locked tight")]
    [InlineData(LockPicking.LockContext.Building, "building appears to have been abandoned")]
    [InlineData(LockPicking.LockContext.Traversal, "locked grate")]
    public void EachContextReachesItsOwnWording(LockPicking.LockContext context, string phrase) {
        Dialog? dialogs = LoadShippedDialogs();
        if (dialogs == null) {
            return;
        }

        DialogEntry prompt = dialogs.Entries.First(e => e.Id == (uint)LockPrompt);
        DialogEntry leaf = DialogBranchWalker.WalkToLeaf(dialogs, prompt,
            key => key == GameStateEventFields.FieldBase ? (int)context : null);

        Assert.Contains(phrase, leaf.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
