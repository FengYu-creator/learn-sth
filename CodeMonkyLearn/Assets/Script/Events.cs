using System;
using UnityEngine;

public static class Events
{

    public static event Action BattleStarted;
    public static void CallBattleStarted()
    {
        BattleStarted?.Invoke();
    }



}
