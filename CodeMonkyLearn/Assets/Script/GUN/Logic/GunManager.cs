using UnityEngine;
using Shoot.Data;

/// <summary>
/// 枪械管理器单例 — 编排 GunDatabase 和 GunInventory
/// 对标 StatManager
/// </summary>
public class GunManager : MonoBehaviour
{
    public static GunManager Instance { get; private set; }

    private GunAttInventory inventory;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        inventory = new GunAttInventory();
        Debug.Log("[GunManager] 初始化完成");
    }

    private void Start()
    {
        RegistTestGuns();
        Events.BattleStarted += OnBattleStarted;
    }

    private void OnDestroy()
    {
        Events.BattleStarted -= OnBattleStarted;
    }

    // ============================================================
    //  对外接口 — 枪械
    // ============================================================

    public void RegistTestGuns()
    {
        RegisterGun("1911");
        RegisterGun("AK");
        Debug.Log($"[GunManager] 测试枪械注册完成, 共 {inventory.GetGunCount()} 把");
    }

    /// <summary>注册一把枪到仓库（通过模板索引）</summary>
    public int RegisterGun(int templateIndex)
    {
        return inventory.RegisterGun(templateIndex);
    }

    /// <summary>注册一把枪到仓库（通过名称）</summary>
    public int RegisterGun(string gunName)
    {
        return inventory.RegisterGun(gunName);
    }

    /// <summary>获取枪械实例</summary>
    public bool TryGetGun(int gunId, out GunAttInventory.GunInstance gun)
    {
        return inventory.TryGetGun(gunId, out gun);
    }

    /// <summary>仓库中枪的数量</summary>
    public int GetGunCount() => inventory.GetGunCount();

    /// <summary>获取所有枪械实例</summary>
    public System.Collections.Generic.Dictionary<int, GunAttInventory.GunInstance>.ValueCollection GetAllGuns() => inventory.GetAllGuns();

    // ============================================================
    //  对外接口 — 角色装备/卸下（战前预分配）
    // ============================================================

    /// <summary>将枪械预分配给角色</summary>
    public bool AssignGun(int unitId, int gunId) => inventory.AssignGun(unitId, gunId);

    /// <summary>取消角色的枪械预分配</summary>
    public bool UnassignGun(int unitId) => inventory.UnassignGun(unitId);

    /// <summary>获取角色的预分配枪械ID（-1=无）</summary>
    public int GetAssignedGunId(int unitId) => inventory.GetAssignedGunId(unitId);

    /// <summary>查询某把枪被谁预分配（-1=在仓库）</summary>
    public int GetAssignedOwnerUnitId(int gunId) => inventory.GetAssignedOwnerUnitId(gunId);

    // ============================================================
    //  战斗开始时：真正装备（将预分配转给场上 Unit）
    // ============================================================

    private void OnBattleStarted()
    {
        ApplyAssignmentsToUnits();
    }

    /// <summary>战斗开始时调用：将预分配记录应用给场上单位</summary>
    public void ApplyAssignmentsToUnits()
    {
        var assignments = inventory.GetAllAssignments();
        int equipped = 0;

        foreach (var pair in assignments)
        {
            int unitId = pair.Key;
            int gunId = pair.Value;

            // 找场上对应 unitId 的单位
            Unit targetUnit = FindUnitByStatId(unitId);
            if (targetUnit == null)
            {
                Debug.LogWarning($"[GunManager] unitId={unitId} 在场上未找到，跳过枪械 {gunId}");
                continue;
            }

            // 这里简单记录一把枪给角色，战斗时 UnitGun 可查询
            targetUnit.GetComponent<UnitGun>().SetEquippedGunId(gunId);
            equipped++;
            Debug.Log($"[GunManager] 战斗开始，{targetUnit.name} 装备枪 ID={gunId}");
        }

        Debug.Log($"[GunManager] 枪械装备完成，共 {equipped} 把");
    }

    /// <summary>通过 UnitStat.id 找到场上单位</summary>
    private Unit FindUnitByStatId(int statId)
    {
        foreach (var unit in UnityEngine.Object.FindObjectsOfType<Unit>())
        {
            var stat = unit.GetComponent<UnitStat>();
            if (stat != null && stat.id == statId)
                return unit;
        }
        return null;
    }

    // ============================================================
    //  对外接口 — 配件
    // ============================================================

    /// <summary>添加配件到仓库</summary>
    public int AddAttachment(AttachmentData attachment)
    {
        return inventory.AddAttachment(attachment);
    }

    /// <summary>获取配件</summary>
    public bool TryGetAttachment(int attId, out AttachmentData attachment)
    {
        return inventory.TryGetAttachment(attId, out attachment);
    }

    // ============================================================
    //  对外接口 — 装配/卸下
    // ============================================================

    /// <summary>将配件装配到指定枪的指定部位</summary>
    public bool EquipAttachment(int gunId, PartTag part, int attId)
    {
        return inventory.EquipAttachment(gunId, part, attId);
    }

    /// <summary>从枪的指定部位卸下配件</summary>
    public bool UnequipAttachment(int gunId, PartTag part)
    {
        return inventory.UnequipAttachment(gunId, part);
    }

    // ============================================================
    //  对外接口 — 查询/筛选
    // ============================================================

    /// <summary>获取某把枪某部位可用的配件列表</summary>
    public System.Collections.Generic.List<AttachmentData> GetAvailableAttachments(int gunId, PartTag part)
    {
        return inventory.GetAvailableAttachments(gunId, part);
    }

    /// <summary>获取某把枪某部位当前装备的配件</summary>
    public bool TryGetEquipped(int gunId, PartTag part, out GunAttInventory.EquippedAttachment equipped)
    {
        return inventory.TryGetEquipped(gunId, part, out equipped);
    }

    // ============================================================
    //  调试
    // ============================================================

    /// <summary>打印仓库状态</summary>
    public void DebugLogInventory()
    {
        inventory.DebugLogAll();
    }
}
