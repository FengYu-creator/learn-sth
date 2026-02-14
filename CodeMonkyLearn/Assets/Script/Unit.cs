using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    Vector3 targetPosition;
    Vector3 direction;
    private int speed=5;
    private float stoppingdistance = 0.1f;
    private GridPosition gridPosition;

    private void Awake()
    {
        targetPosition= transform.position;
    }
    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
    }
    void Update()
    {

        direction = (targetPosition-transform.position).normalized;
        if(Vector3.Distance(transform.position,targetPosition)>stoppingdistance)
        {
            transform .position +=direction *speed*Time.deltaTime;
        }

        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            LevelGrid.Instance.UnitMovedGridPosition(this,gridPosition,newGridPosition);
            gridPosition = newGridPosition;
        }

    }
    public void Move(Vector3 targetPosition)
    {
        this.targetPosition =targetPosition; 
    }
        
}
