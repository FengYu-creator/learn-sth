using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinAction : BaseAction
{
    float rotationAngle;

    // Update is called once per frame
    void Update()
    {
        if (!isActive)
        {
            return;
        }

        float rotate = 360f * Time.deltaTime;
        rotationAngle += rotate;
        transform.eulerAngles += new Vector3(0, rotate, 0);

        if (rotationAngle >= 360f)
        {
            isActive = false;
            rotationAngle = 0;
            OnActionComplete();

        }
    }

    public override void TakeAction(GridPosition gridPosition, Action OnActionComplete)
    {
        this.OnActionComplete = OnActionComplete;
        isActive = true;
    }

    public override string GetActionName()
    {
        return "Spin";
    }
    public override List<GridPosition> GetValidGridPosition()
    {
        //基类获取的unit
        GridPosition gridPosition = unit.GetGridPosition();
        return new List<GridPosition>
        {
            gridPosition
        };
    }

    public override int GetActionPointsCost()
    {
        return 2;
    }

}
