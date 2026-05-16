using System;
using UnityEngine;
using UnityEngine.UI;

public class ShootUI : MonoBehaviour
{
    [Header("瞄准按钮")]
    [SerializeField] private Button aimButton;

    [Header("UI 引用")]
    [SerializeField] private Transform crosshair;  // 准星父物体

    [Header("准星图像引用")]
    [SerializeField] private Image topLine;
    [SerializeField] private Image bottomLine;
    [SerializeField] private Image leftLine;
    [SerializeField] private Image rightLine;
    [SerializeField] private Transform specialCore;

    // 准星数值（由 UnitStat 推送更新）
    private int obStability;
    private int obHandling;
    private float baseGap = 60f;      // 基础距离
    private float currentGap = 60f;   // 当前距离
    private float innerRing;          // 内环 = baseGap * 0.5
    private float outerRing;          // 外环 = baseGap * 1.5
    private const float shrinkSpeed = 60f;   // 收缩速度（像素/秒）

    // 松开右键平滑恢复
    private bool isReleasingRightButton;
    private float releaseTimer;
    private const float releaseDuration = 0.1f;
    private float releaseStartGap;

    // 动画相关
    private bool isInAimState;       // 是否处于瞄准状态
    private float recoilTimer;        // 后坐力恢复计时
    private bool waitingForRecovery; // 是否等待恢复
    private const float recoilDelay = 0.1f;

    // 射击冷却
    private float shootCooldown = 0f;     // 射击间隔冷却计时
    private float fireInterval = 0.2f;   // 射击间隔（秒），从 UnitStat 获取

    // specialCore 摆动参数
    [Header("SpecialCore 摆动参数")]
    [SerializeField] private float swingRange = 0.5f;  // 摆动幅度
    [SerializeField] private float swingSpeed = 2f;    // 摆动速度
    private const float coreY = 0.91f;                  // Y 轴固定值

    private UnitStat currentStat;
    private ShootAction currentShootAction;

    void Start()
    {
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(false);
        }

        // 绑定瞄准按钮点击事件
        if (aimButton != null)
        {
            aimButton.onClick.AddListener(TryToggleAim);
        }

        // 订阅 Events 的瞄准状态事件
        Events.OnAimStateEntered += OnAimStateEntered;
        Events.OnAimStateExited += OnAimStateExited;

        // 订阅 UnitActionSystem 的选中单位变化事件
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged += OnSelectedUnitChanged;
        }

        // 初始化当前单位数据
        UpdateStatsFromCurrentUnit();
        UpdateAimButtonInteractable();

        // 初始隐藏瞄准按钮，战斗开始后显示
        SetAimButtonVisible(false);
        Events.BattleStarted += OnBattleStarted;
    }

    private void OnDestroy()
    {
        if (aimButton != null)
        {
            aimButton.onClick.RemoveListener(TryToggleAim);
        }

        Events.OnAimStateEntered -= OnAimStateEntered;
        Events.OnAimStateExited -= OnAimStateExited;

        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged -= OnSelectedUnitChanged;
        }

        Events.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
        SetAimButtonVisible(true);
        UpdateAimButtonInteractable();
    }

    private void SetAimButtonVisible(bool visible)
    {
        if (aimButton != null)
            aimButton.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 点击瞄准按钮时调用，条件检查后进入瞄准状态
    /// </summary>
    public void TryToggleAim()
    {
        if (isInAimState) return;
        if (currentShootAction != null && !currentShootAction.CanEnterShoot) return;
        Events.CallAimStateEntered();
    }

    /// <summary>
    /// 根据 ShootAction.CanEnterShoot 更新瞄准按钮交互状态
    /// </summary>
    private void UpdateAimButtonInteractable()
    {
        if (aimButton == null || !aimButton.gameObject.activeInHierarchy) return;
        aimButton.interactable = (currentShootAction != null && currentShootAction.CanEnterShoot);
    }

    /// <summary>
    /// 单位变更时，从 UnitStat 获取新数据并刷新准星
    /// </summary>
    private void OnSelectedUnitChanged(object sender, EventArgs e)
    {
        UpdateStatsFromCurrentUnit();
        SetAimButtonVisible(currentShootAction != null);
        UpdateAimButtonInteractable();
    }

    /// <summary>
    /// 进入瞄准状态
    /// </summary>
    private void OnAimStateEntered()
    {
        ShowCrosshair();
        isInAimState = true;
        Events.IsInAimState = true;
        SetAimButtonVisible(false);
    }

    /// <summary>
    /// 退出瞄准状态
    /// </summary>
    private void OnAimStateExited()
    {
        HideCrosshair();
        isInAimState = false;
        Events.IsInAimState = false;
        shootCooldown = 0f;
        SetAimButtonVisible(true);
        UpdateAimButtonInteractable();
    }

    /// <summary>
    /// 从当前选中单位获取数据并更新准星基础数值
    /// </summary>
    private void UpdateStatsFromCurrentUnit()
    {
        if (UnitActionSystem.Instance == null) return;

        var selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null)
        {
            currentStat = null;
            currentShootAction = null;
            return;
        }

        currentStat = selectedUnit.GetComponent<UnitStat>();
        currentShootAction = selectedUnit.GetComponent<ShootAction>();

        if (currentStat == null) return;

        // 更新准星数值
        obStability = currentStat.battleStats.OBStability;
        obHandling = currentStat.battleStats.OBHandling;
        fireInterval = currentStat.battleStats.OBFireRate;

        // 计算基础距离：60 - (小于10的部分*3 + 大于10的部分*2)
        float stabilityReduction = Mathf.Min(obStability, 10) * 3f 
                                 + Mathf.Max(obStability - 10, 0) * 2f;
        baseGap = Mathf.Max(60f - stabilityReduction, 10f);

        // 同步计算内外环
        innerRing = baseGap * 0.5f;
        outerRing = baseGap * 1.5f;

        // 如果没有后坐力恢复中，立即同步到基础距离
        if (!waitingForRecovery)
        {
            currentGap = baseGap;
        }
    }

    void Update()
    {
        // ESC 退出瞄准
        if (isInAimState && Input.GetKeyDown(KeyCode.Escape))
        {
            Events.CallAimStateExited();
        }

        // 只负责动画相关逻辑
        UpdateCrosshairAnimation();

        // 进入瞄准状态后，检测左键发布射击事件
        if (isInAimState)
        {
            // 冷却减少
            if (shootCooldown > 0f)
            {
                shootCooldown -= Time.deltaTime;
            }

            // 按下左键 + 冷却完毕 → 发布射击事件
            if (Input.GetKeyDown(KeyCode.Mouse0) && shootCooldown <= 0f)
            {
                shootCooldown = fireInterval;

                // 执行后坐力效果
                float recoil = Mathf.Max(30f - obHandling * 2f, 10f);
                currentGap += recoil;
                currentGap = Mathf.Min(currentGap, baseGap * 2f);
                recoilTimer = recoilDelay;
                waitingForRecovery = true;

                // 发布射击事件（供其他系统响应，如 LaserBeam 等）
                Events.CallShoot();

                // 执行射击（计算路径、生成子弹、锁定本回合）
                if (currentShootAction != null)
                {
                    currentShootAction.ExecuteShoot();
                }

                // 射击后立即更新按钮状态（可能退出瞄准状态）
                UpdateAimButtonInteractable();
            }
        }
    }

    // 准星动画更新（仅在瞄准状态下运行）
    private void UpdateCrosshairAnimation()
    {
        if (!isInAimState) return;

        bool isRightBtnDown = Input.GetKey(KeyCode.Mouse1);
        bool isLeftBtnDown = Input.GetKey(KeyCode.Mouse0);

        // 射击后坐力恢复计时
        if (waitingForRecovery)
        {
            recoilTimer -= Time.deltaTime;
            if (recoilTimer <= 0f)
            {
                waitingForRecovery = false;
            }
        }

        // ===== currentGap 更新逻辑 =====
        // 1. 按住右键：线性收缩到内环
        if (isRightBtnDown)
        {
            isReleasingRightButton = false;
            if (currentGap > innerRing)
            {
                currentGap -= shrinkSpeed * Time.deltaTime;
            }
        }
        // 2. 松开右键：左键已松开 → 0.1s 平滑到基础值
        else
        {
            if (!isLeftBtnDown)
            {
                if (!isReleasingRightButton)
                {
                    isReleasingRightButton = true;
                    releaseTimer = releaseDuration;
                    releaseStartGap = currentGap;
                }

                releaseTimer -= Time.deltaTime;
                if (releaseTimer > 0f)
                {
                    float t = 1f - (releaseTimer / releaseDuration);
                    currentGap = Mathf.Lerp(releaseStartGap, baseGap, t);
                }
                else
                {
                    currentGap = baseGap;
                    isReleasingRightButton = false;
                }
            }
        }

        // 3. 自动收缩：currentGap > 基础值 → 线性收缩
        if (currentGap > baseGap)
        {
            currentGap -= shrinkSpeed * Time.deltaTime;
        }

        // 4. 范围钳制到 [内环, 外环]
        currentGap = Mathf.Clamp(currentGap, innerRing, outerRing);

        // 应用到四个图像
        ApplyGapToLines();

        // crosshair 跟随鼠标
        UpdateCrosshairPosition();

        // 更新 specialCore 位置（世界坐标）
        UpdateSpecialCoreMovement();
    }

    /// <summary>
    /// crosshair 跟随鼠标位置
    /// </summary>
    private void UpdateCrosshairPosition()
    {
        if (crosshair != null)
        {
            crosshair.position = Input.mousePosition;
        }
    }

    /// <summary>
    /// 鼠标射线与 Y=coreY 平面求交（视觉对齐）
    /// </summary>
    private Vector3 GetMouseWorldPosAtCoreY()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // 射线与平面 Y = coreY 求交
        // t = (coreY - origin.y) / direction.y
        if (Mathf.Abs(ray.direction.y) < 0.001f)
            return Vector3.zero;

        float t = (coreY - ray.origin.y) / ray.direction.y;
        if (t < 0) return Vector3.zero;

        return ray.origin + ray.direction * t;
    }

    /// <summary>
    /// 更新 specialCore 世界坐标：鼠标射线与 Y=coreY 平面交点 + 垂直方向摆动
    /// </summary>
    private void UpdateSpecialCoreMovement()
    {
        if (specialCore == null) return;
        if (UnitActionSystem.Instance == null) return;

        var unit = UnitActionSystem.Instance.GetSelectedUnit();
        if (unit == null) return;

        // 鼠标射线与 Y=coreY 平面求交（视觉对齐）
        Vector3 basePos = GetMouseWorldPosAtCoreY();
        if (basePos == Vector3.zero) return;

        // 角色到基础位置的方向
        Vector3 toBasePos = (basePos - unit.transform.position).normalized;

        // 垂直方向（叉积）
        Vector3 perpDir = Vector3.Cross(toBasePos, Vector3.up).normalized;

        // 正弦摆动
        float swingOffset = Mathf.Sin(Time.time * swingSpeed) * swingRange;

        // 最终位置
        Vector3 targetPos = basePos + perpDir * swingOffset;
        targetPos.y = coreY;

        specialCore.position = targetPos;

        // 写入全局激光目标位置，供 LaserBeam 读取
        Events.LaserTargetPosition = targetPos;
    }

    // 应用 gap 数值到四条准星线
    private void ApplyGapToLines()
    {
        if (topLine != null) topLine.rectTransform.anchoredPosition = new Vector2(0, currentGap);
        if (bottomLine != null) bottomLine.rectTransform.anchoredPosition = new Vector2(0, -currentGap);
        if (leftLine != null) leftLine.rectTransform.anchoredPosition = new Vector2(-currentGap, 0);
        if (rightLine != null) rightLine.rectTransform.anchoredPosition = new Vector2(currentGap, 0);
    }

    // 显示/隐藏准星
    private void ShowCrosshair()
    {
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(true);
            currentGap = baseGap;
        }
    }

    private void HideCrosshair()
    {
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(false);
        }
    }
}
