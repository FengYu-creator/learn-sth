using UnityEngine;

/// <summary>
/// 敌人生成点 — 隔一回合尝试生成敌人，最大生成3个
/// 玩家在10格范围内 → 不生成
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private EnemyData enemyData;        // 要生成的敌人模板
    [SerializeField] private int maxSpawnCount = 3;      // 最大生成数量
    [SerializeField] private bool drawGizmo = true;

    [Header("生成条件")]
    [SerializeField] private float playerDetectionRadius = 10f; // 玩家靠近此距离则不生成

    private int spawnCount = 0;
    private bool active = true;
    private int turnSkipCounter = 0;  // 隔回合计数

    private void Start()
    {
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.OnTurnChanged += OnTurnChanged;
    }

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.OnTurnChanged -= OnTurnChanged;
    }

    private void OnTurnChanged(object sender, System.EventArgs e)
    {
        if (!active)
        {
            Debug.Log($"[EnemySpawner] {name} active=false，跳过");
            return;
        }
        if (!isActiveAndEnabled)
        {
            Debug.Log($"[EnemySpawner] {name} isActiveAndEnabled=false（父节点禁用？），跳过");
            return;
        }
        if (!TurnSystem.Instance.IsPlayerTurn()) return;

        // 检测玩家是否在10格范围内 → 不生成
        if (IsAnyPlayerInRange())
        {
            Debug.Log($"[EnemySpawner] {name} 玩家在{playerDetectionRadius}格范围内，跳过生成");
            return;
        }

        // 隔一回合生成一个：每2个玩家回合生成一次
        turnSkipCounter++;
        Debug.Log($"[EnemySpawner] {name} turnSkipCounter={turnSkipCounter}，{(turnSkipCounter < 2 ? "等待下一回合" : "触发生成")}");
        if (turnSkipCounter < 2) return;
        turnSkipCounter = 0;

        SpawnEnemy();
    }

    /// <summary>检测是否有玩家单位在指定半径内</summary>
    private bool IsAnyPlayerInRange()
    {
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var u in allUnits)
        {
            if (u.IsEnemy()) continue;
            var hs = u.GetComponent<HealthSystem>();
            if (hs == null || !hs.IsAlive()) continue;

            float dist = Vector3.Distance(transform.position, u.transform.position);
            if (dist <= playerDetectionRadius)
                return true;
        }
        return false;
    }

    private void SpawnEnemy()
    {
        if (enemyData == null)
        {
            Debug.LogWarning($"[EnemySpawner] {name} 没有配置 EnemyData");
            return;
        }

        if (StatManager.Instance == null)
        {
            Debug.LogWarning("[EnemySpawner] StatManager 不存在");
            return;
        }

        StatManager.Instance.PlaceEnemy(enemyData, transform.position);
        spawnCount++;

        Debug.Log($"[EnemySpawner] {name} 生成 {enemyData.enemyName} ({spawnCount}/{maxSpawnCount})");

        if (spawnCount >= maxSpawnCount)
        {
            active = false;
            Debug.Log($"[EnemySpawner] {name} 已达到最大生成次数，停止生成");
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        Gizmos.color = active ? Color.red : Color.gray;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // 绘制玩家检测半径
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
    }

    /// <summary>重置所有敌人生成器（切换关卡时调用）</summary>
    public static void ResetAll()
    {
        var allSpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        int resetCount = 0;
        foreach (var s in allSpawners)
        {
            // 只重置当前激活场景中的生成器（跳过被禁用的旧关卡场景）[DEBUG]
            if (!s.isActiveAndEnabled)
            {
                Debug.Log($"[EnemySpawner] ResetAll 跳过 {s.name}（父节点已禁用，activeInHierarchy=false）");
                continue;
            }
            Debug.Log($"[EnemySpawner] ResetAll 重置 {s.name}，之前 spawnCount={s.spawnCount}, active={s.active}");
            s.spawnCount = 0;
            s.active = true;
            resetCount++;
        }
        Debug.Log($"[EnemySpawner] 已重置 {resetCount}/{allSpawners.Length} 个生成器（跳过了 {allSpawners.Length - resetCount} 个禁用的）");
    }
}
