using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnSystemUI : MonoBehaviour
{
    [SerializeField] private Button endTurnBtn;
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private GameObject enemyTurnVisualGameObject;
    void Start()
    {
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        Events.BattleStarted += TurnSystem_OnBattleStarted;
        ClickNextTurn();
        UpdateTurnText();
        UpdateEnemyTurnVisual();
        UpdateEndTurnButton();
        endTurnBtn.gameObject.SetActive(false);
    }

    void Update()
    {
    }
    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateEnemyTurnVisual();
        UpdateTurnText();
        UpdateEndTurnButton();
    }
    private void TurnSystem_OnBattleStarted()
    {
        endTurnBtn.gameObject.SetActive(true);

    }

    private void ClickNextTurn()
    {
        endTurnBtn.onClick.AddListener(() => { TurnSystem.Instance.NextTurn(); });
    }
    private void UpdateTurnText()
    {
        turnNumberText.text = "TURN " + TurnSystem.Instance.GetTurnNumber();
    }
    private void UpdateEnemyTurnVisual()
    {
        enemyTurnVisualGameObject.SetActive(!TurnSystem.Instance.IsPlayerTurn());
    }
    private void UpdateEndTurnButton()
    {
        endTurnBtn.gameObject.SetActive(TurnSystem.Instance.IsPlayerTurn());
    }

}
