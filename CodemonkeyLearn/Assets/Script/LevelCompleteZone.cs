using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 撤离区 — 回合改变时检测是否所有玩家单位都在区域内
/// 全部在区域内 → 关卡完成 → 弹出结算
/// </summary>
public class LevelCompleteZone : MonoBehaviour
{
    [Header("结算 UI")]
    [SerializeField] private LootResultUI lootResultUI;

    private List<Unit> unitsInZone = new List<Unit>();

    private void Start()
    {
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.OnTurnChanged += OnTurnChanged;
        Events.BattleEnded += OnBattleEnded;
        Events.BattleStarted += OnBattleStarted;
    }

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.OnTurnChanged -= OnTurnChanged;
        Events.BattleEnded -= OnBattleEnded;
        Events.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
        unitsInZone.Clear();
    }

    private void OnBattleEnded()
    {
        unitsInZone.Clear();
    }

    private void OnTurnChanged(object sender, System.EventArgs e)
    {
        CheckComplete();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Unit>(out var unit) && !unit.IsEnemy())
        {
            if (!unitsInZone.Contains(unit))
            {
                unitsInZone.Add(unit);
                Debug.Log($"[LevelComplete] {unit.name} 进入撤离区，当前区内: {unitsInZone.Count}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Unit>(out var unit) && !unit.IsEnemy())
        {
            unitsInZone.Remove(unit);
            Debug.Log($"[LevelComplete] {unit.name} 离开撤离区，当前区内: {unitsInZone.Count}");
        }
    }

    private void CheckComplete()
    {
        // 获取所有存活玩家单位
        var allPlayerUnits = new List<Unit>();
        foreach (var u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (!u.IsEnemy())
            {
                var hs = u.GetComponent<HealthSystem>();
                if (hs != null && hs.IsAlive())
                    allPlayerUnits.Add(u);
            }
        }

        if (allPlayerUnits.Count == 0)
        {
            Debug.Log("[LevelComplete] 没有存活的玩家单位");
            return;
        }

        Debug.Log($"[LevelComplete] 总玩家单位: {allPlayerUnits.Count}，在撤离区内: {unitsInZone.Count}");

        // 检查是否全部在撤离区内
        foreach (var u in allPlayerUnits)
        {
            if (!unitsInZone.Contains(u))
            {
                Debug.Log($"[LevelComplete] {u.name} 不在撤离区内，未完成撤离");
                return; // 有单位不在区内
            }
        }

        // 全部在撤离区内，关卡完成！
        Debug.Log("[LevelComplete] 所有玩家单位已撤离，关卡完成！");

        // 如果当前是敌人回合，强制结束敌人行动并切回玩家
        if (TurnSystem.Instance != null && !TurnSystem.Instance.IsPlayerTurn())
        {
            EnemyAI.ResetAll();
            TurnSystem.Instance.ResetToPlayerTurn();
        }

        if (lootResultUI != null)
            lootResultUI.ShowResult();
    }
}
