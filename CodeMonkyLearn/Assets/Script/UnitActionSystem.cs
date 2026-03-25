using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitActionSystem : MonoBehaviour
{
    [SerializeField] private Unit selectedUnit;
    [SerializeField] private LayerMask unitPlaneLayerMask;
    public static UnitActionSystem Instance { get; private set; }
    
    public event EventHandler OnSelectedUnitChanged;
    public event EventHandler OnSelectedActionChanged;
    public event EventHandler<bool> OnBusyChanged;
    public event EventHandler OnActionStarted;


    private bool isBusy;
    private BaseAction selectedAction;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetSelectedUnit(selectedUnit);
    }

    private void Update()
    {
        if (isBusy)
        {
            return; 
        }
        if(!TurnSystem .Instance.IsPlayerTurn())
        {
            return;
        }
        if(EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (TryHandleUnitSelection())
        {
            return;
        }

        HandleSelectedAction();


            //Unit脚本初始化时获取组件位置-获取网格位置-获取网格对象-把自己传给网格对象；选中单位时获取位置-网格对象； 清除位置-网格对象-单位；赋值鼠标点击位置；位置-网格位置-网格对象，赋予选中单位；
            //想麻烦了，单位脚本挂载在不同个体上，直接用位置传就行了

        
    }


    private void HandleSelectedAction( )
    {
                
        if (Input.GetMouseButtonDown(0))
        {

                GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());

            if (selectedAction.IsValidGridPosition(mouseGridPosition))
            {
                if (selectedUnit.TrySpendActionPointToTakeAction(selectedAction))
                {
                    SetBusy();
                    selectedAction.TakeAction(mouseGridPosition, ClearBusy);
                    OnActionStarted?.Invoke(this, EventArgs.Empty);
                }
            }

        }
    }



    private bool TryHandleUnitSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, unitPlaneLayerMask))//if可检测是否命中物体
            {
                if (hitInfo.transform.TryGetComponent<Unit>(out Unit unit))//如果能得到Unit组件就会赋值
                {
                    if(unit ==selectedUnit)
                    { 
                        return false; 
                    }
                    if (unit.IsEnemy())
                    {
                        return false;
                    }
                    SetSelectedUnit(unit);
                    return true;
                }
            }
        }
        return false;
    }
    private void SetSelectedUnit(Unit unit)
    {
        selectedUnit = unit;

        OnSelectedUnitChanged?.Invoke(this,EventArgs.Empty);//this将当前实例传递给事件的订阅者,订阅者可借此访问触发事件的实例的其他属性（selectedUnit)和方法
        
        SetSelectedAction(selectedUnit.GetMoveAction());
    }
    public Unit GetSelectedUnit()
    {
        return selectedUnit; 
    }
    public void SetSelectedAction(BaseAction baseAction)
    {
        selectedAction = baseAction;

        OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
    }
    public BaseAction GetSelectedAction()
    {
        return selectedAction;
    }


    private void SetBusy()
    {
        isBusy = true;
        OnBusyChanged?.Invoke(this,isBusy);
    }
    private void ClearBusy()
    {
        isBusy = false;
        OnBusyChanged?.Invoke(this, isBusy);
    }
    

 
}
