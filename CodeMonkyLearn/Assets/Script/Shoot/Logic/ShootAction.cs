using UnityEngine;
using System;

/// <summary>
/// 射击动作组件，负责管理射击状态和生成子弹
/// 需要挂载在单位预制体上
/// </summary>
public class ShootAction : MonoBehaviour
{
    [Header("激光发射点")]
    [SerializeField] private Transform firePoint;  // 激光/子弹的发射位置

    [Header("射击参数")]
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private LayerMask unitLayerMask;
    [SerializeField] private int maxReflectCount = 3;

    /// <summary>
    /// 激光/子弹发射点
    /// </summary>
    public Transform FirePoint => firePoint;

    /// <summary>
    /// 当前回合是否可以进入射击状态
    /// </summary>
    public bool CanEnterShoot { get; private set; } = true;

    /// <summary>
    /// 本回合是否已触发过射击
    /// </summary>
    private bool hasShotThisTurn = false;

    /// <summary>
    /// 射击后由 ShootUI 直接调用，锁定本回合
    /// </summary>
    public void LockShootThisTurn()
    {
        hasShotThisTurn = true;
        CanEnterShoot = false;
    }

    /// <summary>
    /// 执行射击（由 ShootUI 在确认射击时调用）
    /// </summary>
    public void ExecuteShoot()
    {
        if (firePoint == null) return;

        UnitStat unitStat = GetComponent<UnitStat>();
        Unit unit = GetComponent<Unit>();
        if (unitStat == null || unit == null) return;

        Vector3 targetPos = Events.LaserTargetPosition;
        Vector3 direction = (targetPos - firePoint.position).normalized;

        // 计算射程
        float maxRange = unitStat.battleStats.OBRange;
        if (maxRange <= 0f)
        {
            maxRange = 50f;
        }
        int damage = unitStat.battleStats.OBDamage;

        // 计算路径
        var pathData = ShootMath.CalculatePath(
            firePoint.position,
            direction,
            maxRange,
            maxReflectCount,
            wallLayerMask,
            unitLayerMask,
            damage
        );

        // 设置攻击者和子弹寿命
        pathData.attacker = unit;
        pathData.timeout = 3f;

        // 生成子弹
        if (BulletPool.Instance != null)
        {
            var bullet = BulletPool.Instance.GetBullet();
            bullet.Init(pathData);
        }
        else
        {
            // Fallback：直接实例化
            Debug.LogWarning("BulletPool not found, instantiating bullet directly");
            var bulletObj = new GameObject("Bullet");
            bulletObj.transform.position = firePoint.position;
            var bullet = bulletObj.AddComponent<Bullet>();
            bullet.Init(pathData);
        }

        // 触发射击事件
        Events.CallShoot();

        // 锁定本回合
        LockShootThisTurn();
    }

    /// <summary>
    /// 回合变化时，若为玩家回合则重置
    /// </summary>
    private void OnTurnChanged(object sender, EventArgs e)
    {
        // 任何回合切换都清空选中单位
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.SetSelectedUnit(null);
        }

        // 只有玩家回合才重置射击状态
        if (TurnSystem.Instance != null && TurnSystem.Instance.IsPlayerTurn())
        {
            CanEnterShoot = true;
            hasShotThisTurn = false;
        }
    }

    private void OnEnable()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged += OnTurnChanged;
        }
    }

    private void OnDisable()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged -= OnTurnChanged;
        }
    }
}