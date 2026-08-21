namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The "asked about:" topic grid. The full-list layout change and the fact that asked topics stay
/// on the menu are the two a port loses.
/// </summary>
public class KeywordMenuTests {
    [Fact]
    public void NoAvailableTopicMeansNoMenuAtAll() {
        // Not even a farewell button — a caller that always shows the menu puts an empty box on
        // screen where the original shows none.
        Assert.False(KeywordMenu.Opens(0));
        Assert.True(KeywordMenu.Opens(1));
    }

    [Fact]
    public void TheMenuNormallyHoldsFifteenTopicsAndAFarewell() {
        Assert.Equal(16, KeywordMenu.SlotCount(3));
        Assert.Equal(15, KeywordMenu.FarewellSlot(3));
    }

    [Fact]
    public void AFullListGrowsBySlotRatherThanDroppingATopic() {
        Assert.Equal(17, KeywordMenu.SlotCount(16));
        Assert.Equal(16, KeywordMenu.FarewellSlot(16));
    }

    [Fact]
    public void AndTightensTheRowsToMakeItFit() {
        // The step drops by a pixel and the top inset disappears — see the next test for what that
        // actually buys.
        Assert.Equal(15, KeywordMenu.RowHeight(3));
        Assert.Equal(14, KeywordMenu.RowHeight(16));
        Assert.Equal(5, KeywordMenu.TopInset(3));
        Assert.Equal(0, KeywordMenu.TopInset(16));
    }

    [Fact]
    public void TheTighteningLimitsHowFarTheFifthRowReachesRatherThanAvoidingIt() {
        // It does NOT fit five rows into four rows' space — the grid still grows. What the tighter
        // step buys is nine pixels: the fifth row lands at 181 instead of the 190 the normal
        // spacing would have put it at.
        (int _, int fourthRow) = KeywordMenu.SlotPosition(15, 3);
        (int _, int fifthRowTight) = KeywordMenu.SlotPosition(16, 16);
        (int _, int fifthRowIfUntightened) = KeywordMenu.SlotPosition(16, 3);

        Assert.True(fifthRowTight > fourthRow, "the menu does grow");
        Assert.True(fifthRowTight < fifthRowIfUntightened, "but by less than it would have");
        Assert.Equal(9, fifthRowIfUntightened - fifthRowTight);
    }

    [Fact]
    public void TopicsRunInFourColumns() {
        Assert.Equal((12, 130), KeywordMenu.SlotPosition(0, 3));
        Assert.Equal((87, 130), KeywordMenu.SlotPosition(1, 3));
        Assert.Equal((237, 130), KeywordMenu.SlotPosition(3, 3));
        Assert.Equal((12, 145), KeywordMenu.SlotPosition(4, 3));
    }

    [Fact]
    public void AnAskedTopicStaysOnTheMenuAndOnlyChangesAppearance() {
        // It is built as a different element kind rather than dropped, so the player can see what
        // they have covered and still re-ask it. Filtering them out rewrites the conversation.
        Assert.True(KeywordMenu.AlreadyAsked(1));
        Assert.False(KeywordMenu.AlreadyAsked(0));
    }

    [Fact]
    public void TheAskedFlagIsPerTopic() {
        Assert.Equal(7501, KeywordMenu.AskedFlag(1));
        Assert.NotEqual(KeywordMenu.AskedFlag(1), KeywordMenu.AskedFlag(2));
    }

    [Fact]
    public void TheFarewellIsNotAKeywordAndReportsItsOwnAction() {
        Assert.Equal(1, KeywordMenu.FarewellActionId);
        Assert.Equal("GoodBye", KeywordMenu.FarewellLabel);
        Assert.True(KeywordMenu.ActionIdFor(0) > KeywordMenu.FarewellActionId);
    }

    [Fact]
    public void ActionIdsAreKeyedOnTheBranchIndexNotTheTopic() {
        // So the same topic in two entries reports different ids, and an id only means anything
        // against the entry it came from.
        Assert.Equal(128, KeywordMenu.ActionIdFor(0));
        Assert.Equal(131, KeywordMenu.ActionIdFor(3));
    }

    [Fact]
    public void TheKeywordTableIsOneBased() {
        Assert.Equal(0, KeywordMenu.LabelIndexFor(1));
        Assert.Equal(41, KeywordMenu.LabelIndexFor(42));
    }

    [Fact]
    public void TheFarewellSitsApartFromTheGrid() {
        Assert.Equal(237, KeywordMenu.FarewellX);
    }

    [Fact]
    public void TheForcedFarewellXONLYBitesInTheTightLayout() {
        // *** Corrected by this test, which is why it is worth having. *** I wrote it to assert the
        // farewell is always pulled off the grid. It is not: 237 is EXACTLY column 3's grid x, so in
        // the normal 16-slot menu the last slot already lands there and the force changes nothing.
        //
        // It bites only when the list is full: 17 slots put the farewell at index 16, which is
        // column 0 (x 12), and the force drags it back across to 237. So a renderer that lays every
        // slot out by index alone looks perfectly correct until a speaker has sixteen topics.
        int normalLast = KeywordMenu.FarewellSlot(availableKeywords: 5);
        (int normalX, _, _, _) = KeywordMenu.SlotRect(normalLast, 5);
        (int normalGridX, _) = KeywordMenu.SlotPosition(normalLast, 5);
        Assert.Equal(normalGridX * KeywordMenu.CanonicalScaleX, normalX);

        int tightLast = KeywordMenu.FarewellSlot(KeywordMenu.TightLayoutCount);
        (int tightX, int tightY, _, _) = KeywordMenu.SlotRect(tightLast, KeywordMenu.TightLayoutCount);
        (int tightGridX, int tightGridY) = KeywordMenu.SlotPosition(tightLast, KeywordMenu.TightLayoutCount);

        Assert.Equal(KeywordMenu.FarewellX * KeywordMenu.CanonicalScaleX, tightX);
        Assert.NotEqual(tightGridX * KeywordMenu.CanonicalScaleX, tightX);
        // The row is still the slot's own — only x is overridden.
        Assert.Equal(tightGridY * KeywordMenu.CanonicalScaleY, tightY);
    }

    [Fact]
    public void AnOrdinarySlotIsJustItsGridPositionScaled() {
        (int x, int y, int w, int h) = KeywordMenu.SlotRect(0, 5);
        (int vgaX, int vgaY) = KeywordMenu.SlotPosition(0, 5);

        Assert.Equal(vgaX * KeywordMenu.CanonicalScaleX, x);
        Assert.Equal(vgaY * KeywordMenu.CanonicalScaleY, y);
        Assert.Equal(KeywordMenu.EntryWidth * KeywordMenu.CanonicalScaleX, w);
        Assert.Equal(KeywordMenu.EntryHeight * KeywordMenu.CanonicalScaleY, h);
    }

    [Fact]
    public void TheGridStaysInsideTheCanonicalFrame() {
        // Four columns of 70 starting at 12 must not run off 1600, and the deepest row of the tight
        // layout must not run off 1200 — the check that the x5/x6 pair is the right one.
        (int lastColX, _, int w, _) = KeywordMenu.SlotRect(KeywordMenu.Columns - 1, 5);
        Assert.True(lastColX + w <= 1600, "the fourth column fits across");

        int deepest = KeywordMenu.SlotCount(KeywordMenu.TightLayoutCount) - 1;
        (_, int y, _, int h) = KeywordMenu.SlotRect(deepest, KeywordMenu.TightLayoutCount);
        Assert.True(y + h <= 1200, "the deepest row fits down");
    }

    [Fact]
    public void TheTIGHTLayoutSitsHIGHERThanTheNormalOneOnTheSameRow() {
        // The nine pixels the tightening buys, seen in canonical space: same slot, higher on screen.
        (_, int tight, _, _) = KeywordMenu.SlotRect(12, KeywordMenu.TightLayoutCount);
        (_, int normal, _, _) = KeywordMenu.SlotRect(12, 5);

        Assert.True(tight < normal);
    }
}
