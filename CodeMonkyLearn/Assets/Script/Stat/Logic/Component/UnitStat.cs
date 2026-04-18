using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战时属性结构体（On Battlefield）
/// 包含角色所有属性的基础值和战时计算值，方便统一访问
/// </summary>
[Serializable]
public struct BattleStats
{
    // === 角色基础属性（始终等于 unitData.stats，战斗前后一致）===
    public int Stability;
    public int Handling;
    public int Precision;
    public int Constitution;
    public int Willpower;
    public int Speed;

    // === OB 角色属性（角色基础 + 枪械修正% + 特质加成，战斗开始后计算）===
    // 受枪械影响: Stability, Handling, Precision
    public int OBStability;
    public int OBHandling;
    public int OBPrecision;
    // 受特质影响: Constitution, Willpower, Speed（特质 TODO）
    public int OBConstitution;
    public int OBWillpower;
    public int OBSpeed;

    // === OB 枪械属性（原始值 × 操控曲线修正）===
    public GunType OBGunType;
    public int OBHRecoil;
    public int OBVRecoil;
    public int OBDamage;
    public int OBRange;
    public float OBFireRate;
    public int OBStaminaCost;

    /// <summary>空战时属性（无枪时默认）</summary>
    public static BattleStats Empty => new BattleStats
    {
        Stability = 0,
        Handling = 0,
        Precision = 0,
        Constitution = 0,
        Willpower = 0,
        Speed = 0,
        OBStability = 0,
        OBHandling = 0,
        OBPrecision = 0,
        OBConstitution = 0,
        OBWillpower = 0,
        OBSpeed = 0,
        OBGunType = GunType.None,
        OBHRecoil = 0,
        OBVRecoil = 0,
        OBDamage = 0,
        OBRange = 0,
        OBFireRate = 0f,
        OBStaminaCost = 0
    };
}

/// <summary>
/// 角色身上的属性组件，管理角色基础属性和战时属性计算
/// </summary>
public class UnitStat : MonoBehaviour
{
    public int id;
    public UnitData unitData;

    /// <summary>战时属性（战斗开始后计算填充）</summary>
    [HideInInspector] public BattleStats battleStats;

    private void Start()
    {
        // 初始化基础属性（始终等于 unitData.stats）
        battleStats = new BattleStats
        {
            Stability    = unitData.stats.Stability,
            Handling     = unitData.stats.Handling,
            Precision    = unitData.stats.Precision,
            Constitution = unitData.stats.Constitution,
            Willpower    = unitData.stats.Willpower,
            Speed        = unitData.stats.Speed
        };

        Events.BattleStarted += OnBattleStarted;
    }

    private void OnDestroy()
    {
        Events.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
        StartCoroutine(CalculateBattleStatsNextFrame());
    }

    /// <summary>
    /// 延迟一帧，确保 GunManager 已将 gunId 写入 UnitGun
    /// </summary>
    private IEnumerator CalculateBattleStatsNextFrame()
    {
        yield return null;

        // 从 UnitGun 获取当前装备枪械 ID
        UnitGun unitGun = GetComponent<UnitGun>();
        if (unitGun == null)
        {
            battleStats = BattleStats.Empty;
            yield break;
        }

        int gunId = unitGun.GetEquippedGunId();
        if (gunId < 0)
        {
            // 无枪，战时属性 = 角色自身基础值
            battleStats = CalculateBattleStatsWithoutGun();
            yield break;
        }

        // 从 GunManager 获取枪实例
        GunAttInventory.GunInstance gun;
        if (!GunManager.Instance.TryGetGun(gunId, out gun))
        {
            battleStats = BattleStats.Empty;
            yield break;
        }

        battleStats = CalculateBattleStats(gun);
    }

    /// <summary>无枪时，战时属性 = 角色自身基础值</summary>
    private BattleStats CalculateBattleStatsWithoutGun()
    {
        return new BattleStats
        {
            // 基础属性
            Stability    = unitData.stats.Stability,
            Handling     = unitData.stats.Handling,
            Precision    = unitData.stats.Precision,
            Constitution = unitData.stats.Constitution,
            Willpower    = unitData.stats.Willpower,
            Speed        = unitData.stats.Speed,

            // OB 角色属性（无枪 = 基础值）
            OBStability    = unitData.stats.Stability,
            OBHandling     = unitData.stats.Handling,
            OBPrecision    = unitData.stats.Precision,
            OBConstitution = unitData.stats.Constitution,  // TODO: 特质加成
            OBWillpower    = unitData.stats.Willpower,     // TODO: 特质加成
            OBSpeed        = unitData.stats.Speed,         // TODO: 特质加成

            // 无枪械属性
            OBGunType    = GunType.None,
            OBHRecoil    = 0,
            OBVRecoil    = 0,
            OBDamage     = 0,
            OBRange      = 0,
            OBFireRate   = 0f,
            OBStaminaCost = 0
        };
    }

    /// <summary>有枪时，计算战时属性（两层：OB角色属性 → OB枪械属性）</summary>
    private BattleStats CalculateBattleStats(GunAttInventory.GunInstance gun)
    {
        // === 角色基础属性 ===
        int baseStability    = unitData.stats.Stability;
        int baseHandling     = unitData.stats.Handling;
        int basePrecision    = unitData.stats.Precision;
        int baseConstitution = unitData.stats.Constitution;
        int baseWillpower    = unitData.stats.Willpower;
        int baseSpeed        = unitData.stats.Speed;

        // === 第一层：OB 角色属性 ===

        // 受枪械修正%影响: Stability, Handling, Precision
        float stabilityMod = gun.GetFinalStabilityMod();
        float handlingMod  = gun.GetFinalHandlingMod();
        float precisionMod = gun.GetFinalPrecisionMod();

        int obStability = Mathf.RoundToInt(baseStability * (1f + stabilityMod / 100f));
        int obHandling  = Mathf.RoundToInt(baseHandling  * (1f + handlingMod  / 100f));
        int obPrecision = Mathf.RoundToInt(basePrecision  * (1f + precisionMod / 100f));

        // 受特质加成（TODO）: Constitution, Willpower, Speed
        int traitConstitutionBonus = 0;  // TODO
        int traitWillpowerBonus    = 0;  // TODO
        int traitSpeedBonus        = 0;  // TODO

        int obConstitution = baseConstitution + traitConstitutionBonus;
        int obWillpower    = baseWillpower    + traitWillpowerBonus;
        int obSpeed        = baseSpeed        + traitSpeedBonus;

        // === 第二层：OB 枪械属性 = 原始值 × (1 - 操控曲线修正) ===

        float recoilReduction = GetRecoilReductionLogImproved(obHandling);

        int obHRecoil     = Mathf.RoundToInt(gun.template.HorizontalStability * (1f - recoilReduction));
        int obVRecoil     = Mathf.RoundToInt(gun.template.VerticalStability   * (1f - recoilReduction));
        int obDamage      = gun.GetFinalDamage();
        int obRange       = gun.GetFinalRange();
        float obFireRate  = gun.GetFinalFireRate();
        int obStaminaCost = gun.GetFinalStaminaCost();

        return new BattleStats
        {
            // 基础属性
            Stability    = baseStability,
            Handling     = baseHandling,
            Precision    = basePrecision,
            Constitution = baseConstitution,
            Willpower    = baseWillpower,
            Speed        = baseSpeed,

            // OB 角色属性（枪械修正后）
            OBStability    = obStability,
            OBHandling     = obHandling,
            OBPrecision    = obPrecision,
            OBConstitution = obConstitution,
            OBWillpower    = obWillpower,
            OBSpeed        = obSpeed,

            // OB 枪械属性
            OBGunType     = gun.template.gunType,
            OBHRecoil     = obHRecoil,
            OBVRecoil     = obVRecoil,
            OBDamage      = obDamage,
            OBRange       = obRange,
            OBFireRate    = obFireRate,
            OBStaminaCost = obStaminaCost
        };
    }

    /// <summary>
    /// 根据操控属性计算后坐力减少曲线（数值越高，后坐力减少越多）
    /// </summary>
    private float GetRecoilReductionLogImproved(int control)
    {
        // 用 log(1.5x+1) 让前期更陡峭
        float x = control;
        float reduction = Mathf.Log(1.5f * x + 1) / Mathf.Log(1.5f * 20f + 1);

        // 再加一个二次项增强前期效果
        reduction = reduction * 0.7f + (x / 20f) * (x / 20f) * 0.3f;

        return reduction * 0.85f;
    }

    /// <summary>获取战时属性（供外部调用）</summary>
    public BattleStats GetBattleStats() => battleStats;
}
