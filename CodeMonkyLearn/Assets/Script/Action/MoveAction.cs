using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    [SerializeField] private Animator unitAnimator;
    [SerializeField] private int maxMoveDistance=4;
    Vector3 targetPosition;
    Vector3 direction;
    private int speed = 5;
    private float stoppingdistance = 0.1f;

    protected override void Awake()
    {
        base.Awake();
        targetPosition = transform.position;
    }


    void Update()
    {
        if (!isActive)
        {
            return;
        }

        direction = (targetPosition - transform.position).normalized;

        if (Vector3.Distance(transform.position, targetPosition) > stoppingdistance)
        {

            transform.position += direction * speed * Time.deltaTime;
            unitAnimator.SetBool("IsWalking", true);

        }
        else
        {
            unitAnimator.SetBool("IsWalking", false);
            isActive = false;
            OnActionComplete();
        }

        float rotateSpeed = 15f;
        transform.forward = Vector3.Lerp(transform.forward, direction, rotateSpeed * Time.deltaTime);

    }

    public override void TakeAction(GridPosition gridPosition,Action OnActionComplete)
    {
        this.OnActionComplete = OnActionComplete;
        this.targetPosition = LevelGrid.Instance.GetWorldPosition(gridPosition);
        isActive = true;
    }


    public override List<GridPosition>GetValidGridPosition()

    {
        List<GridPosition>ValidGridPosition = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();
        for (int x = -maxMoveDistance; x <= maxMoveDistance; x++) 
            for (int y = -maxMoveDistance; y <= maxMoveDistance; y++)
            {
                GridPosition offsetPosition = new GridPosition(x,y);
                GridPosition testGridPosition = unitGridPosition + offsetPosition;
                if(!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }
                if(unitGridPosition==testGridPosition)
                {
                    continue;
                }
                if(LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
                {
                    continue;
                }

                ValidGridPosition.Add(testGridPosition);
            }
        return ValidGridPosition;
    }

    public override string GetActionName()
    {
        return "Move";
    }


}
