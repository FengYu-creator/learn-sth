using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人 AI — 串行逐个行动 + 射线遮挡检测 + 寻路移动 + 寻找掩护
/// 
/// 主循环（每回合最多 2 次行动）：
///   1. 优先射击：射程内有可见玩家 → 射击（本回合结束）
///   2. 射不了 → 移动（靠近/找掩护）
///   3. 移动后回到第 1 步（移动后可能获得射击视野）
///   4. 2倍射程内无玩家 → 巡逻（随机走）
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("场景预放置时直接拖入 SO，代码生成时自动赋值")]
    [SerializeField] private EnemyData enemyData;

    private Unit unit;
    private UnitStat unitStat;
    private MoveAction moveAction;
    private ShootAction shootAction;

    private int actionsThisTurn = 0;
    private const int MAX_ACTIONS = 2;

    /// <summary>本回合是否已射击过（每回合限一次）</summary>
    private bool hasShotThisTurn = false;

    // === 串行调度 ===
    private static List<EnemyAI> activeEnemies = new List<EnemyAI>();
    private static bool isProcessingTurn = false;

    // 四方向偏移（用于掩护检测）
    private static readonly Vector2Int[] GridDirections = {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

    private void Awake()
    {
        unit = GetComponent<Unit>();
        unitStat = GetComponent<UnitStat>();
        moveAction = GetComponent<MoveAction>();
        shootAction = GetComponent<ShootAction>();

        if (enemyData != null && unitStat != null)
            unitStat.enemyData = enemyData;
    }

    private void OnEnable()
    {
        activeEnemies.Add(this);
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.OnTurnChanged += OnTurnChanged;
        Events.OnEnemySpawned += OnEnemySpawned;
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.OnTurnChanged -= OnTurnChanged;
        Events.OnEnemySpawned -= OnEnemySpawned;
    }

    private void OnEnemySpawned(Unit spawnedUnit, EnemyData data)
    {
        if (spawnedUnit == unit)
            enemyData = data;
    }

    // ============================================================
    //  回合调度：串行处理所有敌人
    // ============================================================

    private void OnTurnChanged(object sender, System.EventArgs e)
    {
        if (TurnSystem.Instance.IsPlayerTurn()) return;
        if (isProcessingTurn) return;

        if (activeEnemies.Count > 0 && activeEnemies[0] == this)
        {
            isProcessingTurn = true;
            StartCoroutine(ProcessAllEnemies());
        }
    }

    private IEnumerator ProcessAllEnemies()
    {
        var snapshot = new List<EnemyAI>(activeEnemies);

        foreach (var enemy in snapshot)
        {
            if (enemy == null || !enemy.gameObject.activeSelf) continue;

            var hs = enemy.GetComponent<HealthSystem>();
            if (hs != null && !hs.IsAlive()) continue;

            yield return enemy.StartCoroutine(enemy.DoEnemyTurn());
        }

        isProcessingTurn = false;

        if (TurnSystem.Instance != null && !TurnSystem.Instance.IsPlayerTurn())
            TurnSystem.Instance.NextTurn();
    }

    public static void ResetAll()
    {
        foreach (var e in activeEnemies)
        {
            if (e != null)
                e.StopAllCoroutines();
        }
        activeEnemies.Clear();
        isProcessingTurn = false;
    }

    private IEnumerator DoEnemyTurn()
    {
        yield return new WaitForSeconds(0.15f);

        actionsThisTurn = 0;
        hasShotThisTurn = false;

        // === 检测 2倍射程内是否有玩家 ===
        Unit[] players = FindAlivePlayerUnits();
        int range = unitStat.battleStats.OBRange;
        int detectionRange = range * 2;

        Unit nearestPlayer = FindNearestPlayer(players);
        float nearestDist = nearestPlayer != null
            ? Vector3.Distance(transform.position, nearestPlayer.transform.position)
            : float.MaxValue;

        // 2倍射程内没有玩家 → 巡逻（随机走动）
        if (nearestPlayer == null || nearestDist > detectionRange)
        {
            TryPatrol();
            while (moveAction != null && moveAction.IsPerforming)
                yield return null;
            yield break;
        }

        // === 主循环：每轮优先尝试射击，射不了才移动 ===
        while (actionsThisTurn < MAX_ACTIONS && unit.GetActionPoint() > 0)
        {
            // 1. 先尝试射击（最高优先级）
            if (!hasShotThisTurn)
            {
                bool shot = TryShootAtBestTarget();
                if (shot)
                {
                    hasShotThisTurn = true;
                    actionsThisTurn++;
                    yield return new WaitForSeconds(0.3f);

                    // 射击完还有行动点 → 找掩护
                    if (actionsThisTurn >= MAX_ACTIONS || unit.GetActionPoint() <= 0)
                        break;
                    bool tookCover = TryTakeCover();
                    if (tookCover)
                    {
                        actionsThisTurn++;
                        while (moveAction != null && moveAction.IsPerforming)
                            yield return null;
                        yield return new WaitForSeconds(0.15f);
                    }
                    continue;
                }
            }

            // 2. 射不了（已射过 或 无可见目标）→ 尝试移动
            if (actionsThisTurn >= MAX_ACTIONS || unit.GetActionPoint() <= 0)
                break;

            // 已有玩家在射程内但不可见 → 找掩护，不要直冲
            bool anyPlayerInRange = IsAnyPlayerInRange(FindAlivePlayerUnits());
            bool moved;
            if (anyPlayerInRange)
                moved = TryTakeCover();
            else
                moved = TryMoveToNearestPlayer(FindAlivePlayerUnits()) || TryTakeCover();

            if (!moved) break; // 动不了就结束

            actionsThisTurn++;
            while (moveAction != null && moveAction.IsPerforming)
                yield return null;
            yield return new WaitForSeconds(0.15f);

            // 3. 移动后，回到循环顶部再次尝试射击
        }
    }

    // ============================================================
    //  行为决策
    // ============================================================

    /// <summary>
    /// 尝试射击射程内最近的可见玩家。成功返回 true。
    /// </summary>
    private bool TryShootAtBestTarget()
    {
        if (shootAction == null) return false;
        if (hasShotThisTurn) return false;
        if (unit.GetActionPoint() <= 0) return false;
        if (enemyData == null || enemyData.behaviorType == EnemyBehaviorType.Melee) return false;

        Unit[] players = FindAlivePlayerUnits();
        if (players.Length == 0) return false;

        int range = unitStat.battleStats.OBRange;

        // 找射程内最近可见玩家
        Unit bestTarget = null;
        float bestDist = float.MaxValue;
        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist <= range && dist < bestDist && CanSeeTarget(p))
            {
                bestDist = dist;
                bestTarget = p;
            }
        }

        if (bestTarget == null) return false;

        if (!unit.TrySpendActionPoint(1))
            return false;

        Vector3 dir = (bestTarget.transform.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.forward = dir;

        shootAction.ExecuteShootAt(bestTarget.transform.position);
        return true;
    }

    /// <summary>检查是否有任何玩家在射程内（不管可见与否）</summary>
    private bool IsAnyPlayerInRange(Unit[] players)
    {
        if (players.Length == 0) return false;
        int range = unitStat.battleStats.OBRange;
        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist <= range) return true;
        }
        return false;
    }

    // ============================================================
    //  寻找掩护
    // ============================================================

    /// <summary>
    /// 寻找掩护：检测合法移动位置中哪些有墙遮挡（薄墙 + 厚墙）
    /// Ranged  — 射程内选离玩家最远的墙后格子
    /// Melee   — 选离玩家最近的墙后格子
    /// </summary>
    private bool TryTakeCover()
    {
        if (moveAction == null) return false;
        if (!unit.CanSpendActionPointsToTakeAction(moveAction)) return false;

        if (enemyData == null) return false;
        if (enemyData.behaviorType == EnemyBehaviorType.Special ||
            enemyData.behaviorType == EnemyBehaviorType.Neutral)
            return false;

        List<GridPosition> validMoves = moveAction.GetValidGridPosition();
        if (validMoves.Count == 0) return false;

        Unit[] players = FindAlivePlayerUnits();
        if (players.Length == 0) return false;

        EnemyBehaviorType type = enemyData.behaviorType;
        int obRange = unitStat.battleStats.OBRange;

        GridPosition bestCover = new GridPosition(-999, -999);
        float bestScore = (type == EnemyBehaviorType.Ranged) ? float.MinValue : float.MaxValue;

        foreach (var move in validMoves)
        {
            Vector3 moveWorldPos = LevelGrid.Instance.GetWorldPosition(move);

            // 检测该格是否有墙提供掩护（薄墙或厚墙）
            bool wallShieldsPlayer = false;
            float nearestShieldedDist = float.MaxValue;

            foreach (var dir in GridDirections)
            {
                GridPosition neighbor = new GridPosition(move.x + dir.x, move.z + dir.y);
                if (!LevelGrid.Instance.IsValidGridPosition(neighbor)) continue;

                // 检测薄墙
                bool hasThinWall = LevelGrid.Instance.HasThinWallBetween(move, neighbor);
                // 检测厚墙：邻居格有厚墙也算掩护
                bool hasThickWall = HasThickWallAt(neighbor);

                if (!hasThinWall && !hasThickWall) continue;

                Vector3 wallDir = new Vector3(dir.x, 0f, dir.y);
                foreach (var p in players)
                {
                    Vector3 toPlayer = (p.transform.position - moveWorldPos).normalized;
                    if (Vector3.Dot(toPlayer, wallDir) > 0.3f)
                    {
                        wallShieldsPlayer = true;
                        float dist = Vector3.Distance(moveWorldPos, p.transform.position);
                        if (dist < nearestShieldedDist) nearestShieldedDist = dist;
                    }
                }
            }

            if (!wallShieldsPlayer) continue;

            float score = nearestShieldedDist;

            if (type == EnemyBehaviorType.Ranged)
            {
                // 远程：选离玩家最远的掩护点（但不超过自己射程）
                if (score > bestScore && score <= obRange)
                { bestScore = score; bestCover = move; }
                else if (bestScore <= 0 && score > bestScore)
                { bestScore = score; bestCover = move; } // 没有射程内的就选最远的
            }
            else
            {
                if (score < bestScore)
                { bestScore = score; bestCover = move; }
            }
        }

        if (bestCover.x == -999) return false;

        if (unit.TrySpendActionPointToTakeAction(moveAction))
        {
            moveAction.TakeAction(bestCover, null);
            return true;
        }
        return false;
    }

    /// <summary>检测指定格子是否有厚墙</summary>
    private bool HasThickWallAt(GridPosition gp)
    {
        Vector3 worldPos = LevelGrid.Instance.GetWorldPosition(gp);
        float cellSize = LevelGrid.Instance.GetWorldPosition(new GridPosition(1, 0)).x
                       - LevelGrid.Instance.GetWorldPosition(new GridPosition(0, 0)).x;
        Vector3 boxSize = new Vector3(cellSize * 0.45f, 5f, cellSize * 0.45f);

        LayerMask wallMask = shootAction != null
            ? shootAction.WallLayerMask
            : LayerMask.GetMask("Wall");
        Collider[] colliders = Physics.OverlapBox(worldPos, boxSize, Quaternion.identity, wallMask);
        return colliders.Length > 0;
    }

    // ============================================================
    //  移动逻辑（寻路）
    // ============================================================

    private bool TryMoveToNearestPlayer(Unit[] players)
    {
        if (players.Length == 0 || moveAction == null) return false;

        List<GridPosition> validMoves = moveAction.GetValidGridPosition();
        if (validMoves.Count == 0) return false;

        GridPosition myPos = unit.GetGridPosition();

        Unit nearest = null;
        int nearestBfsDist = int.MaxValue;
        foreach (var p in players)
        {
            GridPosition targetPos = p.GetGridPosition();
            int bfsDist = Pathfinding.GetShortestPathDistance(
                myPos, targetPos,
                gp => IsWalkableIgnoreGoal(gp, targetPos),
                canMoveBetween: (from, to) => !LevelGrid.Instance.HasThinWallBetween(from, to));
            if (bfsDist < nearestBfsDist)
            {
                nearestBfsDist = bfsDist;
                nearest = p;
            }
        }
        if (nearest == null) return false;

        GridPosition playerGrid = nearest.GetGridPosition();

        GridPosition bestMove = validMoves[0];
        int bestDist = int.MaxValue;
        foreach (var move in validMoves)
        {
            int d = Pathfinding.GetShortestPathDistance(
                move, playerGrid,
                gp => IsWalkableIgnoreGoal(gp, playerGrid),
                canMoveBetween: (from, to) => !LevelGrid.Instance.HasThinWallBetween(from, to));
            if (d < bestDist)
            {
                bestDist = d;
                bestMove = move;
            }
            if (bestDist == 0) break;
        }

        if (unit.TrySpendActionPointToTakeAction(moveAction))
        {
            moveAction.TakeAction(bestMove, null);
            return true;
        }
        return false;
    }

    private static bool IsWalkableIgnoreGoal(GridPosition gp, GridPosition goalPos)
    {
        if (gp == goalPos) return LevelGrid.Instance.IsValidGridPosition(gp);
        return LevelGrid.Instance.IsWalkable(gp);
    }

    // ============================================================
    //  射线遮挡检测
    // ============================================================

    private bool CanSeeTarget(Unit target)
    {
        if (shootAction == null || shootAction.FirePoint == null) return false;

        Vector3 origin = shootAction.FirePoint.position;
        Vector3 dir = (target.transform.position - origin).normalized;
        float dist = Vector3.Distance(origin, target.transform.position);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, shootAction.WallLayerMask))
        {
            return false;
        }
        return true;
    }

    // ============================================================
    //  巡逻逻辑
    // ============================================================

    /// <summary>
    /// 巡逻：朝最近玩家的方向随机距离走动。未发现玩家时调用。
    /// </summary>
    private void TryPatrol()
    {
        if (moveAction == null) return;
        if (!unit.CanSpendActionPointsToTakeAction(moveAction)) return;

        List<GridPosition> validMoves = moveAction.GetValidGridPosition();
        if (validMoves.Count == 0) return;

        // 朝最近玩家方向随机距离巡逻
        GridPosition patrolTarget;
        Unit[] players = FindAlivePlayerUnits();
        if (players.Length > 0)
        {
            GridPosition myPos = unit.GetGridPosition();
            GridPosition playerPos = FindNearestPlayer(players).GetGridPosition();
            int myDistToPlayer = Pathfinding.GetShortestPathDistance(
                myPos, playerPos,
                gp => IsWalkableIgnoreGoal(gp, playerPos),
                canMoveBetween: (from, to) => !LevelGrid.Instance.HasThinWallBetween(from, to));

            // 筛选出"比当前位置更靠近玩家"的移动目标
            List<GridPosition> closerMoves = new List<GridPosition>();
            foreach (var move in validMoves)
            {
                int moveDist = Pathfinding.GetShortestPathDistance(
                    move, playerPos,
                    gp => IsWalkableIgnoreGoal(gp, playerPos),
                    canMoveBetween: (from, to) => !LevelGrid.Instance.HasThinWallBetween(from, to));
                if (moveDist < myDistToPlayer)
                    closerMoves.Add(move);
            }

            // 有更近的格子就随机选，否则在当前可达格中随机选
            patrolTarget = closerMoves.Count > 0
                ? closerMoves[Random.Range(0, closerMoves.Count)]
                : validMoves[Random.Range(0, validMoves.Count)];
        }
        else
        {
            // 没有玩家存活时随机走
            patrolTarget = validMoves[Random.Range(0, validMoves.Count)];
        }

        if (unit.TrySpendActionPointToTakeAction(moveAction))
        {
            moveAction.TakeAction(patrolTarget, null);
        }
    }

    // ============================================================
    //  工具方法
    // ============================================================

    private Unit[] FindAlivePlayerUnits()
    {
        List<Unit> result = new List<Unit>();
        foreach (var u in FindObjectsOfType<Unit>())
        {
            if (u.IsEnemy()) continue;
            var hs = u.GetComponent<HealthSystem>();
            if (hs != null && hs.IsAlive())
                result.Add(u);
        }
        return result.ToArray();
    }

    private Unit FindNearestPlayer(Unit[] players)
    {
        Unit nearest = null;
        float minDist = float.MaxValue;
        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = p;
            }
        }
        return nearest;
    }
}
