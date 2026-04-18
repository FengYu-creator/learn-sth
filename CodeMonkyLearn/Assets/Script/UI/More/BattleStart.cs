using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;


public class BattleStart : MonoBehaviour
{
    [SerializeField] private Button battleStartButton;
    // Start is called before the first frame update
    void Start()
    {
        ClickBattleStart();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void ClickBattleStart()
    {
        battleStartButton.onClick.AddListener(() => { UnitActionSystem.Instance.OnBattleStart(); Destroy(battleStartButton.gameObject);Events.CallBattleStarted(); });
    }
    //todo：进入关卡时生成开始战斗按钮

}
