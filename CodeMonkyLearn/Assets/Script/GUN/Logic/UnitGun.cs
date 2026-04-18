using UnityEngine;

/// <summary>
/// 挂载在 Unit 预制体上，战斗时查询自己装备的枪
/// </summary>
public class UnitGun : MonoBehaviour
{
    [SerializeField] private int equippedGunId = -1;

    public void SetEquippedGunId(int gunId)
    {
        equippedGunId = gunId;
    }

    public int GetEquippedGunId()
    {
        return equippedGunId;
    }

    /// <summary>获取该角色当前装备的枪实例（null=无装备）</summary>
    public GunAttInventory.GunInstance? GetMyGun()
    {
        if (equippedGunId < 0) return null;
        GunManager.Instance.TryGetGun(equippedGunId, out var gun);
        return gun;
    }

    /// <summary>是否装备了枪</summary>
    public bool HasGun()
    {
        return equippedGunId >= 0;
    }
}
