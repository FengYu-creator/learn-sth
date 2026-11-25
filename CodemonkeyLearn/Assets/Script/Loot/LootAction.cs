using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 搜刮动作 — 靠近搜刮点 → 旋转朝向 → IsFinding → 搜刮
/// </summary>
public class LootAction : BaseAction
{
    [SerializeField] private float searchDuration = 5f;       // 搜刮持续秒数
    [SerializeField] private float rotateSpeed = 10f;          // 旋转速度
    [SerializeField] private Animator unitAnimator;           // 动画控制器

    private LootablePoint targetPoint;
    private LootController lootController;

    protected override void Awake()
    {
        base.Awake();
        lootController = GetComponent<LootController>();
        if (lootController == null)
            lootController = gameObject.AddComponent<LootController>();
        if (unitAnimator == null)
            unitAnimator = GetComponent<Animator>();
    }

    public override string GetActionName() => "搜刮";

    public override void TakeAction(GridPosition gridPosition, Action OnActionComplete)
    {
        this.OnActionComplete = OnActionComplete;
        isActive = true;
        StartCoroutine(PerformLoot());
    }

    private IEnumerator PerformLoot()
    {
        // 1. 找到最近的搜刮点
        targetPoint = FindNearestLootablePoint();
        if (targetPoint == null)
        {
            isActive = false;
            OnActionComplete?.Invoke();
            yield break;
        }

        // 2. 旋转朝向搜刮点
        yield return StartCoroutine(RotateToTarget(targetPoint.transform.position));

        // 3. 进入搜刮状态
        lootController.StartSearching(targetPoint.ItemDatabase);

        // 播放搜刮动画
        if (unitAnimator != null)
            unitAnimator.SetBool("IsFinding", true);

        // 4. 持续搜刮
        float elapsed = 0f;
        while (elapsed < searchDuration)
        {
            elapsed += Time.deltaTime;
            Events.CallLootSearch();
            yield return null;
        }

        // 5. 结束搜刮，标记搜刮点已使用
        if (unitAnimator != null)
            unitAnimator.SetBool("IsFinding", false);
        Events.CallLootSearchStop();
        lootController.StopSearching();
        targetPoint.MarkAsLooted();
        isActive = false;
        OnActionComplete?.Invoke();
    }

    private IEnumerator RotateToTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0f;

        if (dir == Vector3.zero) yield break;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    private LootablePoint FindNearestLootablePoint()
    {
        LootablePoint[] points = FindObjectsByType<LootablePoint>(FindObjectsSortMode.None);
        LootablePoint nearest = null;
        float minDist = float.MaxValue;

        foreach (var p in points)
        {
            if (!p.PlayerInRange) continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = p;
            }
        }

        return nearest;
    }

    public override List<GridPosition> GetValidGridPosition()
    {
        // 搜刮不需要特定网格位置，只需附近有搜刮点
        return new List<GridPosition> { unit.GetGridPosition() };
    }

    public override bool IsValidGridPosition(GridPosition gridPosition)
    {
        return FindNearestLootablePoint() != null;
    }
}
