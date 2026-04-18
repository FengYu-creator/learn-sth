using System.Collections.Generic;
using UnityEngine;
using Shoot.Data;

public class GunAttInventory
{
    // ============================================================
    //  数据结构
    // ============================================================

    /// <summary>
    /// 仓库中单把枪的运行时实例
    /// </summary>
    public struct GunInstance
    {
        public int inventoryId;                                                    // 仓库唯一ID
        public GunData template;                                                   // 引用枪械模板SO
        public Dictionary<PartTag, EquippedAttachment> equippedAttachments;        // 已装备配件(部位→配件)

        public GunInstance(int id, GunData gunTemplate)
        {
            inventoryId = id;
            template = gunTemplate;
            equippedAttachments = new Dictionary<PartTag, EquippedAttachment>();
        }

        /// <summary>计算最终伤害（基础 + 配件修正）</summary>
        public int GetFinalDamage()
        {
            int bonus = 0;
            foreach (var att in equippedAttachments.Values)
                bonus += att.template.damageModifier;
            return template.BaseDamage + bonus;
        }

        /// <summary>计算最终射程（基础 + 配件修正）</summary>
        public int GetFinalRange()
        {
            int bonus = 0;
            foreach (var att in equippedAttachments.Values)
                bonus += att.template.rangeModifier;
            return template.Range + bonus;
        }

        /// <summary>计算最终射击间隔（基础 + 配件修正）</summary>
        public float GetFinalFireRate()
        {
            float bonus = 0f;
            foreach (var att in equippedAttachments.Values)
                bonus += att.template.fireRateModifier;
            return template.FireRate + bonus;
        }

        /// <summary>计算最终精力消耗（基础 + 配件修正）</summary>
        public int GetFinalStaminaCost()
        {
            int bonus = 0;
            foreach (var att in equippedAttachments.Values)
                bonus += att.template.staminaCostModifier;
            return template.StaminaCost + bonus;
        }

        /// <summary>计算最终稳定性修正百分比（基础 + 配件修正）</summary>
        public float GetFinalStabilityMod()
        {
            float bonus = 0f;
            foreach (var att in equippedAttachments.Values)
                bonus += att.template.stabilityMod;
            return template.StabilityMod + bonus;
        }

        /// <summary>计算最终精准度修正百分比（基础 + 配件修正）</summary>
        public float GetFinalPrecisionMod()
        {
            float bonus = 0f;
            foreach (var att in equippedAttachments.Values)
                bonus += att.template.precisionMod;
            return template.PrecisionMod + bonus;
        }

        /// <summary>计算最终操控性修正百分比（基础 + 配件修正）</summary>
        public float GetFinalHandlingMod()
        {
            float bonus = 0f;
            foreach (var att in equippedAttachments.Values)
                bonus += att.template.handlingMod;
            return template.HandlingMod + bonus;
        }
    }

    /// <summary>
    /// 已装备的配件（SO引用 + 运行时状态）
    /// </summary>
    public struct EquippedAttachment
    {
        public AttachmentData template;    // 引用配件SO（属性加成从这里读）
        public int durability;            // 耐久度 0~100

        public EquippedAttachment(AttachmentData att)
        {
            template = att;
            durability = 100;
        }
    }

    // ============================================================
    //  核心字典
    // ============================================================

    /// <summary>枪械仓库字典: inventoryId → 枪实例</summary>
    private Dictionary<int, GunInstance> gunInventory = new Dictionary<int, GunInstance>();
    private int nextGunId = 0;

    /// <summary>配件仓库字典: inventoryId → 配件SO</summary>
    private Dictionary<int, AttachmentData> attachmentInventory = new Dictionary<int, AttachmentData>();
    private int nextAttId = 0;

    /// <summary>角色-枪械预分配字典: unitId → 装备的枪ID</summary>
    private Dictionary<int, int> equippedAssignments = new Dictionary<int, int>();

    // ============================================================
    //  角色装备操作（战前预分配）
    // ============================================================

    /// <summary>将枪械预分配给角色（战前记录，战斗时生效）</summary>
    public bool AssignGun(int unitId, int gunId)
    {
        if (!gunInventory.ContainsKey(gunId))
        {
            Debug.LogWarning($"[GunAttInventory] 枪ID={gunId} 不存在");
            return false;
        }
        equippedAssignments[unitId] = gunId;
        Debug.Log($"[GunAttInventory] 角色 unitId={unitId} 预分配枪械 ID={gunId}");
        return true;
    }

    /// <summary>取消角色的枪械预分配</summary>
    public bool UnassignGun(int unitId)
    {
        if (!equippedAssignments.Remove(unitId))
        {
            Debug.LogWarning($"[GunAttInventory] 角色 unitId={unitId} 没有预分配枪械");
            return false;
        }
        Debug.Log($"[GunAttInventory] 角色 unitId={unitId} 取消预分配");
        return true;
    }

    /// <summary>获取角色的预分配枪械ID（-1=无）</summary>
    public int GetAssignedGunId(int unitId)
    {
        return equippedAssignments.TryGetValue(unitId, out int gunId) ? gunId : -1;
    }

    /// <summary>查询某把枪被谁预分配（unitId=-1=在仓库）</summary>
    public int GetAssignedOwnerUnitId(int gunId)
    {
        foreach (var pair in equippedAssignments)
        {
            if (pair.Value == gunId) return pair.Key;
        }
        return -1;
    }

    /// <summary>获取所有预分配记录 (unitId → gunId)</summary>
    public Dictionary<int, int> GetAllAssignments() => equippedAssignments;

    // ============================================================
    //  枪械操作
    // ============================================================

    /// <summary>
    /// 从模板注册一把新枪到仓库
    /// </summary>
    /// <param name="templateIndex">GunDatabase 中的模板索引</param>
    /// <returns>仓库ID，失败返回-1</returns>
    public int RegisterGun(int templateIndex)
    {
        GunData template = GunDatabase.Instance.GetGun(templateIndex);
        if (template == null)
        {
            Debug.LogError($"[GunInventory] 模板索引 {templateIndex} 无效");
            return -1;
        }

        int id = nextGunId++;
        var instance = new GunInstance(id, template);
        gunInventory.Add(id, instance);

        Debug.Log($"[GunInventory]  注册枪械: {template.gunName} (ID={id})");
        return id;
    }

    /// <summary>通过名称注册枪械</summary>
    public int RegisterGun(string gunName)
    {
        GunData template = GunDatabase.Instance.GetGunByName(gunName);
        if (template == null)
        {
            Debug.LogError($"[GunInventory] 未找到枪械: {gunName}");
            return -1;
        }

        int id = nextGunId++;
        var instance = new GunInstance(id, template);
        gunInventory.Add(id, instance);

        Debug.Log($"[GunInventory]  注册枪械: {template.gunName} (ID={id})");
        return id;
    }

    /// <summary>获取枪械实例</summary>
    public bool TryGetGun(int inventoryId, out GunInstance gun)
    {
        return gunInventory.TryGetValue(inventoryId, out gun);
    }

    /// <summary>仓库中枪的数量</summary>
    public int GetGunCount() => gunInventory.Count;

    /// <summary>获取所有枪械实例</summary>
    public Dictionary<int, GunInstance>.ValueCollection GetAllGuns() => gunInventory.Values;

    // ============================================================
    //  配件操作
    // ============================================================

    /// <summary>
    /// 添加配件到仓库
    /// </summary>
    public int AddAttachment(AttachmentData attachment)
    {
        if (attachment == null)
        {
            Debug.LogWarning("[GunInventory] 添加了空配件");
            return -1;
        }

        int id = nextAttId++;
        attachmentInventory.Add(id, attachment);
        Debug.Log($"[GunInventory]  添加配件: {attachment.attachmentName} (ID={id})");
        return id;
    }

    /// <summary>获取配件</summary>
    public bool TryGetAttachment(int inventoryId, out AttachmentData attachment)
    {
        return attachmentInventory.TryGetValue(inventoryId, out attachment);
    }

    /// <summary>仓库中配件的数量</summary>
    public int GetAttachmentCount() => attachmentInventory.Count;

    // ============================================================
    //  装配/卸下
    // ============================================================

    /// <summary>
    /// 将配件装配到指定枪的指定部位
    /// </summary>
    /// <param name="gunId">枪的仓库ID</param>
    /// <param name="part">目标部位</param>
    /// <param name="attId">配件仓库ID</param>
    /// <returns>是否成功</returns>
    public bool EquipAttachment(int gunId, PartTag part, int attId)
    {
        // 1. 检查枪存在
        if (!gunInventory.TryGetValue(gunId, out GunInstance gun))
        {
            Debug.LogWarning($"[GunInventory] 枪ID={gunId} 不存在");
            return false;
        }

        // 2. 检查配件存在
        if (!attachmentInventory.TryGetValue(attId, out AttachmentData att))
        {
            Debug.LogWarning($"[GunInventory] 配件ID={attId} 不存在");
            return false;
        }

        // 3. 兼容性检查
        if (!gun.template.CanEquip(att))
        {
            Debug.LogWarning($"[GunInventory] ⚠️ [{att.attachmentName}] 无法安装在 [{gun.template.gunName}] 的 {part} 部位!");
            return false;
        }

        // 4. 检查该枪是否有这个部位的槽位
        if (!gun.template.HasSlot(part))
        {
            Debug.LogWarning($"[GunInventory] ⚠️ [{gun.template.gunName}] 没有 {part} 部位的槽位!");
            return false;
        }

        // 5. 从配件仓库移除
        attachmentInventory.Remove(attId);

        // 6. 装到枪上
        var equipped = new Dictionary<PartTag, EquippedAttachment>(gun.equippedAttachments);
        equipped[part] = new EquippedAttachment(att);
        gun.equippedAttachments = equipped;
        gunInventory[gunId] = gun;

        Debug.Log($"[GunInventory] ✅ [{gun.template.gunName}] 的 {part} 部位装配 [{att.attachmentName}]");
        return true;
    }

    /// <summary>
    /// 从枪的指定部位卸下配件，放回仓库
    /// </summary>
    /// <param name="gunId">枪的仓库ID</param>
    /// <param name="part">目标部位</param>
    /// <returns>是否成功</returns>
    public bool UnequipAttachment(int gunId, PartTag part)
    {
        // 1. 检查枪存在
        if (!gunInventory.TryGetValue(gunId, out GunInstance gun))
        {
            Debug.LogWarning($"[GunInventory] 枪ID={gunId} 不存在");
            return false;
        }

        // 2. 检查该部位有没有配件
        if (!gun.equippedAttachments.TryGetValue(part, out EquippedAttachment equipped))
        {
            Debug.LogWarning($"[GunInventory] [{gun.template.gunName}] 的 {part} 部位没有装备配件");
            return false;
        }

        // 3. 从枪上移除
        var equippedCopy = new Dictionary<PartTag, EquippedAttachment>(gun.equippedAttachments);
        equippedCopy.Remove(part);
        gun.equippedAttachments = equippedCopy;
        gunInventory[gunId] = gun;

        // 4. 放回配件仓库
        int newAttId = nextAttId++;
        attachmentInventory.Add(newAttId, equipped.template);

        Debug.Log($"[GunInventory] 🔧 [{gun.template.gunName}] 的 {part} 部位卸下 [{equipped.template.attachmentName}]，已放回仓库");
        return true;
    }

    // ============================================================
    //  查询/筛选
    // ============================================================

    /// <summary>
    /// 获取某把枪某部位可用的配件列表（从配件仓库中筛选）
    /// </summary>
    /// <param name="gunId">枪的仓库ID</param>
    /// <param name="part">目标部位</param>
    /// <returns>可用的配件列表（不含已装在该枪上的）</returns>
    public List<AttachmentData> GetAvailableAttachments(int gunId, PartTag part)
    {
        List<AttachmentData> result = new List<AttachmentData>();

        if (!gunInventory.TryGetValue(gunId, out GunInstance gun))
        {
            Debug.LogWarning($"[GunInventory] 枪ID={gunId} 不存在");
            return result;
        }

        foreach (var pair in attachmentInventory)
        {
            AttachmentData att = pair.Value;

            // 部位匹配 + 种类兼容
            if (att.part == part && gun.template.CanEquip(att))
            {
                result.Add(att);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取某把枪某部位当前装备的配件（如果有）
    /// </summary>
    public bool TryGetEquipped(int gunId, PartTag part, out EquippedAttachment equipped)
    {
        equipped = default;

        if (!gunInventory.TryGetValue(gunId, out GunInstance gun))
            return false;

        return gun.equippedAttachments.TryGetValue(part, out equipped);
    }

    /// <summary>
    /// 打印仓库状态（调试用）
    /// </summary>
    public void DebugLogAll()
    {
        Debug.Log($"=== 枪械仓库 (共 {gunInventory.Count} 把) ===");
        foreach (var pair in gunInventory)
        {
            var gun = pair.Value;
            string parts = "";
            foreach (var ep in gun.equippedAttachments)
            {
                parts += $" {ep.Key}={ep.Value.template.attachmentName}";
            }
            Debug.Log($"  ID={pair.Key}: {gun.template.gunName} | 配件:{parts}");
        }

        Debug.Log($"\n=== 配件仓库 (共 {attachmentInventory.Count} 个) ===");
        foreach (var pair in attachmentInventory)
        {
            var att = pair.Value;
            string cats = string.Join(", ", att.categories);
            Debug.Log($"  ID={pair.Key}: [{att.attachmentName}] {att.part} | {cats}");
        }
    }
}