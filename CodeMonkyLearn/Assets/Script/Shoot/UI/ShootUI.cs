using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ShootUI : MonoBehaviour
{
    [SerializeField] private Transform crosshair;  // 准星父物体

    [Header("准星图像引用")]
    [SerializeField] private Image topLine;
    [SerializeField] private Image bottomLine;
    [SerializeField] private Image leftLine;
    [SerializeField] private Image rightLine;

    private UnitStat unitStat;
    private ShootAction shootAction;

    // 准星数值
    private int obStability;
    private int obHandling;
    private float baseGap = 60f;      // 基础距离
    private float currentGap = 60f;  // 当前距离

    // 动画相关
    private bool isInAimState;       // 是否处于瞄准状态
    private float recoilTimer;        // 后坐力恢复计时
    private bool waitingForRecovery; // 是否等待恢复
    private const float recoilDelay = 0.1f;
    private const float recoveryDuration = 0.5f;

    void Start()
    {
        // 监听选中单位变化
        UnitActionSystem.Instance.OnSelectedUnitChanged += OnSelectedUnitChanged;
        crosshair?.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged -= OnSelectedUnitChanged;
        if (shootAction != null)
        {
            shootAction.OnEnterAimState -= ShootAction_OnEnterAimState;
            shootAction.OnExitAimState -= ShootAction_OnExitAimState;
        }
    }

    // 选中单位变化时更新准星数据
    private void OnSelectedUnitChanged(object sender, System.EventArgs e)
    {
        // 取消旧的事件监听
        if (shootAction != null)
        {
            shootAction.OnEnterAimState -= ShootAction_OnEnterAimState;
            shootAction.OnExitAimState -= ShootAction_OnExitAimState;
        }

        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        unitStat = selectedUnit?.GetComponent<UnitStat>();
        shootAction = selectedUnit?.GetComponent<ShootAction>();

        // 订阅新的事件
        if (shootAction != null)
        {
            shootAction.OnEnterAimState += ShootAction_OnEnterAimState;
            shootAction.OnExitAimState += ShootAction_OnExitAimState;
        }

        UpdateBaseGap();
    }

    private void ShootAction_OnEnterAimState(object sender, EventArgs e)
    {
        EnterAimState();
    }

    private void ShootAction_OnExitAimState(object sender, EventArgs e)
    {
        ExitAimState();
    }

    void Update()
    {
        UpdateCrosshairAnimation();
    }

    // 根据单位属性刷新基础距离
    private void UpdateBaseGap()
    {
        if (unitStat == null) return;

        obStability = unitStat.GetBattleStats().OBStability;
        obHandling = unitStat.GetBattleStats().OBHandling;

        // 计算基础距离：60 - (小于10的部分*3 + 大于10的部分*2)
        float stabilityReduction = Mathf.Min(obStability, 10) * 3f
                                 + Mathf.Max(obStability - 10, 0) * 2f;
        baseGap = Mathf.Max(60f - stabilityReduction, 10f);
    }

    // 准星动画更新（仅在瞄准状态下运行）
    private void UpdateCrosshairAnimation()
    {
        // 只有进入瞄准状态后才响应右键
        if (isInAimState)
        {
            // 按住右键：缩小到基础距离的一半
            if (Input.GetKey(KeyCode.Mouse1))
            {
                float minGap = baseGap * 0.5f;
                currentGap = Mathf.Lerp(currentGap, minGap, Time.deltaTime * 5f);
            }
            // 松开右键：平滑回到基础距离（恢复协程运行期间也会走这里，但协程会覆盖目标）
            else if (!waitingForRecovery)
            {
                currentGap = Mathf.Lerp(currentGap, baseGap, Time.deltaTime * 10f);
            }
        }

        // 射击后坐力恢复
        if (waitingForRecovery)
        {
            recoilTimer -= Time.deltaTime;
            if (recoilTimer <= 0f)
            {
                StartCoroutine(RecoverToBaseGap());
                waitingForRecovery = false;
            }
        }

        // 应用到四个图像
        ApplyGapToLines();
    }

    // 应用 gap 数值到四条准星线
    private void ApplyGapToLines()
    {
        if (topLine != null) topLine.rectTransform.anchoredPosition = new Vector2(0, currentGap);
        if (bottomLine != null) bottomLine.rectTransform.anchoredPosition = new Vector2(0, -currentGap);
        if (leftLine != null) leftLine.rectTransform.anchoredPosition = new Vector2(-currentGap, 0);
        if (rightLine != null) rightLine.rectTransform.anchoredPosition = new Vector2(currentGap, 0);
    }

    // 射击时调用（由 ShootManager 触发）
    public void OnShoot()
    {
        // 计算后坐力扩张量
        float recoil = Mathf.Max(30f - obHandling * 2f, 10f);
        currentGap += recoil;

        // 上限为基础距离的两倍
        currentGap = Mathf.Min(currentGap, baseGap * 2f);

        // 开始延迟等待
        recoilTimer = recoilDelay;
        waitingForRecovery = true;
    }

    // 射击后坐力恢复协程（方案B：动态目标）
    private System.Collections.IEnumerator RecoverToBaseGap()
    {
        float elapsed = 0f;
        float startGap = currentGap;

        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoveryDuration;

            // 动态目标：如果用户按着右键就往缩小目标走，否则往基础距离走
            float target = Input.GetKey(KeyCode.Mouse1) ? baseGap * 0.5f : baseGap;
            currentGap = Mathf.Lerp(startGap, target, t);
            yield return null;
        }

        // 协程结束后由 Update 正常接管
        currentGap = Input.GetKey(KeyCode.Mouse1) ? baseGap * 0.5f : baseGap;
    }

    // 显示/隐藏准星
    public void ShowCrosshair()
    {
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(true);
            currentGap = baseGap; // 重置
        }
    }

    public void HideCrosshair()
    {
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(false);
        }
    }

    // 进入/退出瞄准状态（由 ShootAction 调用）
    public void EnterAimState()
    {
        isInAimState = true;
        ShowCrosshair();
    }

    public void ExitAimState()
    {
        isInAimState = false;
        HideCrosshair();
    }
}
