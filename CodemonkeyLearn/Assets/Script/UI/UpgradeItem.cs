using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个升级项 — 五选一设施（冰箱/TV/执照/灶台/工具台）
/// 结构：空物体 → btnOpen / btnUpgrade / btnUse（三个子按钮）
/// </summary>
public class UpgradeItem : MonoBehaviour
{
    [Header("设施类型（五选一）")]
    [SerializeField] private FacilityManager.FacilityType facilityType;

    [Header("按钮引用")]
    [SerializeField] private Button btnOpen;      // 打开按钮
    [SerializeField] private Button btnUpgrade;   // 升级按钮
    [SerializeField] private Button btnUse;       // 使用按钮
    [SerializeField] private TextMeshProUGUI txtLevel;       // 等级文本

    [Header("升级消耗")]
    [SerializeField] private LootItemType upgradeCostType;   // 消耗的资源种类
    // 每级消耗数量 = 等级数（1级消耗1个，2级消耗2个，以此类推）

    [Header("使用消耗")]
    [SerializeField] private CostEntry[] useCosts;

    [Header("等级")]
    [SerializeField] private int maxLevel = 5;

    private int currentLevel = 0;
    private bool isExpanded = false;  // 升级/使用按钮是否展开

    /// <summary>当前展开的 UpgradeItem（全局唯一）</summary>
    private static UpgradeItem currentExpandedItem = null;

    public event Action<UpgradeItem> OnStateChanged;
    public bool IsUnlocked => currentLevel > 0;
    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        btnOpen.onClick.AddListener(OnOpen);
        btnUpgrade.onClick.AddListener(OnUpgrade);
        btnUse.onClick.AddListener(OnUse);

        // 初始隐藏升级/使用按钮
        btnUpgrade.gameObject.SetActive(false);
        btnUse.gameObject.SetActive(false);

        // 从 FacilityManager 同步等级
        if (FacilityManager.Instance != null)
            currentLevel = FacilityManager.Instance.GetLevel(facilityType);

        RefreshUI();
    }

    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged += OnResourcesChanged;
    }

    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged -= OnResourcesChanged;
    }

    void OnResourcesChanged()
    {
        RefreshUI();
    }

    void OnOpen()
    {
        // 已开启：切换展开/收起
        if (currentLevel > 0)
        {
            // 如果当前已展开，则收起；如果收起，则先收起其他展开的再展开自己
            if (isExpanded)
            {
                isExpanded = false;
                currentExpandedItem = null;
            }
            else
            {
                CollapseOther();
                isExpanded = true;
                currentExpandedItem = this;
            }
            RefreshUI();
            return;
        }

        // 首次开启：先收起其他，再展开自己
        CollapseOther();
        currentLevel = 1;
        isExpanded = true;
        currentExpandedItem = this;
        SyncToManager();
        RefreshUI();
        OnStateChanged?.Invoke(this);
    }

    /// <summary>收起当前展开的其他 UpgradeItem</summary>
    private void CollapseOther()
    {
        if (currentExpandedItem != null && currentExpandedItem != this)
        {
            currentExpandedItem.isExpanded = false;
            currentExpandedItem.RefreshUI();
        }
    }

    void OnUpgrade()
    {
        if (currentLevel >= maxLevel) return;

        int costAmount = currentLevel + 1;  // 升级后等级 = 消耗数量
        if (!ResourceManager.Instance.SpendResource(upgradeCostType, costAmount))
            return;

        currentLevel++;
        SyncToManager();
        RefreshUI();
        OnStateChanged?.Invoke(this);
    }

    void OnUse()
    {
        if (currentLevel <= 0) return;

        // 消耗使用物资
        foreach (var c in useCosts)
        {
            if (!ResourceManager.Instance.SpendResource(c.type, c.amount))
                return;
        }

        // 根据设施类型执行使用效果
        ExecuteUseEffect();
        RefreshUI();
        OnStateChanged?.Invoke(this);
    }

    void SyncToManager()
    {
        if (FacilityManager.Instance != null)
            FacilityManager.Instance.SetLevel(facilityType, currentLevel);
    }

    void ExecuteUseEffect()
    {
        if (FacilityManager.Instance == null) return;

        switch (facilityType)
        {
            case FacilityManager.FacilityType.TV:
                // TV：立即随机加一把枪到仓库
                FacilityManager.Instance.AddRandomGun();
                break;
            case FacilityManager.FacilityType.License:
                // 执照：增加可招募计数
                FacilityManager.Instance.AddRecruitCount();
                break;
            default:
                // 冰箱/灶台/工具台的使用效果在战斗开始时生效
                FacilityManager.Instance.MarkUsed(facilityType);
                break;
        }
    }

    void RefreshUI()
    {
        bool unlocked = currentLevel > 0;
        bool showSub = unlocked && isExpanded;

        // 打开按钮：始终显示，始终可交互
        btnOpen.gameObject.SetActive(true);
        btnOpen.interactable = true;

        // 升级/使用按钮：已解锁且展开时显示
        btnUpgrade.gameObject.SetActive(showSub);
        btnUse.gameObject.SetActive(showSub);

        if (showSub)
        {
            // 升级按钮：满级不可交互，否则检查物资
            if (currentLevel >= maxLevel)
            {
                btnUpgrade.interactable = false;
            }
            else
            {
                int costAmount = currentLevel + 1;
                btnUpgrade.interactable = CanAfford(upgradeCostType, costAmount);
            }

            // 使用按钮：检查所有消耗是否足够
            btnUse.interactable = CanAffordAll(useCosts);
        }

        // 等级文本
        if (txtLevel != null)
        {
            if (!unlocked)
                txtLevel.text = "";
            else
                txtLevel.text = "等级" + currentLevel;
        }
    }

    bool CanAfford(LootItemType type, int amount)
    {
        if (ResourceManager.Instance == null) return false;
        return ResourceManager.Instance.HasEnough(type, amount);
    }

    bool CanAffordAll(CostEntry[] costs)
    {
        if (costs == null || costs.Length == 0) return true;
        if (ResourceManager.Instance == null) return false;
        foreach (var c in costs)
        {
            if (!ResourceManager.Instance.HasEnough(c.type, c.amount))
                return false;
        }
        return true;
    }

    /// <summary>外部刷新（资源变化时调用）</summary>
    public void Refresh()
    {
        RefreshUI();
    }
}

/// <summary>消耗条目：类型 + 数量</summary>
[System.Serializable]
public struct CostEntry
{
    public LootItemType type;
    public int amount;
}
