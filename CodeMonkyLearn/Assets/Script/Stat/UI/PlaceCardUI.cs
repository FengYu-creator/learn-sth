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
        PlaceButton.gameObject.SetActive(false);
        CancelPlaceButton.gameObject.SetActive(true);
        placedUnit = StatManager.Instance.PlaceUnit(unitID,new Vector3(1,0,1));
        StatManager.Instance.onClickPlace();
    }
    public void onClickCancelPlaceButton()
    {
        PlaceButton.gameObject.SetActive(true);
        CancelPlaceButton.gameObject.SetActive(false);
        //从格子列表移除
        if (placedUnit != null) 
        {
            Destroy(placedUnit);
        }
    }
}
