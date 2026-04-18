using System.Collections.Generic;
using UnityEngine;
using Shoot.Data;

/// <summary>
/// 枪械模板数据库 — 管理所有枪械SO模板
/// 玩家仓库的 uniqueGunId 由 InventorySystem 单独管理，与模板索引无关
/// </summary>
public class GunDatabase : MonoBehaviour
{
    public static GunDatabase Instance { get; private set; }

    [Header("所有枪械模板 (Inspector拖入)")]
    public List<GunData> gunList = new List<GunData>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[GunDatabase] 已加载 {gunList.Count} 把枪");
    }

    /// <summary>通过索引获取枪械模板</summary>
    public GunData GetGun(int index)
    {
        return (index >= 0 && index < gunList.Count) ? gunList[index] : null;
    }

    /// <summary>通过枪械名称获取模板</summary>
    public GunData GetGunByName(string gunName)
    {
        foreach (GunData gun in gunList)
        {
            if (gun != null && gun.gunName == gunName)
            {
                return gun;
            }
        }
        Debug.LogWarning($"[GunDatabase] 未找到枪械: {gunName}");
        return null;
    }
}

// ============================================================
//  枪械种类枚举
// ============================================================

/// <summary>
/// 枪械种类枚举
/// </summary>
public enum GunType
{
    Pistol,         // 手枪
    AssaultRifle,   // 突击步枪
    SMG,            // 冲锋枪
    Sniper,         // 狙击枪
    Shotgun,        // 霰弹枪
    LMG,             // 轻机枪
    None
}

// ============================================================
//  枪械模板 SO
// ============================================================

/// <summary>
/// 枪械属性（ScriptableObject）
/// 在 Project 窗口: Create → Shoot → GunData
/// </summary>
[CreateAssetMenu(fileName = "NewGun", menuName = "Shoot/GunData")]
public class GunData : ScriptableObject
{
    [Header("基本信息")]
    public string gunName;

    [Header("枪械种类")]
    public GunType gunType;

    [Header("枪械基础属性")]
    [Tooltip("横向后座")]
    public int HorizontalStability;
    [Tooltip("纵向后座")]
    public int VerticalStability;
    [Tooltip("操控性-控制晃动")]
    public int Handling;
    [Tooltip("精准度-控制预瞄线长度")]
    public int Precision;
    [Tooltip("射程（格子数）")]
    public int Range;
    [Tooltip("基础伤害")]
    public int BaseDamage;
    [Tooltip("射击间隔（秒）")]
    public float FireRate;
    [Tooltip("精力消耗")]
    public int StaminaCost;
    [Tooltip("弹匣容量")]
    public int MaxAmmo;

    [Header("角色属性百分比修正（战斗时生效）")]
    [Tooltip("稳定性修正（百分比，如10=角色稳定性+10%）")]
    public float StabilityMod;
    [Tooltip("精准度修正（百分比）")]
    public float PrecisionMod;
    [Tooltip("操控性修正（百分比）")]
    public float HandlingMod;

    [Header("配件槽位配置")]
    [Tooltip("定义这把枪每个部位接受哪些种类的配件")]
    public List<PartCategoryRule> compatibleParts = new List<PartCategoryRule>();

    /// <summary>检查某个配件能否装备到这把枪上</summary>
    public bool CanEquip(AttachmentData attachment)
    {
        if (attachment == null) return false;

        foreach (var rule in compatibleParts)
        {
            if (rule.part == attachment.part)
            {
                foreach (var cat in attachment.categories)
                {
                    if (rule.allowedCategories.Contains(cat))
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>获取指定部位的兼容规则</summary>
    public PartCategoryRule GetSlotRule(PartTag part)
    {
        foreach (var rule in compatibleParts)
        {
            if (rule.part == part)
                return rule;
        }
        return default;
    }

    /// <summary>是否有这个部位的槽位</summary>
    public bool HasSlot(PartTag part)
    {
        foreach (var rule in compatibleParts)
        {
            if (rule.part == part) return true;
        }
        return false;
    }
}
