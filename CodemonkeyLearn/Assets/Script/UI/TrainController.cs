using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TrainController : MonoBehaviour
{
    public static TrainController Instance { get; private set; }

    [Header("相机")]
    [SerializeField] private GameObject mainCamera;      // 主虚拟相机
    [SerializeField] private GameObject trainCamera;     // 列车固定相机
    [SerializeField] private CameraController cameraController;  // 相机控制点

    [Header("撤离动画")]
    [SerializeField] private GameObject evacCameraPlain;      // 平原撤离相机
    [SerializeField] private GameObject evacCameraSnow;       // 雪地撤离相机
    [SerializeField] private GameObject evacCameraDesert;     // 沙漠撤离相机
    [SerializeField] private Animator evacAnimatorPlain;      // 平原撤离物体 Animator
    [SerializeField] private Animator evacAnimatorSnow;       // 雪地撤离物体 Animator
    [SerializeField] private Animator evacAnimatorDesert;     // 沙漠撤离物体 Animator
    [SerializeField] private float evacAnimationDuration = 5f; // 撤离动画时长

    [Header("黑屏过渡")]
    [SerializeField] private Image fadeOverlay;               // 全屏黑底 Image（RaycastTarget 关闭）

    [Header("UI")]
    [SerializeField] private GameObject panelGame;       // 游戏 UI 面板
    [SerializeField] private GameObject panelTrain;      // 列车 UI 面板

    /// <summary>当前是否在列车界面</summary>
    public bool IsInTrain { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 强行确保初始状态：列车UI关，游戏UI开
        if (panelTrain != null) panelTrain.SetActive(false);
        if (panelGame != null) panelGame.SetActive(true);

        // 所有撤离 Animator 初始暂停（等点击"返回列车"时才播放）— 放 Awake 更早阻止循环
        if (evacAnimatorPlain != null) evacAnimatorPlain.speed = 0f;
        if (evacAnimatorSnow != null) evacAnimatorSnow.speed = 0f;
        if (evacAnimatorDesert != null) evacAnimatorDesert.speed = 0f;
    }

    private void Start()
    {
        // 初始状态：游戏模式
        EnterGameMode();

        // 黑屏 overlay 初始透明
        if (fadeOverlay != null)
            fadeOverlay.gameObject.SetActive(false);
    }

    /// <summary>进入列车界面（带撤离动画）</summary>
    public void EnterTrain()
    {
        IsInTrain = true;

        if (panelGame != null) panelGame.SetActive(false);
        // panelTrain 等撤离动画播完再开

        // 结束当前战斗（如果正在战斗中）
        if (UnitActionSystem.Instance != null && UnitActionSystem.Instance.IsBattleStart)
            UnitActionSystem.Instance.OnBattleEnd();

        // 销毁所有单位 + 重置回合
        LevelGrid.Instance?.DestroyAllUnits();
        if (TurnSystem.Instance != null)
            TurnSystem.Instance.ResetToPlayerTurn();

        // 播放撤离 BGM（播完后自动切列车 BGM）
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEvacThenTrainBgm();

        // 播放撤离动画
        StartCoroutine(PlayEvacuationSequence());
    }

    /// <summary>撤离动画序列：黑屏 → 切撤离相机 → 亮屏 → 播动画 → 黑屏 → 切列车相机 → 亮屏</summary>
    private IEnumerator PlayEvacuationSequence()
    {
        int level = LevelGrid.Instance != null ? LevelGrid.Instance.CurrentLevel : 1;

        // 1. 根据关卡名称获取撤离相机和 Animator
        (GameObject evacCam, Animator evacAnim) = level switch
        {
            1 => (evacCameraPlain, evacAnimatorPlain),
            2 => (evacCameraSnow, evacAnimatorSnow),
            3 => (evacCameraDesert, evacAnimatorDesert),
            _ => (evacCameraPlain, evacAnimatorPlain)
        };

        // 2. 提前启动撤离物体动画（在黑屏/切换相机期间播放）
        if (evacAnim != null)
        {
            evacAnim.speed = 1f;
            evacAnim.Play("Empty", 0, 0f);    // 先切回空状态
            evacAnim.SetTrigger("PlayEvac");
        }

        // 3. 黑屏 → 切换撤离相机 → 亮屏
        if (evacCam != null)
        {
            yield return StartCoroutine(FadeCameraSwitch(from: mainCamera, to: evacCam));

            // 4. 等待撤离动画完成
            yield return new WaitForSeconds(evacAnimationDuration);

            // 5. 黑屏 → 切换列车相机 → 亮屏
            yield return StartCoroutine(FadeCameraSwitch(from: evacCam, to: trainCamera));
        }
        else
        {
            // 没有撤离相机，直接切到列车相机
            Debug.LogWarning("[TrainController] 撤离相机为 null，直接切换到列车相机");
            yield return StartCoroutine(FadeCameraSwitch(from: mainCamera, to: trainCamera));
        }

        // 6. 亮屏后显示列车 UI
        if (panelTrain != null) panelTrain.SetActive(true);
    }

    /// <summary>黑屏过渡切换相机：0.5秒变黑 → 切换相机 → 0.5秒恢复</summary>
    private IEnumerator FadeCameraSwitch(GameObject from, GameObject to)
    {
        float fadeDuration = 0.5f;

        // 渐黑
        yield return StartCoroutine(FadeTo(1f, fadeDuration));

        // 切换相机
        if (from != null)
            from.SetActive(false);
        if (to != null)
            to.SetActive(true);

        EnsureSingleAudioListener(to);

        // 渐亮
        yield return StartCoroutine(FadeTo(0f, fadeDuration));
    }

    /// <summary>渐变到目标透明度（0=透明，1=全黑）</summary>
    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.gameObject.SetActive(true);

        float startAlpha = fadeOverlay.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            Color c = fadeOverlay.color;
            c.a = alpha;
            fadeOverlay.color = c;
            yield return null;
        }

        // 确保到达目标值
        Color final = fadeOverlay.color;
        final.a = targetAlpha;
        fadeOverlay.color = final;

        // 完全透明后关闭 overlay
        if (targetAlpha <= 0f)
            fadeOverlay.gameObject.SetActive(false);
    }

    /// <summary>返回游戏界面</summary>
    public void EnterGameMode()
    {
        IsInTrain = false;

        // 切换战斗 BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBattleBgm();

        // 关闭所有撤离相机
        if (evacCameraPlain != null) evacCameraPlain.SetActive(false);
        if (evacCameraSnow != null) evacCameraSnow.SetActive(false);
        if (evacCameraDesert != null) evacCameraDesert.SetActive(false);

        // 切换相机前，确保只有一个 Audio Listener
        SwapCamera(trainCamera, mainCamera);

        // 重置相机控制点到初始位置
        if (cameraController != null)
            cameraController.transform.position = new Vector3(6f, 0f, -2f);

        if (panelGame != null) panelGame.SetActive(true);
        if (panelTrain != null) panelTrain.SetActive(false);
    }

    /// <summary>全局禁用所有 AudioListener，然后仅启用目标相机及其子物体的第一个（保留 AudioManager 上的 Listener）</summary>
    private void EnsureSingleAudioListener(GameObject targetCamera)
    {
        // 1. 全局禁用所有 AudioListener
        var allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var l in allListeners)
        {
            l.enabled = false;
        }

        // 2. AudioManager（DontDestroyOnLoad）始终需要 AudioListener
        if (AudioManager.Instance != null)
        {
            var amListener = AudioManager.Instance.GetComponent<AudioListener>();
            if (amListener != null)
                amListener.enabled = true;
        }

        // 3. 在目标相机及其子物体中启用第一个 AudioListener
        if (targetCamera != null)
        {
            var targetListeners = targetCamera.GetComponentsInChildren<AudioListener>(true);
            if (targetListeners.Length > 0)
            {
                targetListeners[0].enabled = true;
                Debug.Log($"[TrainController] EnsureSingleAudioListener -> {targetCamera.name}，总AudioListener={allListeners.Length}，已启用目标相机的第1个");
            }
            else
            {
                Debug.LogWarning($"[TrainController] EnsureSingleAudioListener -> {targetCamera.name} 上没有 AudioListener！");
            }
        }
    }

    /// <summary>切换相机，先禁用旧相机的 AudioListener 再激活新相机</summary>
    private void SwapCamera(GameObject fromCamera, GameObject toCamera)
    {
        if (fromCamera != null)
            fromCamera.SetActive(false);

        if (toCamera != null)
            toCamera.SetActive(true);

        EnsureSingleAudioListener(toCamera);
    }
}
