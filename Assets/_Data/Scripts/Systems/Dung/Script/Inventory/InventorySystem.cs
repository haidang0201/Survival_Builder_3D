using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : Singleton<InventorySystem>
{
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    public event Action OnInventoryChanged;

    public IReadOnlyList<InventoryItem> Items => items;

    private bool isLoadingFromJson;
    private bool isDirty;

    protected override void Awake()
    {
        MakeSingleton(false);
        LoadFromJson();
    }

    public int GetItemCount(ItemType itemType)
    {
        int index = FindItemIndex(itemType);
        return index >= 0 ? Mathf.Max(0, items[index].amount) : 0;
    }

    public bool HasItem(ItemType itemType, int amount = 1)
    {
        return amount > 0 && GetItemCount(itemType) >= amount;
    }

    public bool AddItem(ItemType itemType, int amount = 1)
    {
        if (amount <= 0)
        {
            return false;
        }

        int index = FindItemIndex(itemType);
        if (index >= 0)
        {
            InventoryItem item = items[index];
            item.amount += amount;
            items[index] = item;
        }
        else
        {
            items.Add(new InventoryItem(itemType, amount));
        }

        MarkChangedAndSync();
        return true;
    }

    public bool RemoveItem(ItemType itemType, int amount = 1)
    {
        if (amount <= 0)
        {
            return false;
        }

        int index = FindItemIndex(itemType);
        if (index < 0)
        {
            return false;
        }

        InventoryItem item = items[index];
        if (item.amount < amount)
        {
            return false;
        }

        item.amount -= amount;
        if (item.amount <= 0)
        {
            items.RemoveAt(index);
        }
        else
        {
            items[index] = item;
        }

        MarkChangedAndSync();
        return true;
    }

    public void SetItemCount(ItemType itemType, int amount, bool syncNow = true)
    {
        int index = FindItemIndex(itemType);

        if (amount <= 0)
        {
            if (index >= 0)
            {
                items.RemoveAt(index);
                MarkChanged(syncNow);
            }

            return;
        }

        if (index >= 0)
        {
            InventoryItem item = items[index];
            item.amount = amount;
            items[index] = item;
        }
        else
        {
            items.Add(new InventoryItem(itemType, amount));
        }

        MarkChanged(syncNow);
    }

    public void ClearInventory(bool syncNow = true)
    {
        if (items.Count == 0)
        {
            return;
        }

        items.Clear();
        MarkChanged(syncNow);
    }

    public List<InventoryItem> GetInventorySnapshot()
    {
        return new List<InventoryItem>(items);
    }

    public void LoadFromJson()
    {
        isLoadingFromJson = true;

        try
        {
            JsonDataManager.GameSaveData data = JsonDataManager.Ins.LoadGame();
            if (data != null && data.inventoryData != null)
            {
                items = NormalizeInventory(data.inventoryData);
            }
            else
            {
                items = new List<InventoryItem>();
            }

            isDirty = false;
        }
        finally
        {
            isLoadingFromJson = false;
        }
    }

    public void SyncToJson()
    {
        if (isLoadingFromJson)
        {
            return;
        }

        try
        {
            JsonDataManager.GameSaveData data = JsonDataManager.Ins.LoadGame() ?? new JsonDataManager.GameSaveData();
            data.inventoryData = ConvertToInventoryStates(items);
            data.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            JsonDataManager.Ins.SaveGame(data);

            isDirty = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Inventory sync failed: {ex.Message}");
        }
    }

    public void DebugPrintInventory()
    {
        if (items.Count == 0)
        {
            Debug.Log("Inventory is empty.");
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Debug.Log($"Inventory[{i}] = {items[i].itemType} x{items[i].amount}");
        }
    }

    [ContextMenu("Inventory/Debug Print")]
    private void ContextDebugPrintInventory()
    {
        DebugPrintInventory();
    }

    [ContextMenu("Inventory/Add Wood +1")]
    private void ContextAddWood()
    {
        AddItem(ItemType.Wood, 1);
    }

    [ContextMenu("Inventory/Remove Wood -1")]
    private void ContextRemoveWood()
    {
        RemoveItem(ItemType.Wood, 1);
    }

    [ContextMenu("Inventory/Clear")]
    private void ContextClearInventory()
    {
        ClearInventory();
    }

    [ContextMenu("Inventory/Load From Json")]
    private void ContextLoadFromJson()
    {
        LoadFromJson();
    }

    [ContextMenu("Inventory/Sync To Json")]
    private void ContextSyncToJson()
    {
        SyncToJson();
    }

    private void MarkChangedAndSync()
    {
        MarkChanged(true);
    }

    private void MarkChanged(bool syncNow)
    {
        isDirty = true;
        OnInventoryChanged?.Invoke();

        if (syncNow)
        {
            SyncToJson();
        }
    }

    private int FindItemIndex(ItemType itemType)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemType == itemType)
            {
                return i;
            }
        }

        return -1;
    }

    private static List<InventoryItem> NormalizeInventory(List<JsonDataManager.InventoryState> source)
    {
        List<InventoryItem> normalized = new List<InventoryItem>();
        if (source == null)
        {
            return normalized;
        }

        for (int i = 0; i < source.Count; i++)
        {
            JsonDataManager.InventoryState item = source[i];
            if (item == null || item.amount <= 0 || string.IsNullOrWhiteSpace(item.itemType))
            {
                continue;
            }

            if (!TryParseItemType(item.itemType, out ItemType parsedType))
            {
                continue;
            }

            int existingIndex = FindItemIndex(normalized, parsedType);
            if (existingIndex >= 0)
            {
                InventoryItem existing = normalized[existingIndex];
                existing.amount += item.amount;
                normalized[existingIndex] = existing;
            }
            else
            {
                normalized.Add(new InventoryItem(parsedType, item.amount));
            }
        }

        return normalized;
    }

    private static int FindItemIndex(List<InventoryItem> list, ItemType itemType)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].itemType == itemType)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryParseItemType(string value, out ItemType itemType)
    {
        return Enum.TryParse(value, true, out itemType);
    }

    private static List<JsonDataManager.InventoryState> ConvertToInventoryStates(List<InventoryItem> items)
    {
        List<JsonDataManager.InventoryState> states = new List<JsonDataManager.InventoryState>();
        if (items == null)
        {
            return states;
        }

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];
            if (item.amount <= 0)
            {
                continue;
            }

            states.Add(new JsonDataManager.InventoryState
            {
                itemType = item.itemType.ToString(),
                amount = item.amount
            });
        }

        return states;
    }

    [Serializable]
    public enum ItemType
    {
        Wood,
        Stone,
        Metal,
        Food,
        Water,
        Tool,
        Weapon,
        Medicine,
        Fuel,
        Seeds
    }

    [Serializable]
    public struct InventoryItem
    {
        public ItemType itemType;
        public int amount;

        public InventoryItem(ItemType itemType, int amount)
        {
            this.itemType = itemType;
            this.amount = amount;
        }
    }

}
