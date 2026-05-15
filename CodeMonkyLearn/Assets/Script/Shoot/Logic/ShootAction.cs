using UnityEngine;
using System;

public class ShootAction : MonoBehaviour
{
    [Header("激光发射点")]
    [SerializeField] private Transform firePoint;  // 激光/子弹的发射位置

    /// <summary>
    /// 激光/子弹发射点
    /// </summary>
    public Transform FirePoint => firePoint;

    /// <summary>
    /// 当前回合是否可以进入射击状态
    /// </summary>
    public bool CanEnterShoot { get; private set; } = true;

    /// <summary>
    /// 本回合是否已触发过射击
    /// </summary>
    private bool hasShotThisTurn = false;

    /// <summary>
    /// 射击后由 ShootUI 直接调用，锁定本回合
    /// </summary>
    public void LockShootThisTurn()
    {
        hasShotThisTurn = true;
        CanEnterShoot = false;
    }

    /// <summary>
    /// 回合变化时，若为玩家回合则重置
    /// </summary>
    private void OnTurnChanged(object sender, EventArgs e)
    {
        // 任何回合切换都清空选中单位
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.SetSelectedUnit(null);
        }

        // 只有玩家回合才重置射击状态
        if (TurnSystem.Instance != null && TurnSystem.Instance.IsPlayerTurn())
        {
            CanEnterShoot = true;
            hasShotThisTurn = false;
        }
    }

    private void OnEnable()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged += OnTurnChanged;
        }
    }

    private void OnDisable()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged -= OnTurnChanged;
        }
    }
}