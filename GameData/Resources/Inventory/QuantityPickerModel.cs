namespace GameData.Resources.Inventory;

/// <summary>
/// The quantity picker's value/label rules (quantityPickerDialog @0x59EA1, canassa
/// INVINSP.C:60; spec docs/specs/inventory-item-handling.md §14), kept engine-independent so
/// the view is a dumb shell. The value starts at the full maximum; single steps wrap
/// 0 ↔ max; five-steps clamp to the bound and only wrap when already exactly on it.
/// </summary>
public sealed class QuantityPickerModel {
    public int Max { get; }
    public int Value { get; private set; }

    public QuantityPickerModel(int max) {
        Max = max < 0 ? 0 : max;
        Value = Max; // the picker opens at "all"
    }

    /// <summary>−1; below 0 wraps to max (INVINSP.C:122-124).</summary>
    public void StepDown() => Value = Value - 1 < 0 ? Max : Value - 1;

    /// <summary>+1; above max wraps to 0 (INVINSP.C:138-140).</summary>
    public void StepUp() => Value = Value + 1 > Max ? 0 : Value + 1;

    /// <summary>−5, clamped at 0 — except from exactly 0, which wraps to max
    /// (INVINSP.C:153-162). Also Shift + single step.</summary>
    public void StepDown5() => Value = Value == 0 ? Max : (Value - 5 < 0 ? 0 : Value - 5);

    /// <summary>+5, clamped at max — except from exactly max, which wraps to 0
    /// (INVINSP.C:143-152). Also Shift + single step.</summary>
    public void StepUp5() => Value = Value == Max ? 0 : (Value + 5 > Max ? Max : Value + 5);

    /// <summary>The accept-button text (INVINSP.C:90-98): "Give: N", with " (All)" at the
    /// maximum; 0 reads "None: (Cancel)" — and accepting there cancels.</summary>
    public string Label => Value == 0
        ? "None: (Cancel)"
        : "Give: " + Value + (Value == Max ? " (All)" : string.Empty);
}
