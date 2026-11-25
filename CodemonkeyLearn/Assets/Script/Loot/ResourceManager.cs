using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资源管理器 — 管理五种资源的数量
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public event Action OnResourcesChanged;

    private Dictionary<LootItemType, int> resources = new Dictionary<LootItemType, int>
    {
        { LootItemType.Food, 20 },
        { LootItemType.Parts, 20 },
        { LootItemType.Money, 20 },
        { LootItemType.Tools, 20 },
        { LootItemType.Medicine, 20 },
    };

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int GetResource(LootItemType type) => resources.TryGetValue(type, out int v) ? v : 0;

    public void AddResource(LootItemType type, int amount)
    {
        if (!resources.ContainsKey(type)) return;
        resources[type] += amount;
        OnResourcesChanged?.Invoke();
    }

    /// <summary>批量添加资源（来自结算）</summary>
    public void AddResources(Dictionary<LootItemType, int> dict)
    {
        foreach (var kvp in dict)
        {
            if (resources.ContainsKey(kvp.Key))
                resources[kvp.Key] += kvp.Value;
        }
        OnResourcesChanged?.Invoke();
    }

    /// <summary>消耗资源，成功返回 true</summary>
    public bool SpendResource(LootItemType type, int amount)
    {
        if (!resources.ContainsKey(type)) return false;
        if (resources[type] < amount) return false;
        resources[type] -= amount;
        OnResourcesChanged?.Invoke();
        return true;
    }

    /// <summary>批量扣除资源（不检查是否足够，调用者保证），只触发一次事件</summary>
    public void DeductResources(Dictionary<LootItemType, int> dict)
    {
        foreach (var kvp in dict)
        {
            if (resources.ContainsKey(kvp.Key))
                resources[kvp.Key] -= kvp.Value;
        }
        OnResourcesChanged?.Invoke();
    }

    /// <summary>检查是否有足够资源</summary>
    public bool HasEnough(LootItemType type, int amount)
    {
        return resources.TryGetValue(type, out int v) && v >= amount;
    }
}
