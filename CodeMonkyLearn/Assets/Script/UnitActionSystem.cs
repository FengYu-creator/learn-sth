using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitActionSystem : MonoBehaviour
{
    [SerializeField] private Unit selectedUnit;
    [SerializeField] private LayerMask unitPlaneLayerMask;
    public static UnitActionSystem Instance { get; private set; }

    public event EventHandler OnSelectedUnitChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {        
            if(TryHandleUnitSelection()) return;
            selectedUnit.Move(MouseWorld.GetPosition());
            //Unit脚本初始化时获取组件位置-获取网格位置-获取网格对象-把自己传给网格对象；选中单位时获取位置-网格对象； 清除位置-网格对象-单位；赋值鼠标点击位置；位置-网格位置-网格对象，赋予选中单位；
            //想麻烦了，单位脚本挂载在不同个体上，直接用位置传就行了
        }
    }
    private bool TryHandleUnitSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, unitPlaneLayerMask))//if可检测是否命中物体
        {
            if (hitInfo.transform.TryGetComponent<Unit>(out Unit unit))//如果能得到Unit组件就会赋值
            {
                SelectedUnit(unit);
                return true;
            }
        }
        return false;
    }
    private void SelectedUnit(Unit unit)
    {
        selectedUnit = unit;
        OnSelectedUnitChanged?.Invoke(this,EventArgs.Empty);//this将当前实例传递给事件的订阅者,订阅者可借此访问触发事件的实例的其他属性（selectedUnit)和方法
    }
    public Unit GetSelectedUnit()
    {
        return selectedUnit; 
    }
 
}
