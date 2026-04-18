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
        StatManager.Instance.OnClickToPlacePosition += StatManager_OnClickToPlacePosition;
        StatManager.Instance.OnClickPlace += StatManager_OnClickPlace;
        Events.BattleStarted += PalaceUnitUI_BattleStarted;
        X.onClick.AddListener(() => { placeScroll.gameObject.SetActive(false); isPlaceCardOpen = false; });
        placeScroll.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

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
    }
    public void StatManager_OnClickPlace(object sender,EventArgs e)
    {
        //placeCardContainer.gameObject.SetActive(false);
    }
    public void StatManager_OnClickToPlacePosition(object sender,EventArgs e)
    {
        if (!isPlaceCardOpen)
        {
            placeScroll.gameObject.SetActive(true);
            isPlaceCardOpen = true;
            InstantiatePlaceCard();
        }

    }
    private void  PalaceUnitUI_BattleStarted()
    {

        isPlaceCardOpen = false;
        haveInstantiated = false;
        foreach (Transform child in placeCardContainer)
        {
            Destroy(child.gameObject);
        }
        placeScroll.gameObject.SetActive(false);

    }
    private void OnDestroy()

    {

        Events.BattleStarted -= PalaceUnitUI_BattleStarted;

    }
}
