using System;
using UnityEngine;

public class ShootAction : MonoBehaviour
{
    public event EventHandler OnEnterAimState;
    public event EventHandler OnExitAimState;

    private bool isAimActive;

    void Update()
    {
        // 按下右键进入瞄准
        if (Input.GetKeyDown(KeyCode.Mouse1) && !isAimActive)
        {
            EnterAim();
        }
        // 松开右键退出瞄准
        if (Input.GetKeyUp(KeyCode.Mouse1) && isAimActive)
        {
            ExitAim();
        }
    }

    private void EnterAim()
    {
        isAimActive = true;
        OnEnterAimState?.Invoke(this, EventArgs.Empty);
    }

    private void ExitAim()
    {
        isAimActive = false;
        OnExitAimState?.Invoke(this, EventArgs.Empty);
    }
}