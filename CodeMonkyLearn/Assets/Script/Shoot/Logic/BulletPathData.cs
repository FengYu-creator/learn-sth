using UnityEngine;
using System;

/// <summary>
/// 子弹路径数据，包含子弹飞行的所有必要信息
/// 由 ShootMath 计算，传递给 Bullet
/// </summary>
[Serializable]
public class BulletPathData
{
    /// <summary>子弹飞行路径点（发射点已在 Bullet 外部处理）</summary>
    public Vector3[] pathPoints;

    /// <summary>特殊碰撞点（敌人位置），如果有命中目标则不为 null</summary>
    public Vector3? specialPoint;

    /// <summary>命中目标组件引用</summary>
    public UnitStat hitTarget;

    /// <summary>最终折射方向（当没有命中敌人时，子弹最后沿此方向飞出）</summary>
    public Vector3 finalDirection;

    /// <summary>子弹存活时间（秒）</summary>
    public float timeout;

    /// <summary>伤害值</summary>
    public int damage;

    /// <summary>是否有命中目标</summary>
    public bool HasHitTarget => hitTarget != null;

    /// <summary>是否需要沿 finalDirection 飞出</summary>
    public bool HasFinalDirection => finalDirection != Vector3.zero;

    /// <summary>是否有特殊碰撞点</summary>
    public bool HasSpecialPoint => specialPoint.HasValue;

    /// <summary>获取中间碰撞点数组（不含发射点）</summary>
    public Vector3[] GetIntermediatePoints()
    {
        if (pathPoints == null || pathPoints.Length <= 1)
            return Array.Empty<Vector3>();
        // 跳过第一个点（发射点）
        Vector3[] result = new Vector3[pathPoints.Length - 1];
        Array.Copy(pathPoints, 1, result, 0, result.Length);
        return result;
    }
}
