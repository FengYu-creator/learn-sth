using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    [SerializeField] private Animator unitAnimator;
    [SerializeField] private int maxMoveDistance = 4;
    [SerializeField] private float speed = 5f;

    private List<Vector3> pathWorldPositions;   // 路径上的世界坐标列表
    private int currentWaypointIndex;           // 当前走向第几个路径点
    private float stoppingdistance = 0.1f;

    void Update()
    {
        if (!isActive) return;

        Vector3 target = pathWorldPositions[currentWaypointIndex];
        Vector3 direction = (target - transform.position).normalized;

        if (Vector3.Distance(transform.position, target) > stoppingdistance)
        {
            transform.position += direction * speed * Time.deltaTime;
            unitAnimator.SetBool("IsWalking", true);
            // 触发脚步声事件
            Events.CallFootstep();
        }
        else
        {
            // 到达当前路径点，前往下一个
            currentWaypointIndex++;

            if (currentWaypointIndex >= pathWorldPositions.Count)
            {
                // 到达最终目标，停止脚步声
                unitAnimator.SetBool("IsWalking", false);
                Events.CallFootstepStop();
                isActive = false;
                OnActionComplete?.Invoke();
            }
        }

        float rotateSpeed = 15f;
        transform.forward = Vector3.Lerp(transform.forward, direction, rotateSpeed * Time.deltaTime);
    }

    public void SetMoveDistance(int distance)
    {
        maxMoveDistance = distance;
    }

    public int GetMoveDistance() => maxMoveDistance;

    public override void TakeAction(GridPosition gridPosition, Action OnActionComplete)
    {
        this.OnActionComplete = OnActionComplete;

        // 用 Pathfinding 计算最短路径（格子坐标）
        GridPosition startGP = unit.GetGridPosition();
        List<GridPosition> gridPath = Pathfinding.GetShortestPath(
            startGP,
            gridPosition,
            gp => LevelGrid.Instance.IsWalkable(gp),
            maxMoveDistance,
            (from, to) => !LevelGrid.Instance.HasThinWallBetween(from, to));

        // 防御：路径不可达时退回原地
        if (gridPath == null || gridPath.Count == 0)
        {
            isActive = false;
            OnActionComplete?.Invoke();
            return;
        }

        // 转为世界坐标
        pathWorldPositions = new List<Vector3>();
        foreach (var gp in gridPath)
        {
            pathWorldPositions.Add(LevelGrid.Instance.GetWorldPosition(gp));
        }

        currentWaypointIndex = 0;
        isActive = true;
    }

    public override List<GridPosition> GetValidGridPosition()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();
        return Pathfinding.GetReachablePositions(
            unitGridPosition,
            maxMoveDistance,
            gp => LevelGrid.Instance.IsWalkable(gp),
            (from, to) => !LevelGrid.Instance.HasThinWallBetween(from, to));
    }

    public override string GetActionName()
    {
        return "移动";
    }
}
