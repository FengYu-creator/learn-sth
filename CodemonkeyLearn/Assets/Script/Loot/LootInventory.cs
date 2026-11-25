using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 临时背包 — 记录搜刮到的物品
/// </summary>
public class LootInventory : MonoBehaviour
{
    public static LootInventory Instance { get; private set; }

    private List<LootItemRecord> items = new List<LootItemRecord>();

    /// <summary>当前背包物品列表</summary>
    public IReadOnlyList<LootItemRecord> Items => items;

    /// <summary>物品数量</summary>
    public int Count => items.Count;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>添加物品</summary>
    public void AddItem(LootItemRecord item)
    {
        items.Add(item);
    }

    /// <summary>清空背包</summary>
    public void Clear()
    {
        items.Clear();
    }

    /// <summary>
    /// 结算：将背包物品转换为资源数量
    /// 1级 = 1个资源，2级 = 3个，3级 = 6个
    /// </summary>
    public Dictionary<LootItemType, int> CalculateResources()
    {
        var result = new Dictionary<LootItemType, int>
        {
            { LootItemType.Food, 0 },
            { LootItemType.Parts, 0 },
            { LootItemType.Money, 0 },
            { LootItemType.Tools, 0 },
            { LootItemType.Medicine, 0 },
        };

        foreach (var item in items)
        {
            // 手雷和枪械不转换为资源
            if (item.type == LootItemType.Grenade || item.type == LootItemType.Gun)
                continue;

            int value = item.level switch { 1 => 1, 2 => 3, 3 => 6, _ => 0 };
            result[item.type] += value;
        }

        return result;
    }
}
