namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>Lighting a carried light — ITEMUSE.C:356 — and its burn-down, ITEMUSE.C:593.</summary>
public class ItemLightTests {
    private const byte Torch = (byte)InventoryConsume.TorchObjectId;
    private const byte Ring = (byte)ItemLight.RingOfPrandurObjectId;

    // The shipped records: the torch burns 8 hours, the ring 24.
    private static ObjectInfoSet Objects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("O") {
            Number = Torch, Name = "Torch", ObjectType = ObjectType.LightSource, InventorySlots = 1,
            MaxAmount = 25, EffectArgA = 8,
            Flags = ObjectFlags.DiscardWhenEmpty | ObjectFlags.Stackable | ObjectFlags.LimitedUses,
        },
        new ObjectInfo("O") {
            Number = Ring, Name = "Ring of Prandur", ObjectType = ObjectType.LightSource,
            InventorySlots = 1, MaxAmount = 10, EffectArgA = 24,
            Flags = ObjectFlags.SpellcastersOnly | ObjectFlags.LimitedUses,
        },
    });

    private sealed class Clock {
        public bool Burning;
        public long LitFor = -1;
        public int Extinguished;
    }

    private static ItemUseContext Context(Clock clock) {
        var stats = new ActorStat[16];
        for (var i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 20, Max = 40 };
        }
        return new ItemUseContext(stats, 1, _ => 0, (_, _) => { }, _ => 0,
            itemLightBurning: () => clock.Burning,
            lightItem: ticks => { clock.LitFor = ticks; clock.Burning = true; },
            extinguishItemLight: () => clock.Extinguished++);
    }

    private static RuntimeContainer Pack(byte id, byte variable, bool lit) {
        var pack = new RuntimeContainer();
        pack.Items.Add(new RuntimeItem(id, variable, lit ? (ushort)ItemFlags.Lit : (ushort)0));
        return pack;
    }

    [Fact]
    public void UsingAnUnlitTorchLightsItForItsHours() {
        var clock = new Clock();
        RuntimeContainer pack = Pack(Torch, 2, lit: false);

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(), Context(clock));

        Assert.Equal(ItemUseOutcome.Silent, result.Outcome);
        Assert.NotEqual(0, pack.Items[0].ItemFlags & (ushort)ItemFlags.Lit);
        Assert.Equal(8 * 0x708L, clock.LitFor);
        Assert.Equal(2, pack.Items[0].Variable);
    }

    [Fact]
    public void ATorchInTheNaphthaCavernsExplodes_AndInAFightIsPutAway() {
        // ITEMUSE.C:370-376: chapter 4, zone 11. Neither lights the torch nor spends it.
        var clock = new Clock();
        RuntimeContainer pack = Pack(Torch, 2, lit: false);
        ItemUseContext context = Context(clock);
        context.Chapter = 4;
        context.Zone = 11;

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(), context);
        Assert.Equal(0x1b776e, result.DialogId);
        Assert.Equal(-1L, clock.LitFor);
        Assert.Equal(0, pack.Items[0].ItemFlags & (ushort)ItemFlags.Lit);

        context.InCombat = true;
        result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(), context);
        Assert.Equal(0x1b7770, result.DialogId);
        Assert.Equal(2, pack.Items[0].Variable);

        context.Zone = 12;   // anywhere else it lights as usual
        context.InCombat = false;
        Assert.Equal(ItemUseOutcome.Silent,
            InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(), context).Outcome);
    }

    [Fact]
    public void UsingALitTorchPutsItOut() {
        var clock = new Clock { Burning = true };
        RuntimeContainer pack = Pack(Torch, 2, lit: true);

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(), Context(clock));

        Assert.Equal(ItemUseOutcome.Handled, result.Outcome);
        Assert.Equal(1, clock.Extinguished);
    }

    [Fact]
    public void OnlyOneCarriedLightBurnsAtATime() {
        var clock = new Clock { Burning = true };
        RuntimeContainer pack = Pack(Ring, 5, lit: false);

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, Objects(), Context(clock));

        Assert.Equal(ItemUseOutcome.NoEffect, result.Outcome);
        Assert.Equal(0, pack.Items[0].ItemFlags & (ushort)ItemFlags.Lit);
    }

    [Fact]
    public void TheBurnDownSpendsOneTorchAndUnlightsIt() {
        RuntimeContainer pack = Pack(Torch, 3, lit: true);

        Assert.True(ItemLight.BurnDown(pack));
        Assert.Equal(2, pack.Items[0].Variable);
        Assert.Equal(0, pack.Items[0].ItemFlags & (ushort)ItemFlags.Lit);
    }

    [Fact]
    public void TheLastTorchBurnsAway() {
        RuntimeContainer pack = Pack(Torch, 1, lit: true);

        ItemLight.BurnDown(pack);

        Assert.Empty(pack.Items);
    }

    [Fact]
    public void TheRingSpendsAUseButKeepsItsSlotAtZero() {
        RuntimeContainer pack = Pack(Ring, 0, lit: true);

        ItemLight.BurnDown(pack);

        Assert.Single(pack.Items);
        Assert.Equal(0, pack.Items[0].Variable);
        Assert.Equal(0, pack.Items[0].ItemFlags & (ushort)ItemFlags.Lit);
    }

    [Fact]
    public void AnUnlitStackBesideTheBurningOneIsLeftAlone() {
        RuntimeContainer pack = Pack(Torch, 4, lit: false);

        Assert.False(ItemLight.BurnDown(pack));
        Assert.Equal(4, pack.Items[0].Variable);
    }
}
