using System;
using UnityEngine;

public static class Events
{
    public static event Action BattleStarted;
    public static void CallBattleStarted()
    {
        BattleStarted?.Invoke();
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
}
