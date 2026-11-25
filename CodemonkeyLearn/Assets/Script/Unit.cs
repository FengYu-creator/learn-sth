using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private bool isEnemy;
    public static event EventHandler OnAnyActionPointsChanged;
    public event EventHandler OnStaminaChanged;

    /// <summary>设置是否为敌方单位（供代码生成敌人时使用）</summary>
    public void SetEnemy(bool value) { isEnemy = value; }

    private GridPosition gridPosition;
    private UnitStat unitStat;
    private MoveAction moveAction;
    private SpinAction spinAction;
    private BaseAction[] baseActionArray;
    private HealthSystem healthSystem;
    private int actionPoint = 2;
    private const int ACTION_POINTS_MAX = 2;

    // === 精力值系统 ===
    private int maxStamina;
    private int currentStamina;
    private bool staminaInitialized = false;
    private float staminaDecayAccumulator = 0f;
    private const float STAMINA_DECAY_PER_SEC = 2f;

    public int MaxStamina => maxStamina;
    public int CurrentStamina => currentStamina;
    public float GetStaminaPercent() => maxStamina > 0 ? (float)currentStamina / maxStamina : 0f;




    private void Awake()
    {
        moveAction = GetComponent<MoveAction>();
        spinAction = GetComponent<SpinAction>();
        baseActionArray = GetComponents<BaseAction>();
        unitStat = GetComponent<UnitStat>();
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        Events.BattleStarted += OnBattleStarted;
    }
    void Update()
    {
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            LevelGrid.Instance.UnitMovedGridPosition(this,gridPosition,newGridPosition);
            gridPosition = newGridPosition;
        }

        // 精力值自然衰减：每秒 -2
        if (staminaInitialized && currentStamina > 0)
        {
            staminaDecayAccumulator += STAMINA_DECAY_PER_SEC * Time.deltaTime;
            int decay = Mathf.FloorToInt(staminaDecayAccumulator);
            if (decay > 0)
            {
                staminaDecayAccumulator -= decay;
                currentStamina = Mathf.Max(0, currentStamina - decay);
                OnStaminaChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public MoveAction GetMoveAction()    //获取Unit的MoveAction组件
    {
        return moveAction;
    }
    public UnitStat GetUnitStat()
    {
        return unitStat;
    }
    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }
        
    public SpinAction GetSpinAction()
    {
        return spinAction;
    }

    private void OnBattleStarted()
    {
        // 延迟一帧初始化精力值（等 BattleStats 计算完毕）
        StartCoroutine(InitStaminaNextFrame());
    }

    private System.Collections.IEnumerator InitStaminaNextFrame()
    {
        yield return null;
        if (unitStat != null)
        {
            InitStamina(unitStat.battleStats.Willpower);
        }
    }

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.OnTurnChanged -= TurnSystem_OnTurnChanged;
        Events.BattleStarted -= OnBattleStarted;
    }

    public BaseAction[] GetBaseActionArray()
    {
        return baseActionArray;
    }
    public bool TrySpendActionPointToTakeAction(BaseAction baseAction)
    {
        if (CanSpendActionPointsToTakeAction(baseAction))
        {
            SpendActionPoint(baseAction.GetActionPointsCost());
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool CanSpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if (actionPoint >= baseAction.GetActionPointsCost())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private void SpendActionPoint(int amount)
    {
        actionPoint -= amount;
        OnAnyActionPointsChanged?.Invoke(this,EventArgs.Empty);
    }

    public int GetActionPoint()
    {
        return actionPoint;
    }

    /// <summary>消耗指定数量行动点（供 ShootAction 等非 BaseAction 组件使用）</summary>
    public bool TrySpendActionPoint(int amount)
    {
        if (actionPoint >= amount)
        {
            SpendActionPoint(amount);
            return true;
        }
        return false;
    }
    public bool IsEnemy()
    {
        return isEnemy;
    }

    private void TurnSystem_OnTurnChanged(object sender,EventArgs e)
    {
        bool isPlayerTurn = TurnSystem.Instance.IsPlayerTurn();
        // 敌人只在敌方回合重置，玩家只在己方回合重置
        bool shouldReset = (IsEnemy() && !isPlayerTurn) || (!IsEnemy() && isPlayerTurn);
        if (shouldReset)
        {
            actionPoint = ACTION_POINTS_MAX;
            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);

            // 精力值回合回复：50 + OBWillPower * 10
            RecoverStaminaOnTurn();
        }
    }

    // === 精力值系统方法 ===

    /// <summary>
    /// 初始化精力值：maxStamina = 100 + willPower * 10，current = max
    /// </summary>
    public void InitStamina(int willPower)
    {
        maxStamina = 100 + willPower * 10;
        currentStamina = maxStamina;
        staminaInitialized = true;
        OnStaminaChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>增加最大精力值（同时回复等量）</summary>
    public void AddMaxStamina(int amount)
    {
        if (!staminaInitialized) return;
        maxStamina += amount;
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        OnStaminaChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 每回合回复精力：50 + OBWillPower * 10
    /// </summary>
    private void RecoverStaminaOnTurn()
    {
        if (!staminaInitialized || unitStat == null) return;

        int obWillPower = unitStat.battleStats.OBWillpower;
        int recoverAmount = 50 + obWillPower * 10;
        currentStamina = Mathf.Min(maxStamina, currentStamina + recoverAmount);
        OnStaminaChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 消耗精力（射击、屏息等）
    /// </summary>
    /// <returns>是否还有剩余精力</returns>
    public bool ConsumeStamina(int amount)
    {
        if (!staminaInitialized) return true;
        currentStamina = Mathf.Max(0, currentStamina - amount);
        OnStaminaChanged?.Invoke(this, EventArgs.Empty);
        return currentStamina > 0;
    }

    /// <summary>
    /// 是否还有精力
    /// </summary>
    public bool HasStamina()
    {
        return currentStamina > 0;
    }

    public bool IsStaminaInitialized() => staminaInitialized;



}
