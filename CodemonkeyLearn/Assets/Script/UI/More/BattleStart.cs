using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class BattleStart : MonoBehaviour
{
    [SerializeField] private Button battleStartButton;
    [SerializeField] private Button battleStartButtonPrefab;     // 按钮预制体（用于重新创建）
    [SerializeField] private LootResultUI lootResultUI;          // 结算面板引用

    private void Start()
    {
        if (battleStartButton != null)
            battleStartButton.onClick.AddListener(ClickBattleStart);
    }

    /// <summary>隐藏按钮，供 LootResultUI 显示结算时调用</summary>
    public void HideButton()
    {
        if (battleStartButton != null)
            battleStartButton.gameObject.SetActive(false);
    }

    /// <summary>显示按钮，供返回列车/切换关卡时调用</summary>
    public void ShowButton()
    {
        if (battleStartButton != null)
        {
            battleStartButton.gameObject.SetActive(true);
            battleStartButton.onClick.RemoveAllListeners();
            battleStartButton.onClick.AddListener(ClickBattleStart);
        }
    }

    private void ClickBattleStart()
    {
        UnitActionSystem.Instance.SetSelectedUnit(null);

        // 确保回合从玩家开始
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.ResetToPlayerTurn();

        UnitActionSystem.Instance.OnBattleStart();

        // 在 (6,6) 格子生成狙击手
        EnemyData sniper = EnemyDatabase.Instance.GetEnemyByName("Sniper");
        if (sniper != null)
        {
            Vector3 pos = LevelGrid.Instance.GetWorldPosition(new GridPosition(6, 6));
            StatManager.Instance.PlaceEnemy(sniper, pos);
        }
        else
        {
            Debug.LogWarning("[BattleStart] 未找到 Sniper 模板，请检查 EnemyDatabase");
        }

        // 隐藏按钮（不销毁，战斗结束会恢复）
        battleStartButton.gameObject.SetActive(false);
        Events.CallBattleStarted();
    }
}
