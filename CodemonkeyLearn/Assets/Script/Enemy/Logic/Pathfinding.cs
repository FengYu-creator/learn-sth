using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格寻路工具（BFS，支持 8 方向 + 防穿墙验证）
/// 用于 MoveAction 获取可达位置 + 敌人 AI 计算最短路径距离
/// </summary>
public static class Pathfinding
{
    // 4 个正交方向
    private static readonly Vector2Int[] CardinalDirections = {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

    // 4 个斜向方向 + 需要额外验证的两个正交格子（防止穿墙角）
    // (斜向方向, 需验证的正交格子1, 需验证的正交格子2)
    private static readonly (Vector2Int dir, Vector2Int check1, Vector2Int check2)[] DiagonalDirections = {
        (new(1, 1),   new(1, 0), new(0, 1)),
        (new(1, -1),  new(1, 0), new(0, -1)),
        (new(-1, 1),  new(-1, 0), new(0, 1)),
        (new(-1, -1), new(-1, 0), new(0, -1))
    };

    /// <summary>
    /// BFS 获取所有可达的网格位置（不含起点）
    /// canMoveBetween: 可选，检测两个相邻格子之间是否可以穿过（用于薄墙检测）
    /// </summary>
    public static List<GridPosition> GetReachablePositions(
        GridPosition start,
        int maxSteps,
        System.Func<GridPosition, bool> isWalkable,
        System.Func<GridPosition, GridPosition, bool> canMoveBetween = null)
    {
        List<GridPosition> result = new List<GridPosition>();
        Queue<(GridPosition pos, int dist)> queue = new Queue<(GridPosition, int)>();
        HashSet<GridPosition> visited = new HashSet<GridPosition>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();

            // 起点不计入可达列表
            if (dist > 0)
                result.Add(current);

            if (dist >= maxSteps) continue;

            // 正交方向
            foreach (var dir in CardinalDirections)
            {
                GridPosition next = new GridPosition(current.x + dir.x, current.z + dir.y);
                if (!TryMove(current, next, isWalkable, canMoveBetween, visited)) continue;

                visited.Add(next);
                queue.Enqueue((next, dist + 1));
            }

            // 斜向方向（需验证角落格子防止穿墙）
            foreach (var (dir, check1, check2) in DiagonalDirections)
            {
                GridPosition next = new GridPosition(current.x + dir.x, current.z + dir.y);
                if (visited.Contains(next) || !isWalkable(next))
                    continue;

                // 斜向移动必须保证两个正交相邻格子都可行走，且没有薄墙阻断
                GridPosition corner1 = new GridPosition(current.x + check1.x, current.z + check1.y);
                GridPosition corner2 = new GridPosition(current.x + check2.x, current.z + check2.y);

                if (!isWalkable(corner1) || !isWalkable(corner2))
                    continue;
                if (canMoveBetween != null && (!canMoveBetween(current, corner1) || !canMoveBetween(current, corner2)))
                    continue;

                visited.Add(next);
                queue.Enqueue((next, dist + 1));
            }
        }

        return result;
    }

    /// <summary>
    /// BFS 计算从 start 到 goal 的最短路径距离（步数）
    /// 不可达时返回 int.MaxValue
    /// canMoveBetween: 可选，检测两个相邻格子之间是否可以穿过（用于薄墙检测）
    /// </summary>
    public static int GetShortestPathDistance(
        GridPosition start,
        GridPosition goal,
        System.Func<GridPosition, bool> isWalkable,
        int maxSteps = 999,
        System.Func<GridPosition, GridPosition, bool> canMoveBetween = null)
    {
        if (start == goal) return 0;

        Queue<(GridPosition pos, int dist)> queue = new Queue<(GridPosition, int)>();
        HashSet<GridPosition> visited = new HashSet<GridPosition>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            if (dist >= maxSteps) continue;

            // 正交方向
            foreach (var dir in CardinalDirections)
            {
                GridPosition next = new GridPosition(current.x + dir.x, current.z + dir.y);
                if (next == goal) return dist + 1;
                if (!TryMove(current, next, isWalkable, canMoveBetween, visited)) continue;

                visited.Add(next);
                queue.Enqueue((next, dist + 1));
            }

            // 斜向方向（需验证角落格子防止穿墙）
            foreach (var (dir, check1, check2) in DiagonalDirections)
            {
                GridPosition next = new GridPosition(current.x + dir.x, current.z + dir.y);
                if (next == goal) return dist + 1;
                if (visited.Contains(next) || !isWalkable(next))
                    continue;

                // 斜向移动必须保证两个正交相邻格子都可行走，且没有薄墙阻断
                GridPosition corner1 = new GridPosition(current.x + check1.x, current.z + check1.y);
                GridPosition corner2 = new GridPosition(current.x + check2.x, current.z + check2.y);

                if (!isWalkable(corner1) || !isWalkable(corner2))
                    continue;
                if (canMoveBetween != null && (!canMoveBetween(current, corner1) || !canMoveBetween(current, corner2)))
                    continue;

                visited.Add(next);
                queue.Enqueue((next, dist + 1));
            }
        }

        return int.MaxValue;
    }

    /// <summary>
    /// BFS 获取从 start 到 goal 的最短路径（包含起点和终点）
    /// 不可达时返回 null
    /// </summary>
    public static List<GridPosition> GetShortestPath(
        GridPosition start,
        GridPosition goal,
        System.Func<GridPosition, bool> isWalkable,
        int maxSteps = 999,
        System.Func<GridPosition, GridPosition, bool> canMoveBetween = null)
    {
        if (start == goal)
            return new List<GridPosition> { start };

        Queue<(GridPosition pos, int dist)> queue = new Queue<(GridPosition, int)>();
        HashSet<GridPosition> visited = new HashSet<GridPosition>();
        Dictionary<GridPosition, GridPosition> parent = new Dictionary<GridPosition, GridPosition>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            if (dist >= maxSteps) continue;

            // 正交方向
            foreach (var dir in CardinalDirections)
            {
                GridPosition next = new GridPosition(current.x + dir.x, current.z + dir.y);
                if (visited.Contains(next) || !isWalkable(next))
                    continue;
                if (canMoveBetween != null && !canMoveBetween(current, next))
                    continue;

                parent[next] = current;
                if (next == goal)
                    return BuildPath(parent, start, goal);

                visited.Add(next);
                queue.Enqueue((next, dist + 1));
            }

            // 斜向方向（需验证角落格子防止穿墙）
            foreach (var (dir, check1, check2) in DiagonalDirections)
            {
                GridPosition next = new GridPosition(current.x + dir.x, current.z + dir.y);
                if (visited.Contains(next) || !isWalkable(next))
                    continue;

                GridPosition corner1 = new GridPosition(current.x + check1.x, current.z + check1.y);
                GridPosition corner2 = new GridPosition(current.x + check2.x, current.z + check2.y);

                if (!isWalkable(corner1) || !isWalkable(corner2))
                    continue;
                if (canMoveBetween != null && (!canMoveBetween(current, corner1) || !canMoveBetween(current, corner2)))
                    continue;

                parent[next] = current;
                if (next == goal)
                    return BuildPath(parent, start, goal);

                visited.Add(next);
                queue.Enqueue((next, dist + 1));
            }
        }

        return null; // 不可达
    }

    /// <summary>
    /// 根据 parent 字典回溯路径（从 goal 到 start）
    /// </summary>
    private static List<GridPosition> BuildPath(
        Dictionary<GridPosition, GridPosition> parent,
        GridPosition start,
        GridPosition goal)
    {
        List<GridPosition> path = new List<GridPosition>();
        GridPosition current = goal;
        while (!current.Equals(start))
        {
            path.Add(current);
            current = parent[current];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 尝试从 current 正交移动到 next，检测通行条件和薄墙
    /// </summary>
    private static bool TryMove(
        GridPosition current,
        GridPosition next,
        System.Func<GridPosition, bool> isWalkable,
        System.Func<GridPosition, GridPosition, bool> canMoveBetween,
        HashSet<GridPosition> visited)
    {
        if (visited.Contains(next) || !isWalkable(next))
            return false;
        if (canMoveBetween != null && !canMoveBetween(current, next))
            return false;
        return true;
    }
}
