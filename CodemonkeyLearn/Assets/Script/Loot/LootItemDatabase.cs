using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 搜刮到的物品记录
/// </summary>
[Serializable]
public class LootItemRecord
{
    public LootItemType type;
    public int level;               // 1/2/3
    public Sprite icon;             // 物品贴图
}

/// <summary>
/// 物品库 ScriptableObject
/// 六种 × 三级 = 18 个物品贴图列表
/// </summary>
[CreateAssetMenu(fileName = "LootItemDatabase", menuName = "Loot/Item Database")]
public class LootItemDatabase : ScriptableObject
{
    [Header("食物 - 等级1/2/3")]
    public List<Sprite> foodLevel1;
    public List<Sprite> foodLevel2;
    public List<Sprite> foodLevel3;

    [Header("零件 - 等级1/2/3")]
    public List<Sprite> partsLevel1;
    public List<Sprite> partsLevel2;
    public List<Sprite> partsLevel3;

    [Header("金钱 - 等级1/2/3")]
    public List<Sprite> moneyLevel1;
    public List<Sprite> moneyLevel2;
    public List<Sprite> moneyLevel3;

    [Header("工具 - 等级1/2/3")]
    public List<Sprite> toolsLevel1;
    public List<Sprite> toolsLevel2;
    public List<Sprite> toolsLevel3;

    [Header("药品 - 等级1/2/3")]
    public List<Sprite> medicineLevel1;
    public List<Sprite> medicineLevel2;
    public List<Sprite> medicineLevel3;

    [Header("手雷 - 等级1/2")]
    public List<Sprite> grenadeLevel1;
    public List<Sprite> grenadeLevel2;

    [Header("枪械 - 等级1/2")]
    public List<Sprite> gunLevel1;
    public List<Sprite> gunLevel2;

    /// <summary>根据种类和等级获取贴图列表</summary>
    public List<Sprite> GetSprites(LootItemType type, int level)
    {
        return type switch
        {
            LootItemType.Food => level switch { 1 => foodLevel1, 2 => foodLevel2, 3 => foodLevel3, _ => null },
            LootItemType.Parts => level switch { 1 => partsLevel1, 2 => partsLevel2, 3 => partsLevel3, _ => null },
            LootItemType.Money => level switch { 1 => moneyLevel1, 2 => moneyLevel2, 3 => moneyLevel3, _ => null },
            LootItemType.Tools => level switch { 1 => toolsLevel1, 2 => toolsLevel2, 3 => toolsLevel3, _ => null },
            LootItemType.Medicine => level switch { 1 => medicineLevel1, 2 => medicineLevel2, 3 => medicineLevel3, _ => null },
            LootItemType.Grenade => level switch { 1 => grenadeLevel1, 2 => grenadeLevel2, _ => null },
            LootItemType.Gun => level switch { 1 => gunLevel1, 2 => gunLevel2, _ => null },
            _ => null
        };
    }

    /// <summary>随机获取一张贴图</summary>
    public Sprite GetRandomSprite(LootItemType type, int level)
    {
        var list = GetSprites(type, level);
        if (list == null || list.Count == 0) return null;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    /// <summary>随机获取一种+等级</summary>
    public (LootItemType type, int level) GetRandomTypeAndLevel()
    {
        LootItemType type = (LootItemType)UnityEngine.Random.Range(0, 7);
        int maxLevel = (type == LootItemType.Grenade || type == LootItemType.Gun) ? 2 : 3;
        // 等级权重：1级 60%，2级 30%，3级 10%
        float roll = UnityEngine.Random.value;
        int level = roll < 0.6f ? 1 : roll < 0.9f ? 2 : maxLevel;
        if (level > maxLevel) level = maxLevel;
        return (type, level);
    }
}
