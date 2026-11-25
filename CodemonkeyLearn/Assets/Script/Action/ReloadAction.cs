using System;
using System.Collections.Generic;
using UnityEngine;

public class ReloadAction : BaseAction
{
    [SerializeField] private Animator unitAnimator;
    private float reloadTimer;
    private const float RELOAD_DURATION = 1.5f; // 换弹动画时长（秒）
    private ShootAction shootAction;

    protected override void Awake()
    {
        base.Awake();
        shootAction = GetComponent<ShootAction>();
    }

    void Update()
    {
        if (!isActive) return;

        reloadTimer -= Time.deltaTime;
        if (reloadTimer <= 0f)
        {
            isActive = false;

            // 停止换弹动画
            if (unitAnimator != null)
            {
                unitAnimator.SetBool("IsReloading", false);
            }

            // 补满弹药
            if (shootAction != null)
            {
                shootAction.Reload();
            }

            OnActionComplete?.Invoke();
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action OnActionComplete)
    {
        this.OnActionComplete = OnActionComplete;
        isActive = true;
        reloadTimer = RELOAD_DURATION;

        // 触发换弹音效
        Events.CallReload();

        // 触发换弹动画
        if (unitAnimator != null)
        {
            unitAnimator.SetBool("IsReloading", true);
        }
    }

    public override string GetActionName()
    {
        return "换弹";
    }

    public override List<GridPosition> GetValidGridPosition()
    {
        GridPosition gridPosition = unit.GetGridPosition();
        return new List<GridPosition> { gridPosition };
    }

    public override int GetActionPointsCost()
    {
        return 1;
    }
}
