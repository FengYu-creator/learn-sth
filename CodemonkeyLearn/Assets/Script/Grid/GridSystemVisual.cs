using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : MonoBehaviour
{
    [SerializeField] private Transform gridSystemVisualSingelPrefab;
    private GridSystemVisualSingle[,] gridSystemVisualArray;

    public static GridSystemVisual Instance { get; private set; }

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
        RebuildVisual();
    }

    /// <summary>重建所有可视化格子（切换关卡时调用）</summary>
    public void RebuildVisual()
    {
        // 销毁旧格子
        if (gridSystemVisualArray != null)
        {
            foreach (var visual in gridSystemVisualArray)
            {
                if (visual != null)
                    Destroy(visual.gameObject);
            }
        }

        gridSystemVisualArray = new GridSystemVisualSingle[LevelGrid.Instance.GetWidth(), LevelGrid.Instance.GetHeight()];
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
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

        // 动作执行期间不显示格子
        if (UnitActionSystem.Instance != null && UnitActionSystem.Instance.IsBusy)
            return;

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
        if (selectedAction == null) return;
        ShowGridPositionList(selectedAction.GetValidGridPosition());
    }
}
