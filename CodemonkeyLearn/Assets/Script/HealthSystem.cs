using System;
using UnityEngine;

/// <summary>
/// 生命值系统：管理单位的血量、受伤、死亡
/// 订阅 Events.OnBulletHit 事件来处理伤害
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("生命值（编辑器可见，仅供调试）")]
    [SerializeField] private int currentHP;
    [SerializeField] private int maxHP;

    /// <summary>
    /// 最大生命值（基础值，不含战斗加成）
    /// </summary>
    [SerializeField] private int baseHP;

    /// <summary>
    /// 战斗加成生命值（OBConstitution * 10）
    /// </summary>
    [SerializeField] private int battleBonusHP;

    [Header("死亡表现")]
    [SerializeField] private GameObject deathParticlePrefab;   // 死亡粒子特效预制体
    [SerializeField] private float destroyDelay = 3f;          // 死亡后多久销毁
    [SerializeField] private float fallDuration = 0.5f;        // 倒下旋转持续时间

    // 事件：受伤时触发（参数：attacker, damage, currentHP）
    public event Action<Unit, int, int> OnDamaged;

    // 事件：死亡时触发（参数：killer）
    public event Action<Unit> OnDeath;

    // 事件：生命值初始化完成
    public event Action<int, int> OnHealthInitialized;

    private Unit unit;
    private bool isDead = false;
    private bool isInitialized = false;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    private void Start()
    {
        // 订阅子弹命中事件
        Events.OnBulletHit += OnBulletHit;
    }

    private void OnDestroy()
    {
        Events.OnBulletHit -= OnBulletHit;
    }

    /// <summary>
    /// 初始化生命值
    /// </summary>
    /// <param name="baseHP">基础生命值</param>
    /// <param name="battleBonusHP">战斗加成生命值（OBConstitution * 10）</param>
    public void Init(int baseHP, int battleBonusHP)
    {
        this.baseHP = baseHP;
        this.battleBonusHP = battleBonusHP;
        this.maxHP = baseHP + battleBonusHP;
        this.currentHP = this.maxHP;  // 战斗开始时 currentHP = maxHP
        this.isDead = false;
        this.isInitialized = true;

        Debug.Log($"[{unit?.name}] HealthSystem Init: HP = {currentHP}/{maxHP} (base: {baseHP}, bonus: {battleBonusHP})");

        OnHealthInitialized?.Invoke(currentHP, maxHP);
    }

    /// <summary>
    /// 子弹命中事件回调
    /// </summary>
    private void OnBulletHit(Unit attacker, Unit target, int damage)
    {
        // 只处理打中自己的子弹
        if (target != unit) return;

        TakeDamage(attacker, damage);
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="damage">伤害值</param>
    /// <param name="ignoreFriendlyFire">是否忽略友军伤害（爆炸时设为 true）</param>
    public void TakeDamage(Unit attacker, int damage, bool ignoreFriendlyFire = false)
    {
        if (isDead || !isInitialized) return;

        // 友军伤害检测（除非 ignoreFriendlyFire = true，如爆炸）
        if (!ignoreFriendlyFire && IsSameFaction(attacker))
        {
            Debug.Log($"[{unit?.name}] 友军伤害，忽略");
            return;
        }

        int oldHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        Debug.Log($"[{unit?.name}] 受到 {damage} 点伤害，剩余 HP: {currentHP}/{maxHP} (攻击者: {attacker?.name})");

        // 触发受伤事件
        OnDamaged?.Invoke(attacker, damage, currentHP);

        // 检查死亡
        if (currentHP <= 0)
        {
            Die(attacker);
        }
    }

    /// <summary>
    /// 检查攻击者是否与目标是同一阵营
    /// </summary>
    private bool IsSameFaction(Unit attacker)
    {
        if (attacker == null || unit == null) return false;
        return attacker.IsEnemy() == unit.IsEnemy();
    }

    /// <summary>
    /// 治疗
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead || !isInitialized) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"[{unit?.name}] 恢复 {amount} 点生命，当前 HP: {currentHP}/{maxHP}");
    }

    /// <summary>增加最大生命值（同时治疗等量）</summary>
    public void AddMaxHP(int amount)
    {
        if (isDead || !isInitialized) return;
        maxHP += amount;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthInitialized?.Invoke(currentHP, maxHP);
    }

    /// <summary>
    /// 死亡处理：旋转倒下 → 粒子特效 → 延迟销毁
    /// </summary>
    private void Die(Unit killer)
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"[{unit?.name}] 已死亡！(击杀者: {killer?.name})");

        // 触发死亡事件
        OnDeath?.Invoke(killer);
        // 触发全局死亡音效
        Events.CallDeath(killer);

        // 禁用碰撞体（避免挡住射线/移动）
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = false;

        // 禁用移动/射击等脚本（防止死亡后还能行动）
        var moveAction = GetComponent<MoveAction>();
        if (moveAction != null) moveAction.enabled = false;

        var shootAction = GetComponent<ShootAction>();
        if (shootAction != null) shootAction.enabled = false;

        var enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null) enemyAI.enabled = false;

        // 1. 旋转倒下 + 粒子特效
        StartCoroutine(FallOverCoroutine());

        // 2. 延迟销毁
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>旋转倒下：绕 Z 轴旋转 90 度向侧面倒地</summary>
    private System.Collections.IEnumerator FallOverCoroutine()
    {
        // 随机左倒或右倒，避免穿模
        float sideDir = UnityEngine.Random.value > 0.5f ? 1f : -1f;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, 90f * sideDir);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            // ease-out：先快后慢
            t = 1f - (1f - t) * (1f - t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        // 倒下完成后播放粒子特效
        if (deathParticlePrefab != null)
        {
            var particle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            var ps = particle.GetComponent<ParticleSystem>();
            if (ps != null)
                Destroy(particle, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(particle, 3f);
        }
    }

    // === 查询接口 ===

    public float GetHPPercent() => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public int GetCurrentHP() => currentHP;
    public int GetMaxHP() => maxHP;
    public int GetBaseHP() => baseHP;
    public int GetBattleBonusHP() => battleBonusHP;
    public bool IsDead() => isDead;
    public bool IsAlive() => !isDead;
    public bool IsInitialized() => isInitialized;
}
