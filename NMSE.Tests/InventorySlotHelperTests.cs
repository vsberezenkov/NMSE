using NMSE.Core;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for inventory slot drag-and-drop data manipulation.
/// Verifies UpdateSlotIndex, SwapSlotIndices, and DuplicateSlot
/// correctly modify the underlying JSON data.
/// </summary>
public class InventorySlotHelperTests
{
    /// <summary>
    /// Helper to create a minimal inventory slot JSON object at a given position.
    /// </summary>
    private static JsonObject MakeSlot(string itemId, int x, int y, int amount = 100, int maxAmount = 250)
    {
        var slot = new JsonObject();
        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", "Product");
        slot.Add("Type", typeObj);
        slot.Add("Id", itemId);
        slot.Add("Amount", amount);
        slot.Add("MaxAmount", maxAmount);
        slot.Add("DamageFactor", 0.0);
        slot.Add("FullyInstalled", true);
        slot.Add("AddedAutomatically", false);
        var indexObj = new JsonObject();
        indexObj.Add("X", x);
        indexObj.Add("Y", y);
        slot.Add("Index", indexObj);
        return slot;
    }

    // ──────────────────────────────────────────────────────────
    // UpdateSlotIndex
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateSlotIndex_UpdatesExistingIndex()
    {
        var slot = MakeSlot("^FUEL1", 0, 0);
        InventorySlotHelper.UpdateSlotIndex(slot, 3, 2);
        var idx = slot.GetObject("Index")!;
        Assert.Equal(3, idx.GetInt("X"));
        Assert.Equal(2, idx.GetInt("Y"));
    }

    [Fact]
    public void UpdateSlotIndex_CreatesIndexWhenMissing()
    {
        var slot = new JsonObject();
        slot.Add("Id", "^FUEL1");
        // No Index field
        InventorySlotHelper.UpdateSlotIndex(slot, 5, 7);
        var idx = slot.GetObject("Index")!;
        Assert.NotNull(idx);
        Assert.Equal(5, idx.GetInt("X"));
        Assert.Equal(7, idx.GetInt("Y"));
    }

    [Fact]
    public void UpdateSlotIndex_PreservesOtherFields()
    {
        var slot = MakeSlot("^EYEBALL", 1, 1, 50, 100);
        InventorySlotHelper.UpdateSlotIndex(slot, 9, 4);
        Assert.Equal("^EYEBALL", slot.Get("Id"));
        Assert.Equal(50, slot.GetInt("Amount"));
        Assert.Equal(100, slot.GetInt("MaxAmount"));
    }

    // ──────────────────────────────────────────────────────────
    // SwapSlotIndices
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void SwapSlotIndices_SwapsPositions()
    {
        var slotA = MakeSlot("^FUEL1", 0, 0);
        var slotB = MakeSlot("^EYEBALL", 2, 3);

        InventorySlotHelper.SwapSlotIndices(slotA, 0, 0, slotB, 2, 3);

        // Slot A should now be at position (2,3)
        var idxA = slotA.GetObject("Index")!;
        Assert.Equal(2, idxA.GetInt("X"));
        Assert.Equal(3, idxA.GetInt("Y"));

        // Slot B should now be at position (0,0)
        var idxB = slotB.GetObject("Index")!;
        Assert.Equal(0, idxB.GetInt("X"));
        Assert.Equal(0, idxB.GetInt("Y"));
    }

    [Fact]
    public void SwapSlotIndices_PreservesItemData()
    {
        var slotA = MakeSlot("^FUEL1", 1, 0, 100, 250);
        var slotB = MakeSlot("^EYEBALL", 4, 2, 50, 100);

        InventorySlotHelper.SwapSlotIndices(slotA, 1, 0, slotB, 4, 2);

        // Item data unchanged
        Assert.Equal("^FUEL1", slotA.Get("Id"));
        Assert.Equal(100, slotA.GetInt("Amount"));
        Assert.Equal("^EYEBALL", slotB.Get("Id"));
        Assert.Equal(50, slotB.GetInt("Amount"));
    }

    [Fact]
    public void SwapSlotIndices_SymmetricOperation()
    {
        var slotA = MakeSlot("^A", 1, 2);
        var slotB = MakeSlot("^B", 5, 6);

        // Swap once
        InventorySlotHelper.SwapSlotIndices(slotA, 1, 2, slotB, 5, 6);

        // Verify swapped
        Assert.Equal(5, slotA.GetObject("Index")!.GetInt("X"));
        Assert.Equal(6, slotA.GetObject("Index")!.GetInt("Y"));
        Assert.Equal(1, slotB.GetObject("Index")!.GetInt("X"));
        Assert.Equal(2, slotB.GetObject("Index")!.GetInt("Y"));

        // Swap back
        InventorySlotHelper.SwapSlotIndices(slotA, 5, 6, slotB, 1, 2);

        // Should be back to original positions
        Assert.Equal(1, slotA.GetObject("Index")!.GetInt("X"));
        Assert.Equal(2, slotA.GetObject("Index")!.GetInt("Y"));
        Assert.Equal(5, slotB.GetObject("Index")!.GetInt("X"));
        Assert.Equal(6, slotB.GetObject("Index")!.GetInt("Y"));
    }

    // ──────────────────────────────────────────────────────────
    // DuplicateSlot
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void DuplicateSlot_CreatesNewSlotAtTargetPosition()
    {
        var source = MakeSlot("^FUEL1", 0, 0, 100, 250);
        var dup = InventorySlotHelper.DuplicateSlot(source, 3, 4);

        var idx = dup.GetObject("Index")!;
        Assert.Equal(3, idx.GetInt("X"));
        Assert.Equal(4, idx.GetInt("Y"));
    }

    [Fact]
    public void DuplicateSlot_CopiesItemId()
    {
        var source = MakeSlot("^EYEBALL", 1, 1, 50, 100);
        var dup = InventorySlotHelper.DuplicateSlot(source, 5, 5);

        Assert.Equal("^EYEBALL", dup.Get("Id"));
    }

    [Fact]
    public void DuplicateSlot_CopiesAmountAndMaxAmount()
    {
        var source = MakeSlot("^FUEL1", 0, 0, 42, 999);
        var dup = InventorySlotHelper.DuplicateSlot(source, 2, 3);

        Assert.Equal(42, dup.GetInt("Amount"));
        Assert.Equal(999, dup.GetInt("MaxAmount"));
    }

    [Fact]
    public void DuplicateSlot_CopiesInventoryType()
    {
        var source = MakeSlot("^TECH1", 0, 0);
        var srcType = source.GetObject("Type")!;
        srcType.Set("InventoryType", "Technology");

        var dup = InventorySlotHelper.DuplicateSlot(source, 1, 1);
        var dupType = dup.GetObject("Type")!;
        Assert.Equal("Technology", dupType.GetString("InventoryType"));
    }

    [Fact]
    public void DuplicateSlot_DoesNotModifySource()
    {
        var source = MakeSlot("^FUEL1", 2, 3, 100, 250);
        _ = InventorySlotHelper.DuplicateSlot(source, 8, 9);

        // Source unchanged
        var srcIdx = source.GetObject("Index")!;
        Assert.Equal(2, srcIdx.GetInt("X"));
        Assert.Equal(3, srcIdx.GetInt("Y"));
        Assert.Equal(100, source.GetInt("Amount"));
    }

    [Fact]
    public void DuplicateSlot_ProducesIndependentObject()
    {
        var source = MakeSlot("^FUEL1", 0, 0, 100, 250);
        var dup = InventorySlotHelper.DuplicateSlot(source, 5, 5);

        // Modifying duplicate should not affect source
        dup.Set("Amount", 999);
        Assert.Equal(100, source.GetInt("Amount"));
    }

    // ──────────────────────────────────────────────────────────
    // Edge cases
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateSlotIndex_ZeroCoordinates()
    {
        var slot = MakeSlot("^FUEL1", 5, 5);
        InventorySlotHelper.UpdateSlotIndex(slot, 0, 0);
        var idx = slot.GetObject("Index")!;
        Assert.Equal(0, idx.GetInt("X"));
        Assert.Equal(0, idx.GetInt("Y"));
    }

    [Fact]
    public void SwapSlotIndices_SamePosition_NoChange()
    {
        var slotA = MakeSlot("^A", 3, 3);
        var slotB = MakeSlot("^B", 3, 3);

        // Swapping slots at the same position should just re-set coordinates
        InventorySlotHelper.SwapSlotIndices(slotA, 3, 3, slotB, 3, 3);

        Assert.Equal(3, slotA.GetObject("Index")!.GetInt("X"));
        Assert.Equal(3, slotA.GetObject("Index")!.GetInt("Y"));
        Assert.Equal(3, slotB.GetObject("Index")!.GetInt("X"));
        Assert.Equal(3, slotB.GetObject("Index")!.GetInt("Y"));
    }

    [Fact]
    public void DuplicateSlot_WithDamageFactor()
    {
        var source = new JsonObject();
        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", "Technology");
        source.Add("Type", typeObj);
        source.Add("Id", "^LASER");
        source.Add("Amount", -1);
        source.Add("MaxAmount", 1);
        source.Add("DamageFactor", 0.75);
        source.Add("FullyInstalled", true);
        source.Add("AddedAutomatically", false);
        var idx = new JsonObject();
        idx.Add("X", 0);
        idx.Add("Y", 0);
        source.Add("Index", idx);

        var dup = InventorySlotHelper.DuplicateSlot(source, 1, 0);

        Assert.Equal(0.75, dup.GetDouble("DamageFactor"));
        Assert.Equal(-1, dup.GetInt("Amount"));
        Assert.Equal(1, dup.GetInt("MaxAmount"));
    }
}
