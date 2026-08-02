namespace BetrayalAtKrondor.Tests.Inventory;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Stack splitting/combining (docs/specs/inventory-item-handling.md §13-§15): the picker
/// trigger matrix of pickupItem @0x558c2, Apply's whole/split/top-up paths, Distribute's
/// progressive portions, and the QuantityPickerModel value rules.
/// </summary>
public class InventoryStackTests {
    private const byte Rations = 100;   // 0x8800: countable + stackable (always asks)
    private const byte Torches = 101;   // 0x800 only: silent unless member->member
    private const byte Gem = 102;       // no stack flags at all

    private static ObjectInfoSet Objs() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("ra") { Number = Rations, Name = "Rations", InventorySlots = 1, MaxAmount = 10, Flags = (ObjectFlags)0x8800 },
        new ObjectInfo("to") { Number = Torches, Name = "Torches", InventorySlots = 1, MaxAmount = 6, Flags = (ObjectFlags)0x800 },
        new ObjectInfo("ge") { Number = Gem, Name = "Gem", InventorySlots = 1, MaxAmount = 1 },
    });

    private static RuntimeContainer Member(params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.Inventory };
        c.Items.AddRange(items); return c;
    }
    private static RuntimeContainer Corpse(params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.Corpse };
        c.Items.AddRange(items); return c;
    }
    private static InventoryTransfer.TransferPlan Plan(RuntimeContainer src, int idx,
        RuntimeContainer dst, bool allowShare = false) {
        int gold = 0;
        return InventoryTransfer.Plan(src, idx, dst, Objs(), ref gold, false, allowShare);
    }

    // --- picker trigger matrix (spec §14) ---

    [Fact] public void Countable_FromCorpse_AsksQuantity() {
        var plan = Plan(Corpse(new RuntimeItem(Rations, 7, 0)), 0, Member());
        Assert.Null(plan.Immediate);
        Assert.Equal(7, plan.MaxQuantity);
        Assert.False(plan.IsTopUp);
    }

    [Fact] public void Countable_StackOfOne_MovesWithoutAsking() {
        var src = Corpse(new RuntimeItem(Rations, 1, 0));
        var dst = Member();
        var plan = Plan(src, 0, dst);
        Assert.Equal(InventoryTransfer.Result.Moved, plan.Immediate);
        Assert.Empty(src.Items);
        Assert.Single(dst.Items);
    }

    [Fact] public void StackableOnly_FromCorpse_MovesSilently() {
        var src = Corpse(new RuntimeItem(Torches, 4, 0));
        var dst = Member();
        var plan = Plan(src, 0, dst);
        Assert.Equal(InventoryTransfer.Result.Moved, plan.Immediate);
        Assert.Equal(4, dst.Items[0].Variable);
    }

    [Fact] public void StackableOnly_MemberToMember_AsksQuantity() {
        var plan = Plan(Member(new RuntimeItem(Torches, 4, 0)), 0, Member());
        Assert.Null(plan.Immediate);
        Assert.Equal(4, plan.MaxQuantity);
    }

    [Fact] public void AllowShare_IsCarriedOntoThePlan() {
        Assert.True(Plan(Corpse(new RuntimeItem(Rations, 7, 0)), 0, Member(), allowShare: true).AllowShare);
        Assert.False(Plan(Corpse(new RuntimeItem(Rations, 7, 0)), 0, Member()).AllowShare);
    }

    // --- silent merge is FULL-FIT only (spec §13.2) ---

    [Fact] public void SilentMerge_HappensViaConsolidation_WhenWholeAmountFits() {
        var src = Corpse(new RuntimeItem(Torches, 2, 0));
        var dst = Corpse(new RuntimeItem(Torches, 3, 0)); // corpse->corpse: no picker
        var plan = Plan(src, 0, dst);
        Assert.Equal(InventoryTransfer.Result.Moved, plan.Immediate);
        Assert.Single(dst.Items);
        Assert.Equal(5, dst.Items[0].Variable);
        Assert.Empty(src.Items);
    }

    [Fact] public void OverflowingMove_LeavesTwoStacksAtTheDestination_NeverARemainderAtSource() {
        // 5 torches onto a 4/6 stack with room in the grid: the original inserts + consolidates
        // -> destination ends 6 + 3; the source is EMPTY (the old partial-merge-to-source
        // behaviour was a divergence).
        var src = Corpse(new RuntimeItem(Torches, 5, 0));
        var dst = Corpse(new RuntimeItem(Torches, 4, 0));
        var plan = Plan(src, 0, dst);
        Assert.Equal(InventoryTransfer.Result.Moved, plan.Immediate);
        Assert.Empty(src.Items);
        Assert.Equal(2, dst.Items.Count);
        int a = dst.Items[0].Variable, b = dst.Items[1].Variable;
        Assert.Equal(9, a + b);
        Assert.Equal(6, System.Math.Max(a, b)); // one full stack, one remainder
    }

    // --- Apply: whole / split / cancel ---

    [Fact] public void Apply_FullAmount_MovesTheSlot() {
        var src = Corpse(new RuntimeItem(Rations, 7, 0));
        var dst = Member();
        var plan = Plan(src, 0, dst);
        var r = InventoryTransfer.Apply(plan, 7);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Empty(src.Items);
        Assert.Equal(7, dst.Items[0].Variable);
    }

    [Fact] public void Apply_Partial_ClonesAndLeavesTheRemainder() {
        var src = Corpse(new RuntimeItem(Rations, 7, 0));
        var dst = Member();
        var plan = Plan(src, 0, dst);
        var r = InventoryTransfer.Apply(plan, 3);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Equal(4, src.Items[0].Variable);
        Assert.Equal(3, dst.Items[0].Variable);
    }

    [Fact] public void Apply_Zero_CancelsWithoutMoving() {
        var src = Corpse(new RuntimeItem(Rations, 7, 0));
        var dst = Member();
        var plan = Plan(src, 0, dst);
        Assert.Equal(InventoryTransfer.Result.Cancelled, InventoryTransfer.Apply(plan, 0));
        Assert.Equal(7, src.Items[0].Variable);
        Assert.Empty(dst.Items);
    }

    // --- partial top-up (spec §15) ---

    private static RuntimeContainer FullMemberWithStack(byte id, byte count, out ObjectInfoSet objs) {
        // 20 distinct single-slot trinkets exhaust the footprint budget; one rations stack sits
        // among them with headroom.
        var list = new List<ObjectInfo> {
            new ObjectInfo("ra") { Number = Rations, Name = "Rations", InventorySlots = 1, MaxAmount = 10, Flags = (ObjectFlags)0x8800 },
        };
        var c = new RuntimeContainer { Capacity = 30, ContainerType = SaveGameContainerType.Inventory };
        for (int i = 0; i < 20; i++) {
            byte tid = (byte)(150 + i);
            list.Add(new ObjectInfo("t" + i) { Number = tid, Name = "T" + i, InventorySlots = 1, MaxAmount = 1 });
            c.Items.Add(new RuntimeItem(tid, 1, 0));
        }
        c.Items.Add(new RuntimeItem(id, count, 0));
        objs = new ObjectInfoSet("O", list);
        return c;
    }

    [Fact] public void FullDestination_WithStackHeadroom_OffersACappedPicker() {
        RuntimeContainer dst = FullMemberWithStack(Rations, 6, out ObjectInfoSet objs);
        var src = Corpse(new RuntimeItem(Rations, 9, 0));
        int gold = 0;
        var plan = InventoryTransfer.Plan(src, 0, dst, objs, ref gold);
        Assert.Null(plan.Immediate);
        Assert.True(plan.IsTopUp);
        Assert.Equal(4, plan.MaxQuantity); // 10 - 6 headroom, not the stack size

        var r = InventoryTransfer.Apply(plan, 4);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Equal(5, src.Items[0].Variable); // 9 - 4 stayed at the source
        Assert.Contains(dst.Items, it => it.ObjectId == Rations && it.Variable == 10);
    }

    [Fact] public void TopUp_SourceAlwaysKeepsTheRemainder() {
        // The top-up path is structurally partial: an amount that would fully fit the stack
        // classifies as a merge (classify 2) before this path can run, so here the source
        // count always exceeds the headroom and a remainder stays behind. (The DOS keep-1
        // clamp at CMBINV.C:993 is defensive — unreachable through the real flow.)
        RuntimeContainer dst = FullMemberWithStack(Rations, 8, out ObjectInfoSet objs);
        var src = Corpse(new RuntimeItem(Rations, 5, 0)); // 8+5 > 10: no full fit -> top-up
        int gold = 0;
        var plan = InventoryTransfer.Plan(src, 0, dst, objs, ref gold);
        Assert.True(plan.IsTopUp);
        Assert.Equal(2, plan.MaxQuantity);
        InventoryTransfer.Apply(plan, 2);
        Assert.Single(src.Items);
        Assert.Equal(3, src.Items[0].Variable); // 5 - 2 stays at the source
        Assert.Contains(dst.Items, it => it.ObjectId == Rations && it.Variable == 10);
    }

    [Fact] public void FullDestination_NoSameKindStack_DoesNotFit() {
        RuntimeContainer dst = FullMemberWithStack(Rations, 10, out ObjectInfoSet objs); // stack already full
        var src = Corpse(new RuntimeItem(Rations, 3, 0));
        int gold = 0;
        var plan = InventoryTransfer.Plan(src, 0, dst, objs, ref gold);
        Assert.Equal(InventoryTransfer.Result.DoesNotFit, plan.Immediate);
    }

    // --- Share / Distribute (spec §15) ---

    [Fact] public void Distribute_SplitsProgressively() {
        // distributeStackToParty: portion = remaining * assigned / recipients.
        // 7 rations over 3 members -> 7*1/3=2, then 5*2/3=3, then 2*3/3=2.
        var src = Member(new RuntimeItem(Rations, 7, 0));
        var a = Member(); var b = Member(); var c = Member();
        var r = InventoryTransfer.Distribute(src, 0, new[] { a, b, c }, Objs());
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Empty(src.Items);
        Assert.Equal(2, a.Items[0].Variable);
        Assert.Equal(3, b.Items[0].Variable);
        Assert.Equal(2, c.Items[0].Variable);
    }

    [Fact] public void Distribute_SmallStack_CanLeaveEarlyRecipientsEmpty() {
        // 2 over 3: portions 0,1,1 — the first member gets nothing, faithfully.
        var src = Member(new RuntimeItem(Rations, 2, 0));
        var a = Member(); var b = Member(); var c = Member();
        InventoryTransfer.Distribute(src, 0, new[] { a, b, c }, Objs());
        Assert.Empty(a.Items);
        Assert.Equal(1, b.Items[0].Variable);
        Assert.Equal(1, c.Items[0].Variable);
    }

    [Fact] public void Distribute_NoRecipientWithRoom_Refuses() {
        var src = Member(new RuntimeItem(Rations, 5, 0));
        var full = new RuntimeContainer { Capacity = 0, ContainerType = SaveGameContainerType.Inventory };
        var r = InventoryTransfer.Distribute(src, 0, new[] { full }, Objs());
        Assert.Equal(InventoryTransfer.Result.DoesNotFit, r);
        Assert.Single(src.Items); // untouched
    }

    // --- QuantityPickerModel (spec §14 table) ---

    [Fact] public void Model_StartsAtAll_AndWrapsSingleSteps() {
        var m = new QuantityPickerModel(7);
        Assert.Equal(7, m.Value);
        m.StepUp();
        Assert.Equal(0, m.Value);   // above max wraps to 0
        m.StepDown();
        Assert.Equal(7, m.Value);   // below 0 wraps to max
    }

    [Fact] public void Model_FiveSteps_ClampThenWrapFromTheBound() {
        var m = new QuantityPickerModel(7);
        m.StepDown5();              // 7 -> 2
        Assert.Equal(2, m.Value);
        m.StepDown5();              // 2 -> clamps to 0
        Assert.Equal(0, m.Value);
        m.StepDown5();              // exactly 0 -> wraps to max
        Assert.Equal(7, m.Value);
        m.StepUp5();                // exactly max -> wraps to 0
        Assert.Equal(0, m.Value);
        m.StepUp5();                // 0 -> 5
        Assert.Equal(5, m.Value);
        m.StepUp5();                // 5 -> clamps to 7
        Assert.Equal(7, m.Value);
    }

    [Fact] public void Model_LabelRules() {
        var m = new QuantityPickerModel(7);
        Assert.Equal("Give: 7 (All)", m.Label);
        m.StepDown();
        Assert.Equal("Give: 6", m.Label);
        m.StepUp(); m.StepUp();     // 7 -> 0
        Assert.Equal("None: (Cancel)", m.Label);
    }

    // --- the entry guards that moved into Plan ---

    [Fact] public void LitTorch_RefusesToMove() {
        var src = Member(new RuntimeItem(0x54, 20, 0x0001)); // lit
        var plan = Plan(src, 0, Member());
        Assert.Equal(InventoryTransfer.Result.LitTorchRefused, plan.Immediate);
        Assert.Single(src.Items);
    }

    [Fact] public void ActiveRing_RefusesToMove() {
        var src = Member(new RuntimeItem(6, 5, 0x0001)); // active Ring of Prandur
        var plan = Plan(src, 0, Member());
        Assert.Equal(InventoryTransfer.Result.MustKeepEquipped, plan.Immediate);
    }
}
