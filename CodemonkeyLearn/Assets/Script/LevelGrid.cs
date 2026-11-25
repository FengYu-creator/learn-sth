using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public static LevelGrid Instance{get; private set;}
    [SerializeField] Transform gridDebugObjectPrefab;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private LayerMask thinWallLayerMask;

    [Header("网格设置")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 2f;

    [Header("关卡参考点")]
    [SerializeField] private Transform level1Origin;   // 关卡1 网格原点
    [SerializeField] private Transform level2Origin;   // 关卡2 网格原点
    [SerializeField] private Transform level3Origin;   // 关卡3 网格原点

    [Header("关卡场景预制体")]
    [SerializeField] private GameObject level1Scene;   // 关卡1 场景内容
    [SerializeField] private GameObject level2Scene;   // 关卡2 场景内容
    [SerializeField] private GameObject level3Scene;   // 关卡3 场景内容

    /// <summary>当前激活的关卡索引（1/2/3）</summary>
    public int CurrentLevel { get; private set; } = 1;

    private GridSystem gridSystem;
    private void Awake()
    {

        if(Instance!=null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Vector3 origin = level1Origin != null ? level1Origin.position : Vector3.zero;
        gridSystem = new GridSystem(gridWidth, gridHeight, cellSize, origin);
        gridSystem.CreateDebugObjects(gridDebugObjectPrefab);

        // 初始只激活关卡1
        ActivateLevelScene(1);

    }

    /// <summary>
    /// 切换到指定关卡的网格原点
    /// </summary>
    public void SwitchToLevel(int levelIndex)
    {
        CurrentLevel = levelIndex;
        Transform target = GetOriginByLevel(levelIndex);
        if (target == null) return;

        // 结束当前战斗（如果正在战斗中）
        if (UnitActionSystem.Instance != null && UnitActionSystem.Instance.IsBattleStart)
            UnitActionSystem.Instance.OnBattleEnd();

        // 销毁旧关卡所有单位
        DestroyAllUnits();

        // 清空搜刮临时背包
        LootInventory.Instance?.Clear();

        // 切换场景内容
        ActivateLevelScene(levelIndex);

        // 清理旧网格 debug 对象 + 重建网格
        gridSystem.DestroyDebugObjects();
        gridSystem = new GridSystem(gridWidth, gridHeight, cellSize, target.position);

        // 重建可视化格子（移动范围显示）
        GridSystemVisual.Instance?.RebuildVisual();

        // 重置回合系统到玩家回合
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.ResetToPlayerTurn();

        // 重置敌人生成器
        EnemySpawner.ResetAll();

        // 重置搜刮点
        ResetAllLootablePoints();
    }

    /// <summary>重置所有搜刮点（重新激活 + 清除已搜刮标记）</summary>
    private void ResetAllLootablePoints()
    {
        var points = FindObjectsByType<LootablePoint>(FindObjectsSortMode.None);
        foreach (var p in points)
        {
            p.ResetPoint();
        }
        Debug.Log($"[LevelGrid] 已重置 {points.Length} 个搜刮点");
    }

    private void ActivateLevelScene(int levelIndex)
    {
        if (level1Scene != null) level1Scene.SetActive(levelIndex == 1);
        if (level2Scene != null) level2Scene.SetActive(levelIndex == 2);
        if (level3Scene != null) level3Scene.SetActive(levelIndex == 3);
    }

    /// <summary>
    /// 销毁所有单位（玩家 + 敌人）
    /// </summary>
    public void DestroyAllUnits()
    {
        // 先清理敌人 AI 状态，避免协程卡死
        EnemyAI.ResetAll();

        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var u in allUnits)
        {
            // 先从网格移除（加边界保护，防止关卡切换后坐标越界）
            GridObject gridObj = gridSystem.GetGridObject(u.GetGridPosition());
            if (gridObj != null)
                gridObj.RemoveUnit(u);
            else
                Debug.LogWarning($"[LevelGrid] 单位 {u.name} 的网格位置 {u.GetGridPosition()} 超出网格范围，跳过网格移除");

            Destroy(u.gameObject);
        }
        Debug.Log($"[LevelGrid] 已销毁 {allUnits.Length} 个单位");
    }

    private Transform GetOriginByLevel(int levelIndex)
    {
        return levelIndex switch
        {
            1 => level1Origin,
            2 => level2Origin,
            3 => level3Origin,
            _ => null
        };
    }
    public void AddUnitAtGridPosition(GridPosition gridPosition,Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject .AddUnit(unit);
    }
    public List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject .GetUnitList();
    }
    public void RemoveGridUnit(GridPosition gridPosition,Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject.RemoveUnit(unit );
    }

    public void UnitMovedGridPosition(Unit unit , GridPosition fromGridPosition , GridPosition toGridPosition)
    {
        RemoveGridUnit(fromGridPosition, unit);
        AddUnitAtGridPosition(toGridPosition, unit);

    }

    //透传函数，通过此脚本的方法调用gridSystem的方法
    public GridPosition GetGridPosition(Vector3 worldPosition) => gridSystem.GetGridPosition(worldPosition);
    public Vector3 GetWorldPosition(GridPosition gridPosition) => gridSystem.GetWorldPosition(gridPosition);
    public bool IsValidGridPosition(GridPosition gridPosition) => gridSystem.IsValidGridPosition(gridPosition);
    public int GetWidth()=>gridSystem.GetWidth();
    public int GetHeight()=>gridSystem.GetHeight();

    // 参考点公开访问（根据当前关卡返回对应原点）
    public Transform OriginTransform => GetOriginByLevel(CurrentLevel);
    public Vector3 GetSpawnWorldPosition(Transform spawnPoint) => spawnPoint != null ? spawnPoint.position : Vector3.zero;
    public GridPosition GetSpawnGridPosition(Transform spawnPoint) => spawnPoint != null ? GetGridPosition(spawnPoint.position) : new GridPosition(0, 0);

    public bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.HasAnyUnit();
    }

    /// <summary>
    /// 网格格子是否可通行（有效 + 无墙 + 无单位）
    /// </summary>
    public bool IsWalkable(GridPosition gridPosition)
    {
        if (!IsValidGridPosition(gridPosition)) return false;
        if (HasAnyUnitOnGridPosition(gridPosition)) return false;
        return !HasWallAtGridPosition(gridPosition);
    }

    /// <summary>
    /// 检测指定网格位置是否有厚墙壁（用 wallLayerMask 检测）
    /// </summary>
    private bool HasWallAtGridPosition(GridPosition gridPosition)
    {
        float cellSize = GetWorldPosition(new GridPosition(1, 0)).x - GetWorldPosition(new GridPosition(0, 0)).x;
        Vector3 worldPos = GetWorldPosition(gridPosition);
        Vector3 boxSize = new Vector3(cellSize, 5f, cellSize);
        Collider[] colliders = Physics.OverlapBox(worldPos, boxSize * 0.5f, Quaternion.identity, wallLayerMask);
        return colliders.Length > 0;
    }

    /// <summary>
    /// 检测两个相邻格子之间的边界上是否有薄墙（用 thinWallLayerMask 检测）
    /// </summary>
    public bool HasThinWallBetween(GridPosition from, GridPosition to)
    {
        float cellSize = GetWorldPosition(new GridPosition(1, 0)).x - GetWorldPosition(new GridPosition(0, 0)).x;
        Vector3 midPoint = (GetWorldPosition(from) + GetWorldPosition(to)) * 0.5f;

        Vector3 boxSize;
        if (from.x != to.x)
            boxSize = new Vector3(0.1f, 5f, cellSize);
        else
            boxSize = new Vector3(cellSize, 5f, 0.1f);

        Collider[] colliders = Physics.OverlapBox(midPoint, boxSize * 0.5f, Quaternion.identity, thinWallLayerMask);
        return colliders.Length > 0;
    }
}
