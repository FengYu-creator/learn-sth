using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : MonoBehaviour
{
    [SerializeField] private Transform gridSystemVisualSingelPrefab;
    private GridSystemVisualSingle[,] gridSystemVisualArray;

    public static GridSystemVisual Instance { get; private set; }
    private MoveAction moveAction;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {

        gridSystemVisualArray = new GridSystemVisualSingle[LevelGrid.Instance.GetWidth(), LevelGrid.Instance.GetHeight()];
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                GridPosition gridPosition = new GridPosition(x,z);
                Transform gridSystemVisualSingelTransform =
                  Instantiate(gridSystemVisualSingelPrefab, LevelGrid.Instance.GetWorldPosition(gridPosition), Quaternion.identity);

                gridSystemVisualArray[x, z] = gridSystemVisualSingelTransform.GetComponent<GridSystemVisualSingle>();
            }
        }
    }
    private void Update()
    {
        UpdateGridVisual();
    }

    public void HideAllGridPosition()
    {
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                gridSystemVisualArray[x, z].Hide();
            }
        }
    }
    public void ShowGridPositionList(List<GridPosition>gridPositionList)
    {
        foreach( GridPosition gridPosition in gridPositionList)
        {
            gridSystemVisualArray[gridPosition.x,gridPosition.z].Show();
        }
    }
    private void UpdateGridVisual()
    {
        HideAllGridPosition();
        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
        ShowGridPositionList(selectedAction.GetValidGridPosition());
    }
}
