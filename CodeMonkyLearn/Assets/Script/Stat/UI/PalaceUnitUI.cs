using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PalaceUnitUI : MonoBehaviour
{
    // 读取字典数目，根据ID循环实例化place card
    [SerializeField] private Transform placeCardPrefab;
    [SerializeField] private Transform placeCardContainer;
    [SerializeField] private Transform placeScroll;
    [SerializeField] protected Button X;
    private int unitNumber;
    private bool isPlaceCardOpen= false;
    private bool haveInstantiated = false;
    private void Awake()
    {

    }
    void Start()
    {
        StatManager.Instance.OnClickPlace += StatManager_OnClickPlace;
        X.onClick.AddListener(() => { placeScroll.gameObject.SetActive(false); isPlaceCardOpen = false; });
        placeScroll.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.L)&&!isPlaceCardOpen) 
        {
            placeScroll.gameObject.SetActive(true);
            isPlaceCardOpen = true;
            InstantiatePlaceCard();
        }
    }

    public void InstantiatePlaceCard()
    {
        if (!haveInstantiated)
        {
            unitNumber = StatManager.Instance.GetUnitNumber();

            for (int i = 0; i < unitNumber; i++)
            {
                Transform placeCardTransform = Instantiate(placeCardPrefab, placeCardContainer);
                placeCardTransform.GetComponent<PlaceCardUI>().settingPlaceCard(i, 1, "full");
            }
            haveInstantiated = true;
        }
        else
        {
            return;
        }
        //todo.战斗开始事件——恢复haveInstantiated
    }
    public void StatManager_OnClickPlace(object sender,EventArgs e)
    {
        //placeCardContainer.gameObject.SetActive(false);
    }

}
