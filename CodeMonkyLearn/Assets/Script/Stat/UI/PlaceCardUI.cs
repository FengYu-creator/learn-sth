using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaceCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI unitLevel;
    [SerializeField] private TextMeshProUGUI unitState;
    [SerializeField] private Button PlaceButton;
    [SerializeField] private Button CancelPlaceButton;
    private GameObject placedUnit;

    public int unitID;

    private Vector3 PlaceWorldGridPosition;

    void Start()
    {
        PlaceButton.onClick.AddListener(onClickPlaceButton);
        CancelPlaceButton.onClick.AddListener(onClickCancelPlaceButton);
        CancelPlaceButton.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void settingPlaceCard(int id,int level,string state)
    {
        unitID = id;
        unitName.text = StatManager.Instance.GetUnitData(id).name;
        unitLevel.text = level.ToString();
        unitState.text = state;
    }
    public void onClickPlaceButton()
    {
        PlaceWorldGridPosition = StatManager.Instance.GetPlacePosition();
        GridPosition gridPos = LevelGrid.Instance.GetGridPosition(PlaceWorldGridPosition);
        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPos))
        {
            return;  // 有单位就直接返回，不执行部署
        }
        PlaceButton.gameObject.SetActive(false);
        CancelPlaceButton.gameObject.SetActive(true);
        placedUnit = StatManager.Instance.PlaceUnit(unitID, PlaceWorldGridPosition);
        StatManager.Instance.onClickPlace();
    }
    public void onClickCancelPlaceButton()
    {
        PlaceButton.gameObject.SetActive(true);
        CancelPlaceButton.gameObject.SetActive(false);
        //从格子列表移除

            Unit unit = placedUnit.GetComponent<Unit>();
            if (UnitActionSystem.Instance.GetSelectedUnit() == unit)
            {
                UnitActionSystem.Instance.SetSelectedUnit(null);
            }
            GridPosition gridPos = LevelGrid.Instance.GetGridPosition(placedUnit.transform.position);
            LevelGrid.Instance.RemoveGridUnit(gridPos, unit);
            Destroy(placedUnit);
        
    }
}
