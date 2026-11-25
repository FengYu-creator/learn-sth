using System.Collections.Generic;
using UnityEngine;

namespace Shoot.Data
{

/// <summary>
/// 配件模板数据库 — 管理所有配件SO模板
/// </summary>
public class AttachmentDatabase : MonoBehaviour
{
    public static AttachmentDatabase Instance { get; private set; }

    [Header("所有配件模板 (Inspector拖入)")]
    public List<AttachmentData> attachmentList = new List<AttachmentData>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[AttachmentDatabase] 已加载 {attachmentList.Count} 个配件");
    }

    /// <summary>通过索引获取配件模板</summary>
    public AttachmentData GetAttachment(int index)
    {
        return (index >= 0 && index < attachmentList.Count) ? attachmentList[index] : null;
    }

    /// <summary>通过名称获取配件模板</summary>
    public AttachmentData GetAttachmentByName(string attName)
    {
        foreach (var att in attachmentList)
        {
            if (att != null && att.attachmentName == attName)
                return att;
        }
        Debug.LogWarning($"[AttachmentDatabase] 未找到配件: {attName}");
        return null;
    }
}

// ============================================================
//  配件标签枚举
// ============================================================

/// <summary>
/// 配件部位 — 装在枪的哪里（当前3种）
/// </summary>
public enum PartTag
{
    Barrel,     // 枪管
    Stock,      // 枪托
    Magazine    // 弹夹
}

/// <summary>
/// 配件种类 — 决定通用性，哪些枪能装这个配件
/// </summary>
public enum CategoryTag
{
    // --- 按枪械类型 ---
    Pistol_Part,       // 手枪专用
    Rifle_Part,        // 步枪专用 (AssaultRifle / LMG)
    SMG_Part,          // 冲锋枪专用
    Sniper_Part,       // 狙击枪专用
    Shotgun_Part,      // 霰弹枪专用

    // --- 通用类 ---
    Universal_Barrel,  // 通用枪管
    Universal_Stock,   // 通用枪托
    Universal_Magazine, // 通用弹夹
}

// ============================================================
//  配件模板 SO
// ============================================================

/// <summary>
/// 配件模板 (ScriptableObject)
/// 在 Project 窗口: Create → Shoot → Attachment
/// </summary>
[CreateAssetMenu(fileName = "NewAttachment", menuName = "Shoot/Attachment")]
public class AttachmentData : ScriptableObject
{
    [Header("基本信息")]
    public string attachmentName;

    [Header("标签")]
    public PartTag part;                      // 单选: 部位
    public List<CategoryTag> categories;     // 多选: 种类列表

    [Header("属性加成")]
    [Tooltip("稳定性修正")]
    public int stabilityModifier = 0;
    [Tooltip("操控性修正")]
    public int handlingModifier = 0;
    [Tooltip("精准度修正")]
    public int precisionModifier = 0;
    [Tooltip("弹容量修正")]
    public int ammoModifier = 0;
    [Tooltip("射程修正")]
    public int rangeModifier = 0;
    [Tooltip("伤害修正")]
    public int damageModifier = 0;
    [Tooltip("射击间隔修正（秒，负数=加快）")]
    public float fireRateModifier = 0;
    [Tooltip("精力消耗修正（负数=节省）")]
    public int staminaCostModifier = 0;
    [Header("角色属性百分比修正")]
    [Tooltip("稳定性修正（百分比，如5=角色稳定性+5%）")]
    public float stabilityMod;
    [Tooltip("精准度修正（百分比）")]
    public float precisionMod;
    [Tooltip("操控性修正（百分比）")]
    public float handlingMod;
}

// ============================================================
//  枪支的配件兼容规则（供 GunData 使用）
// ============================================================

/// <summary>
/// 一条兼容规则：某个部位 接受哪些种类的配件
/// </summary>
[System.Serializable]
public struct PartCategoryRule
{
    [Tooltip("定义部位")]
    public PartTag part;                       // 这个规则管哪个部位
    [Tooltip("这个部位允许安装哪些种类的配件")]
    public List<CategoryTag> allowedCategories; // 允许的种类列表
}

} // end namespace
