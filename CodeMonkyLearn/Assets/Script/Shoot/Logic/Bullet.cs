using UnityEngine;
using System;

/// <summary>
/// 子弹视觉体，负责沿预定路径飞行，抵达终点后触发伤害事件
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float speed = 50f;       // 飞行速度（米/秒）

    [Header("特效")]
    [SerializeField] private GameObject hitEffectPrefab;  // 命中特效预制体

    private BulletPathData pathData;
    private int currentPointIndex = 1;  // 从 1 开始，跳过发射点
    private float lifetime;

    private void Update()
    {
        // 生命时间递减
        if (lifetime > 0f)
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 初始化子弹数据并开始飞行
    /// </summary>
    public void Init(BulletPathData data)
    {
        pathData = data;
        currentPointIndex = 1;
        lifetime = data.timeout > 0f ? data.timeout : 5f;  // 默认 5 秒超时

        // 设置初始位置
        if (pathData.pathPoints != null && pathData.pathPoints.Length > 0)
        {
            transform.position = pathData.pathPoints[0];
        }

        // 开始飞行协程
        StartCoroutine(FlyRoutine());
    }

    private System.Collections.IEnumerator FlyRoutine()
    {
        Vector3[] intermediatePoints = pathData.GetIntermediatePoints();

        // 1. 依次飞向所有中间碰撞点
        for (int i = 0; i < intermediatePoints.Length; i++)
        {
            yield return MoveToPoint(intermediatePoints[i]);
            currentPointIndex++;

            // 检查寿命
            if (lifetime <= 0f)
            {
                DestroySelf();
                yield break;
            }

            // 检查是否需要中途销毁（折射次数用完，没有后续路径）
            if (!pathData.HasFinalDirection && !pathData.HasSpecialPoint)
            {
                // 折射次数用完，子弹到达最后一个点后直接销毁
                DestroySelf();
                yield break;
            }
        }

        // 2. 所有中间点飞完了，检查后续行为
        if (pathData.HasSpecialPoint && pathData.specialPoint.HasValue)
        {
            // 有命中目标：飞向特殊点
            yield return MoveToPoint(pathData.specialPoint.Value);
            OnHitTarget();
            yield break;
        }

        if (pathData.HasFinalDirection)
        {
            // 无命中目标：沿最终方向飞出
            yield return FlyFinalSegment();
            yield break;
        }

        // 3. 容错：都没有，直接销毁
        DestroySelf();
    }

    /// <summary>
    /// 移动到目标点
    /// </summary>
    private System.Collections.IEnumerator MoveToPoint(Vector3 target)
    {
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);
        float duration = distance / speed;

        // 看向目标方向
        Vector3 direction = (target - start).normalized;
        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }

        if (duration <= 0f)
        {
            transform.position = target;
            yield break;
        }

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            transform.position = Vector3.Lerp(start, target, progress);
            yield return null;
        }

        transform.position = target;
    }

    /// <summary>
    /// 最后一段：沿方向飞出直到寿命耗尽
    /// </summary>
    private System.Collections.IEnumerator FlyFinalSegment()
    {
        Vector3 direction = pathData.finalDirection.normalized;

        while (lifetime > 0f)
        {
            transform.position += direction * speed * Time.deltaTime;
            lifetime -= Time.deltaTime;
            yield return null;
        }

        DestroySelf();
    }

    /// <summary>
    /// 命中目标，触发伤害事件
    /// </summary>
    private void OnHitTarget()
    {
        // 播放命中特效
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 触发命中事件
        if (pathData.hitTarget != null)
        {
            Events.CallBulletHit(pathData.hitTarget, pathData.damage);
        }

        DestroySelf();
    }

    /// <summary>
    /// 销毁子弹
    /// </summary>
    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
