using System;
using UnityEngine;

public static class Events
{
    public static event Action BattleStarted;
    public static void CallBattleStarted()
    {
        BattleStarted?.Invoke();
    }

    public static event Action BattleEnded;
    public static void CallBattleEnded()
    {
        BattleEnded?.Invoke();
    }

    public static event Action OnAimStateEntered;
    public static void CallAimStateEntered()
    {
        OnAimStateEntered?.Invoke();
    }

    public static event Action OnAimStateExited;
    public static void CallAimStateExited()
    {
        OnAimStateExited?.Invoke();
    }

    public static event Action OnShoot;
    public static void CallShoot()
    {
        OnShoot?.Invoke();
    }

    /// <summary>
    /// 弹药量变化事件
    /// </summary>
    public static event Action OnAmmoChanged;
    public static void CallAmmoChanged()
    {
        OnAmmoChanged?.Invoke();
    }

    /// <summary>
    /// 激光瞄准目标的世界坐标（由 ShootUI 每帧写入，LaserBeam 每帧读取）
    /// </summary>
    public static Vector3 LaserTargetPosition;

    /// <summary>
    /// 当前是否处于瞄准状态
    /// </summary>
    public static bool IsInAimState { get; set; }

    /// <summary>
    /// 子弹命中目标事件
    /// </summary>
    public static event Action<Unit, Unit, int> OnBulletHit;
    public static void CallBulletHit(Unit attacker, Unit target, int damage)
    {
        OnBulletHit?.Invoke(attacker, target, damage);
    }

    /// <summary>
    /// 敌人生成事件（Unit 组件 + EnemyData SO）
    /// </summary>
    public static event System.Action<Unit, EnemyData> OnEnemySpawned;
    public static void CallEnemySpawned(Unit unit, EnemyData data)
    {
        OnEnemySpawned?.Invoke(unit, data);
    }

    /// <summary>单位死亡事件（killer）</summary>
    public static event System.Action<Unit> OnDeath;
    public static void CallDeath(Unit killer)
    {
        OnDeath?.Invoke(killer);
    }

    /// <summary>换弹事件</summary>
    public static event Action OnReload;
    public static void CallReload()
    {
        OnReload?.Invoke();
    }

    /// <summary>脚步声事件</summary>
    public static event Action OnFootstep;
    public static void CallFootstep()
    {
        OnFootstep?.Invoke();
    }

    /// <summary>停止脚步声事件（动作结束立刻静音）</summary>
    public static event Action OnFootstepStop;
    public static void CallFootstepStop()
    {
        OnFootstepStop?.Invoke();
    }

    /// <summary>搜刮音效事件</summary>
    public static event Action OnLootSearch;
    public static void CallLootSearch()
    {
        OnLootSearch?.Invoke();
    }

    /// <summary>停止搜刮音效事件（动作结束立刻静音）</summary>
    public static event Action OnLootSearchStop;
    public static void CallLootSearchStop()
    {
        OnLootSearchStop?.Invoke();
    }

    /// <summary>UI 点击音效事件</summary>
    public static event Action OnUIClick;
    public static void CallUIClick()
    {
        OnUIClick?.Invoke();
    }

    /// <summary>卡片弹出音效事件</summary>
    public static event Action OnCardPopup;
    public static void CallCardPopup()
    {
        OnCardPopup?.Invoke();
    }
}
