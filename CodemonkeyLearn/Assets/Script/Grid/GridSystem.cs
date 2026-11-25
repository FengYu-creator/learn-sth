using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridSystem
{

    private int width;
    private int heigth;
    private float cellSize;//单元格尺寸
    private Vector3 originOffset;
    private GridObject[,] gridObjectArray;
    private List<Transform> debugObjects = new List<Transform>();

    public GridSystem(int width,int heigth,float cellsize,Vector3 origin = default)//创建网格边界
    {
        this.width = width;
        this.heigth = heigth;
        this.cellSize = cellsize;
        this.originOffset = origin;

        gridObjectArray = new GridObject[width,heigth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < heigth; z++)//得到每个网格坐标对应的世界坐标
            {
                GridPosition gridPosition = new GridPosition(x,z);
                gridObjectArray[x, z] = new GridObject(this, gridPosition); 
                
            }
        }

    } 

    public Vector3 GetWorldPosition(GridPosition gridPosition )//传入网格坐标，乘以系数得到世界坐标//重构：接收网格位置，而不是int x，int z
    {
        return originOffset + new Vector3(gridPosition.x,0,gridPosition.z)*cellSize;
    }

    public GridPosition GetGridPosition(Vector3 worldPositon)//返回GridPosition类型的网格位置，另有脚本存储定义GridPosition构造体
    {
        Vector3 localPos = worldPositon - originOffset;
        return new GridPosition(Mathf.RoundToInt(localPos.x / cellSize), Mathf.RoundToInt(localPos.z / cellSize));//调用构造函数实例化一个构造体返回
    }
   

    public void CreateDebugObjects(Transform debugPrefab)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < heigth; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                Transform DebugTransform = GameObject.Instantiate(debugPrefab, GetWorldPosition(gridPosition), Quaternion.identity);
                debugObjects.Add(DebugTransform);
                GridDebugObject gridDebugObject = DebugTransform.GetComponent<GridDebugObject>();
                gridDebugObject.SetGridObject(GetGridObject(gridPosition));//把网格对象传给网格调试预制体



            }
        }
    }

    public void DestroyDebugObjects()
    {
        foreach (Transform obj in debugObjects)
        {
            if (obj != null)
                GameObject.Destroy(obj.gameObject);
        }
        debugObjects.Clear();
    }
    public GridObject GetGridObject(GridPosition gridPosition)//从指定网格位置获取网格对象
    {
        if (!IsValidGridPosition(gridPosition))
            return null;
        return gridObjectArray[gridPosition.x, gridPosition.z];

    }
    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        return gridPosition.x >= 0 &&
            gridPosition.z >= 0 &&
            gridPosition.x < width &&
            gridPosition.z < heigth;
    }

    public int GetWidth()
    {
        return width;
    }
    public int GetHeight()
    {
        return heigth;
    }
}
