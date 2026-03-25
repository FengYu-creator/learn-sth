using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public static LevelGrid Instance{get; private set;}
    [SerializeField] Transform gridDebugObjectPrefab;
    private GridSystem gridSystem;
    private void Awake()
    {

        if(Instance!=null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;


        gridSystem = new GridSystem(10, 10, 2f);
        gridSystem.CreateDebugObjects(gridDebugObjectPrefab);

    }
    public void AddUnitAtGridPosition(GridPosition gridPosition,Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject .AddUnit(unit);
    }
    public List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject .GetUnitList();
    }
    public void RemoveGridUnit(GridPosition gridPosition,Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject.RemoveUnit(unit );
    }

    public void UnitMovedGridPosition(Unit unit , GridPosition fromGridPosition , GridPosition toGridPosition)
    {
        RemoveGridUnit(fromGridPosition, unit);
        AddUnitAtGridPosition(toGridPosition, unit);

    }

    //透传函数，通过此脚本的方法调用gridSystem的方法
    public GridPosition GetGridPosition(Vector3 worldPosition) => gridSystem.GetGridPosition(worldPosition);
    public Vector3 GetWorldPosition(GridPosition gridPosition) => gridSystem.GetWorldPosition(gridPosition);
    public bool IsValidGridPosition(GridPosition gridPosition) => gridSystem.IsValidGridPosition(gridPosition);
    public int GetWidth()=>gridSystem.GetWidth();
    public int GetHeight()=>gridSystem.GetHeight();

    public bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.HasAnyUnit();
    }
}
