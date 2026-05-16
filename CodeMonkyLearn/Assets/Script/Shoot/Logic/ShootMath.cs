using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 射击路径计算工具类
/// 负责计算反弹射击的完整路径，包括墙壁反射点和命中目标
/// </summary>
public static class ShootMath
{
    /// <summary>
    /// 计算完整射击路径
    /// </summary>
    /// <param name="startPos">发射位置</param>
    /// <param name="initialDirection">初始射击方向（单位向量）</param>
    /// <param name="maxRange">最大射程（单位）</param>
    /// <param name="maxReflectCount">最大反射次数</param>
    /// <param name="wallLayerMask">墙壁层</param>
    /// <param name="unitLayerMask">单位层</param>
    /// <param name="damage">伤害值（传递给命中回调）</param>
    /// <returns>子弹路径数据</returns>
    public static BulletPathData CalculatePath(
        Vector3 startPos,
        Vector3 initialDirection,
        float maxRange,
        int maxReflectCount,
        LayerMask wallLayerMask,
        LayerMask unitLayerMask,
        int damage)
    {
        var data = new BulletPathData
        {
            pathPoints = new List<Vector3> { startPos }.ToArray(),
            specialPoint = null,
            hitTarget = null,
            finalDirection = Vector3.zero,
            timeout = 0f,
            damage = damage
        };

        List<Vector3> points = new List<Vector3> { startPos };
        Vector3 origin = startPos;
        Vector3 direction = initialDirection.normalized;
        float remainingRange = maxRange;

        for (int i = 0; i < maxReflectCount; i++)
        {
            if (remainingRange <= 0f) break;

            // 1. 先检测是否有敌人（在墙壁前面）
            if (Physics.Raycast(origin, direction, out RaycastHit unitHit, remainingRange, unitLayerMask))
            {
                // 检查敌人前面是否有墙
                float distToUnit = Vector3.Distance(origin, unitHit.point);
                if (!Physics.Raycast(origin, direction, out RaycastHit wallHitCheck, distToUnit - 0.01f, wallLayerMask))
                {
                    // 敌人前面没墙，命中！
                    points.Add(unitHit.point);
                    data.pathPoints = points.ToArray();
                    data.specialPoint = unitHit.point;
                    data.hitTarget = unitHit.collider.GetComponent<UnitStat>();
                    data.finalDirection = Vector3.zero;
                    return data;
                }
            }

            // 2. 检测墙壁
            if (Physics.Raycast(origin, direction, out RaycastHit wallHit, remainingRange, wallLayerMask))
            {
                // 撞墙，记录碰撞点并反射
                points.Add(wallHit.point);
                direction = Vector3.Reflect(direction, wallHit.normal);
                float traveled = Vector3.Distance(origin, wallHit.point);
                remainingRange -= traveled;
                origin = wallHit.point + direction * 0.01f;
            }
            else
            {
                // 未撞墙，沿方向飞出直到射程耗尽
                points.Add(origin + direction * remainingRange);
                data.pathPoints = points.ToArray();
                data.specialPoint = null;
                data.hitTarget = null;
                data.finalDirection = direction;
                // timeout 由 ShootAction 统一设置为固定值，这里只设置 damage
                data.damage = damage;
                return data;
            }
        }

        // 3. 达到最大反射次数，但没有命中敌人，子弹在最后一个反射点消失
        data.pathPoints = points.ToArray();
        data.specialPoint = null;
        data.hitTarget = null;
        data.finalDirection = Vector3.zero;
        data.damage = damage;
        // 没有 finalDirection，子弹会在最后一个点直接销毁
        return data;
    }
}
