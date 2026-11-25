using UnityEngine;
using Shoot.Data;

/// <summary>
/// 敌人行为类型
/// </summary>
public enum EnemyBehaviorType
{
    Ranged,  // 远程 — 使用枪械射击
    Melee,   // 近战 — 冲向玩家、近战攻击
    Special, // 特殊 — 自爆兵
    Neutral  // 中立 — 炸药桶
}

/// <summary>
/// 敌人数据 ScriptableObject
/// 在 Project 窗口: Create → Enemy → EnemyData
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("基本配置")]
    public string enemyName;
    public EnemyBehaviorType behaviorType;
    public Sprite icon;
    public int moveDistance = 3;

    [Header("角色基础属性")]
    public int baseHP = 50;
    public int stability;
    public int handling;
    public int precision;
    public int constitution;
    public int willpower;
    public int speed;

    [Header("OB 属性（直接配置，跳过 CalculateBattleStats）")]
    public int OBStability;
    public int OBHandling;
    public int OBPrecision;
    public int OBConstitution;
    public int OBWillpower;
    public int OBSpeed;

    [Header("枪械 OB 属性（远程敌人填）")]
    public GunType OBGunType;
    public int OBDamage;
    public int OBRange;
    public int OBHRecoil;
    public int OBVRecoil;
    public float OBFireRate = 1f;
    public int OBStaminaCost = 1;

    [Header("近战（近战敌人填）")]
    public int meleeDamage;

    [Header("特殊（自爆兵 / 炸药桶填）")]
    public int explosionDamage;
    public float explosionRadius;
}
