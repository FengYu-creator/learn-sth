using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 设施管理器 — 管理五种设施的等级、被动效果、使用效果
/// 挂在一个单例 GameObject 上
/// </summary>
public class FacilityManager : MonoBehaviour
{
    public static FacilityManager Instance { get; private set; }

    /// <summary>五种设施类型</summary>
    public enum FacilityType
    {
        Fridge,     // 冰箱
        TV,         // TV
        License,    // 执照
        Stove,      // 灶台
        Workbench   // 工具台
    }

    /// <summary>每种设施的等级（0=未开启）</summary>
    private Dictionary<FacilityType, int> levels = new Dictionary<FacilityType, int>
    {
        { FacilityType.Fridge, 0 },
        { FacilityType.TV, 0 },
        { FacilityType.License, 0 },
        { FacilityType.Stove, 0 },
        { FacilityType.Workbench, 0 },
    };

    /// <summary>使用效果标记 — 下一场战斗生效（一次性）</summary>
    private HashSet<FacilityType> usedFlags = new HashSet<FacilityType>();

    /// <summary>可招募单位计数（执照使用效果）</summary>
    private int recruitCount = 1;
    public int RecruitCount => recruitCount;

    /// <summary>可招募计数变化事件</summary>
    public event System.Action<int> OnRecruitCountChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Events.BattleStarted += OnBattleStarted;
    }

    private void OnDestroy()
    {
        Events.BattleStarted -= OnBattleStarted;
    }

    // ============================================================
    //  等级读写
    // ============================================================

    public int GetLevel(FacilityType type) => levels.TryGetValue(type, out int v) ? v : 0;
    public bool IsUnlocked(FacilityType type) => GetLevel(type) > 0;

    public void SetLevel(FacilityType type, int level)
    {
        levels[type] = level;

        // 执照：实时同步招募等级到 StatManager
        if (type == FacilityType.License && StatManager.Instance != null)
            StatManager.Instance.trainLevel = level;
    }

    // ============================================================
    //  使用效果：标记下次战斗生效
    // ============================================================

    public void MarkUsed(FacilityType type)
    {
        usedFlags.Add(type);
    }

    // ============================================================
    //  战斗开始时：应用所有被动 + 一次性使用效果
    // ============================================================

    private void OnBattleStarted()
    {
        // 延迟一帧，等 UnitStat 计算完 BattleStats 再应用加成
        StartCoroutine(ApplyPassiveNextFrame());
    }

    private System.Collections.IEnumerator ApplyPassiveNextFrame()
    {
        // 等待两帧：第一帧 UnitStat 计算 BattleStats，
        // 第二帧 Unit.InitStaminaNextFrame 初始化精力值
        yield return null;
        yield return null;

        foreach (var unit in FindObjectsOfType<Unit>())
        {
            if (unit.IsEnemy()) continue;

            // === 冰箱：被动 — 增加 maxHP 等级*10 ===
            int fridgeLevel = GetLevel(FacilityType.Fridge);
            if (fridgeLevel > 0)
            {
                var hs = unit.GetComponent<HealthSystem>();
                if (hs != null)
                    hs.AddMaxHP(fridgeLevel * 10);
            }

            // === 冰箱：使用效果 — 移动范围 +1 ===
            if (usedFlags.Contains(FacilityType.Fridge))
            {
                var move = unit.GetComponent<MoveAction>();
                if (move != null)
                    move.SetMoveDistance(move.GetMoveDistance() + 1);
            }

            // === TV：被动 — 增加 OBRange 等级*5 ===
            int tvLevel = GetLevel(FacilityType.TV);
            if (tvLevel > 0)
            {
                var stat = unit.GetComponent<UnitStat>();
                if (stat != null)
                    stat.AddOBRange(tvLevel * 5);
            }

            // === 灶台：被动 — 增加 maxStamina 等级*10 ===
            int stoveLevel = GetLevel(FacilityType.Stove);
            if (stoveLevel > 0)
            {
                unit.AddMaxStamina(stoveLevel * 10);
            }

            // === 灶台：使用效果 — 增加 OBHandling 等级*2 ===
            if (usedFlags.Contains(FacilityType.Stove))
            {
                int slv = GetLevel(FacilityType.Stove);
                var stat = unit.GetComponent<UnitStat>();
                if (stat != null)
                    stat.AddOBHandling(slv * 2);
            }

            // === 工具台：被动 — 增加 OBDamage 等级*2 ===
            int wbLevel = GetLevel(FacilityType.Workbench);
            if (wbLevel > 0)
            {
                var stat = unit.GetComponent<UnitStat>();
                if (stat != null)
                    stat.AddOBDamage(wbLevel * 2);
            }
        }

        // === 工具台：使用效果 — 制造一个手雷（记录在 LootInventory 中）===
        if (usedFlags.Contains(FacilityType.Workbench))
        {
            var db = Resources.Load<LootItemDatabase>("LootItemDatabase");
            if (db != null)
            {
                var sprite = db.GetRandomSprite(LootItemType.Grenade, 1);
                LootInventory.Instance?.AddItem(new LootItemRecord
                {
                    type = LootItemType.Grenade,
                    level = 1,
                    icon = sprite
                });
            }
        }

        // 清空一次性使用标记
        usedFlags.Clear();
    }

    /// <summary>TV 使用效果：立即随机加一把枪到仓库</summary>
    public void AddRandomGun()
    {
        if (GunDatabase.Instance != null && GunDatabase.Instance.gunList.Count > 0)
        {
            int idx = Random.Range(0, GunDatabase.Instance.gunList.Count);
            GunManager.Instance?.RegisterGun(idx);
        }
    }

    /// <summary>执照使用效果：增加可招募计数</summary>
    public void AddRecruitCount(int amount = 1)
    {
        recruitCount += amount;
        OnRecruitCountChanged?.Invoke(recruitCount);
    }

    /// <summary>消耗一个可招募计数（招募时调用），返回是否成功</summary>
    public bool TryConsumeRecruit()
    {
        if (recruitCount <= 0) return false;
        recruitCount--;
        OnRecruitCountChanged?.Invoke(recruitCount);
        return true;
    }
}
