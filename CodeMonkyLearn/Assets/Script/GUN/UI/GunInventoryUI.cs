using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunInventoryUI : MonoBehaviour
{
    [SerializeField] private GUNcardUI gunCardPrefab;
    [SerializeField] private Transform gunCardContainer;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject scrollContent;  

    private List<GUNcardUI> activeCards = new List<GUNcardUI>();

    private void Start()
    {
        closeButton.onClick.AddListener(ClosePanel);
        scrollContent.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (IsOpen())
                ClosePanel();
            else
                ShowPanel();
        }
    }

    /// <summary>打开面板并刷新卡片列表</summary>
    public void ShowPanel()
    {
        scrollContent.SetActive(true);
        RefreshCards();
    }

    /// <summary>关闭面板</summary>
    public void ClosePanel()
    {
        scrollContent.SetActive(false);
    }

    /// <summary>面板是否打开</summary>
    public bool IsOpen() => scrollContent != null && scrollContent.activeSelf;

    /// <summary>刷新所有卡片（清空后重新生成）</summary>
    public void RefreshCards()
    {
        ClearCards();

        foreach (var gun in GunManager.Instance.GetAllGuns())
        {
            GUNcardUI card = CreateCard(gun.inventoryId);
            activeCards.Add(card);
        }
    }

    private void ClearCards()
    {
        foreach (var card in activeCards)
        {
            Destroy(card.gameObject);
        }
        activeCards.Clear();
    }

    private GUNcardUI CreateCard(int gunId)
    {
        GUNcardUI card = Instantiate(gunCardPrefab, gunCardContainer);
        card.SetGunData(gunId);
        return card;
    }
}
