using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreatUnitUI : MonoBehaviour
{
    [SerializeField] private Transform creatPrefab;
    [SerializeField] private Transform creatContainerTransform;
    [SerializeField] private Button remakeButton;
    [SerializeField] private Button recruitButton;           // 招募按钮
    [SerializeField] private TextMeshProUGUI recruitCountText;  // 显示剩余可招募单位数
    private Transform card1;
    private Transform card2;
    private Transform card3;
    private bool needWait = false;
    private bool isPanelOpen = false;

    private void Awake()
    {
        remakeButton.onClick.AddListener(OnClickRemake);
        recruitButton.onClick.AddListener(OnClickRecruit);
        StatManager.Instance.OnClickCreat += StatManager_OnClickCreate;

        Events.BattleStarted += OnBattleStarted;
        Events.BattleEnded += OnBattleEnded;
    }

    private void OnDestroy()
    {
        if (FacilityManager.Instance != null)
            FacilityManager.Instance.OnRecruitCountChanged -= OnRecruitCountChanged;
        Events.BattleStarted -= OnBattleStarted;
        Events.BattleEnded -= OnBattleEnded;
    }

    void Start()
    {
        remakeButton.gameObject.SetActive(false);

        // Start 时所有单例已就绪，订阅事件并刷新
        if (FacilityManager.Instance != null)
            FacilityManager.Instance.OnRecruitCountChanged += OnRecruitCountChanged;
        RefreshRecruitButton();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            if (!isPanelOpen)
            {
                OpenPanel();
            }
            else
            {
                ClosePanel();
            }
        }
    }

    // ============================================================
    //  可招募计数
    // ============================================================

    /// <summary>战斗开始：隐藏招募按钮</summary>
    private void OnBattleStarted()
    {
        if (recruitButton != null)
            recruitButton.gameObject.SetActive(false);
    }

    /// <summary>战斗结束（新关卡开始）：显示招募按钮</summary>
    private void OnBattleEnded()
    {
        if (recruitButton != null)
            recruitButton.gameObject.SetActive(true);
        RefreshRecruitButton();
    }

    private void OnRecruitCountChanged(int count)
    {
        RefreshRecruitButton();
    }

    private void RefreshRecruitButton()
    {
        if (recruitButton == null || recruitCountText == null) return;

        int count = FacilityManager.Instance != null ? FacilityManager.Instance.RecruitCount : 0;
        recruitCountText.text = "可招募单位 " + count + " 个";
        recruitButton.interactable = count > 0;
    }

    /// <summary>点击招募按钮</summary>
    public void OnClickRecruit()
    {
        if (FacilityManager.Instance == null) return;
        if (!FacilityManager.Instance.TryConsumeRecruit()) return;
        OpenPanel();
    }

    // ============================================================
    //  面板开关
    // ============================================================

    /// <summary>打开招募面板</summary>
    public void OpenPanel()
    {
        if (isPanelOpen) return;
        Create3Card();
        remakeButton.gameObject.SetActive(true);
        creatContainerTransform.gameObject.SetActive(true);
        recruitButton.gameObject.SetActive(false);
        isPanelOpen = true;
    }

    /// <summary>关闭招募面板</summary>
    public void ClosePanel()
    {
        if (!isPanelOpen) return;
        Remove3Card();
        creatContainerTransform.gameObject.SetActive(false);
        remakeButton.gameObject.SetActive(false);
        recruitButton.gameObject.SetActive(true);
        RefreshRecruitButton();
        isPanelOpen = false;
    }

    public void Create3Card()
    {
         card1 = Instantiate(creatPrefab, creatContainerTransform);
         card2 = Instantiate(creatPrefab, creatContainerTransform);
         card3 = Instantiate(creatPrefab, creatContainerTransform);
    }
    public void Remove3Card()
    {
        if (card1 != null) Destroy(card1.gameObject);
        if (card2 != null) Destroy(card2.gameObject);
        if (card3 != null) Destroy(card3.gameObject);
    }
    public void OnClickRemake()
    {
        if (!needWait)
        {
            needWait = true;
            Remove3Card();
            StartCoroutine(remake());
        }
    }
    IEnumerator remake()
    {
        yield return null;
        Create3Card();
        yield return new WaitForSeconds(1);
        needWait = false;
    }
    public void StatManager_OnClickCreate(object sender,EventArgs e)
    {
        ClosePanel();
    }
}
