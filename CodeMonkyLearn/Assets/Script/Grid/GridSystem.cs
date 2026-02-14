using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridSystem
{

    private int width;
    private int heigth;
    private float cellSize;//单元格尺寸
    private GridObject[,] gridObjectArray;

    public GridSystem(int width,int heigth,float cellsize)//创建网格边界
    {
        this.width = width;
        this.heigth = heigth;
        this.cellSize = cellsize;

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
        return new Vector3(gridPosition.x,0,gridPosition.z)*cellSize;
    }

    public GridPosition GetGridPosition(Vector3 worldPositon)//返回GridPosition类型的网格位置，另有脚本存储定义GridPosition构造体
    {
        return new GridPosition(Mathf.RoundToInt(worldPositon.x / cellSize), Mathf.RoundToInt(worldPositon.z / cellSize));//调用构造函数实例化一个构造体返回
    }
   

    public void CreateDebugObjects(Transform debugPrefab)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < heigth; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                Transform DebugTransform = GameObject.Instantiate(debugPrefab, GetWorldPosition(gridPosition), Quaternion.identity);
                GridDebugObject gridDebugObject = DebugTransform.GetComponent<GridDebugObject>();
                gridDebugObject.SetGridObject(GetGridObject(gridPosition));//把网格对象传给网格调试预制体



            }
        }
    }
    public GridObject GetGridObject(GridPosition gridPosition)//从指定网格位置获取网格对象
    {
        return gridObjectArray[gridPosition.x, gridPosition.z];

    }
    
}
