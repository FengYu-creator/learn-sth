using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }
    private int turnNumber =1;
    public event EventHandler OnTurnChanged;
    private bool isPlayerTurn=true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Events.BattleEnded += OnBattleEnded;
    }

    private void OnDestroy()
    {
        Events.BattleEnded -= OnBattleEnded;
    }

    private void OnBattleEnded()
    {
        ResetToPlayerTurn();
    }


    public void NextTurn()
    {
        turnNumber++;
        isPlayerTurn = !isPlayerTurn;

        OnTurnChanged?.Invoke(this, EventArgs.Empty);

        // 切换到玩家回合时，检测场上是否还有玩家单位
        if (isPlayerTurn && !HasPlayerUnitAlive())
        {
            // 全灭结算，资源减半
            var lootResultUI = FindObjectOfType<LootResultUI>();
            if (lootResultUI != null)
                lootResultUI.ShowResult(halfResources: true);
        }
    }

    /// <summary>检测场上是否有存活的玩家单位</summary>
    private bool HasPlayerUnitAlive()
    {
        foreach (var unit in FindObjectsOfType<Unit>())
        {
            if (!unit.IsEnemy())
            {
                var hs = unit.GetComponent<HealthSystem>();
                if (hs != null && hs.IsAlive())
                    return true;
            }
        }
        return false;
    }

    /// <summary>重置回合系统（切换关卡时调用）</summary>
    public void ResetToPlayerTurn()
    {
        turnNumber = 1;
        isPlayerTurn = true;
        // 不发事件，避免触发敌人 AI
    }

    public int GetTurnNumber()
    {
        return turnNumber;
    }
    
    public bool IsPlayerTurn()
    {
        return isPlayerTurn;    
    }


}
