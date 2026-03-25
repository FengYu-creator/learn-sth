using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    protected Unit unit;
    protected bool isActive;
    protected Action OnActionComplete;

    protected virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }


    public abstract string GetActionName();

    public abstract void TakeAction(GridPosition gridPosition,Action OnActionComplete);

    public virtual bool IsValidGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> ValidGridPosition = GetValidGridPosition();
        return ValidGridPosition.Contains(gridPosition);
    }
    public abstract List<GridPosition> GetValidGridPosition();

    public virtual int GetActionPointsCost()
    {
        return 1;
    }


}
