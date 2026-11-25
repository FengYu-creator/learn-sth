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

    [Header("动画")]
    [SerializeField] private Animator unitAnimator;

    [Header("射击参数")]
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private LayerMask unitLayerMask;
    [SerializeField] private int maxReflectCount = 3;

    /// <summary>墙壁遮挡检测用的 LayerMask（供敌人 AI 射线检测）</summary>
    public LayerMask WallLayerMask => wallLayerMask;

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

    // === 弹药系统 ===
    private int currentAmmo;
    private int maxAmmo;
    private bool ammoInitialized = false;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    /// <summary>
    /// 初始化弹药（从枪械模板获取弹匣容量）
    /// </summary>
    public void InitAmmo(int maxAmmoValue)
    {
        maxAmmo = maxAmmoValue;
        currentAmmo = maxAmmo;
        ammoInitialized = true;
    }

    /// <summary>
    /// 换弹，补满弹药
    /// </summary>
    public void Reload()
    {
        currentAmmo = maxAmmo;
        Events.CallAmmoChanged();
    }

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
        ExecuteShootAt(Events.LaserTargetPosition);
    }

    /// <summary>
    /// 执行射击到指定目标位置（由敌人 AI 调用）
    /// </summary>
    public void ExecuteShootAt(Vector3 targetPos)
    {
        if (firePoint == null) return;

        UnitStat unitStat = GetComponent<UnitStat>();
        Unit unit = GetComponent<Unit>();
        if (unitStat == null || unit == null) return;

        // 弹药检查：弹药为 0 不能射击
        if (ammoInitialized && currentAmmo <= 0)
        {
            Events.CallAimStateExited();
            return;
        }

        Vector3 baseDirection = (targetPos - firePoint.position).normalized;

        // 判断是否为霰弹枪
        bool isShotgun = false;
        var gunInst = GetComponent<UnitGun>()?.GetMyGun();
        if (gunInst.HasValue)
        {
            isShotgun = gunInst.Value.template.gunType == GunType.Shotgun;
        }

        int pelletCount = isShotgun ? 8 : 1;
        float spreadAngle = 15f;

        // 计算射程：优先使用枪械模板射程，否则用角色属性
        float maxRange = 50f;
        if (gunInst.HasValue && gunInst.Value.template.Range > 0)
        {
            maxRange = gunInst.Value.template.Range;
        }
        else if (unitStat.battleStats.OBRange > 0f)
        {
            maxRange = unitStat.battleStats.OBRange;
        }
        int damage = unitStat.battleStats.OBDamage;

        // 霰弹枪：循环发射多颗弹丸
        for (int i = 0; i < pelletCount; i++)
        {
            // 每颗弹丸方向加随机散射
            Vector3 direction = baseDirection;
            if (isShotgun)
            {
                direction = Quaternion.Euler(
                    0,
                    UnityEngine.Random.Range(-spreadAngle, spreadAngle),
                    0
                ) * baseDirection;
            }

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
        }

        // 消耗弹药
        if (ammoInitialized && currentAmmo > 0)
        {
            currentAmmo--;
            Events.CallAmmoChanged();
        }

        // 触发动画
        if (unitAnimator != null)
        {
            unitAnimator.SetTrigger("IsShooting");
        }

        // 触发射击事件
        Events.CallShoot();

        // 锁定本回合
        LockShootThisTurn();

        // 弹药耗尽 → 强制退出瞄准
        if (ammoInitialized && currentAmmo <= 0)
        {
            Events.CallAimStateExited();
        }
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
        Events.BattleStarted += OnBattleStarted;
    }

    private void OnDisable()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged -= OnTurnChanged;
        }
        Events.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
        StartCoroutine(InitAmmoNextFrame());
    }

    private System.Collections.IEnumerator InitAmmoNextFrame()
    {
        yield return null; // 等一帧确保 UnitGun 已装备
        var unitGun = GetComponent<UnitGun>();
        if (unitGun != null && unitGun.HasGun())
        {
            var gunInst = unitGun.GetMyGun();
            if (gunInst.HasValue)
            {
                InitAmmo(gunInst.Value.template.MaxAmmo);
            }
        }
        else
        {
            // 无枪，默认弹匣
            InitAmmo(7);
        }
    }
}