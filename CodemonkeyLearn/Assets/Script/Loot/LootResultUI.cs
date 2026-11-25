using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结算 UI — 物品逐张显示在 LayoutGroup 中，同步增加资源，点击按钮返回列车
/// </summary>
public class LootResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;             // 结算面板
    [SerializeField] private Transform itemContainer;            // LayoutGroup 容器
    [SerializeField] private GameObject itemCardPrefab;          // 物品卡片预制体（带 Image）
    [SerializeField] private float itemShowInterval = 0.7f;      // 每件物品展示间隔

    [Header("资源汇总")]
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI partsText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI toolsText;
    [SerializeField] private TextMeshProUGUI medicineText;

    [Header("按钮")]
    [SerializeField] private Button returnToTrainButton;         // 返回列车按钮
    [SerializeField] private BattleStart battleStart;            // 战斗开始按钮引用

    /// <summary>结算面板是否正在显示</summary>
    public bool IsShowing => resultPanel != null && resultPanel.activeSelf;

    // 当前显示的累计资源
    private int foodCount, partsCount, moneyCount, toolsCount, medicineCount;
    private int prevFoodCount, prevPartsCount, prevMoneyCount, prevToolsCount, prevMedicineCount;

    // 是否全灭（动画播完后资源减半）
    private bool isHalfResources;

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (returnToTrainButton != null)
            returnToTrainButton.onClick.AddListener(ReturnToTrain);
    }

    /// <summary>显示结算</summary>
    public void ShowResult(bool halfResources = false)
    {
        isHalfResources = halfResources;

        Debug.Log($"[LootResultUI] ShowResult 被调用，halfResources={halfResources}");

        // 播放关卡完成音乐
        AudioManager.Instance?.PlayLevelCompleteBgm();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        // 隐藏战斗开始按钮
        if (battleStart != null)
            battleStart.HideButton();

        // 立即写入 ResourceManager（不等动画结束，避免用户跳过导致丢失）
        var inv = LootInventory.Instance;
        Debug.Log($"[LootResultUI] LootInventory.Instance={(inv != null ? "存在" : "NULL")}, Items.Count={inv?.Count ?? 0}");

        var resources = inv?.CalculateResources();
        if (resources != null)
        {
            int totalRes = 0;
            foreach (var kv in resources) totalRes += kv.Value;
            Debug.Log($"[LootResultUI] 结算资源总量={totalRes}");
            ResourceManager.Instance?.AddResources(resources);
        }

        // 结束战斗（销毁敌人 + 重置战斗状态 + 恢复部署位）
        if (UnitActionSystem.Instance != null && UnitActionSystem.Instance.IsBattleStart)
            UnitActionSystem.Instance.OnBattleEnd();

        // 销毁所有单位（玩家 + 敌人，统一用 LevelGrid）
        LevelGrid.Instance?.DestroyAllUnits();

        // 清空旧卡片
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // 重置累计
        foodCount = partsCount = moneyCount = toolsCount = medicineCount = 0;
        prevFoodCount = prevPartsCount = prevMoneyCount = prevToolsCount = prevMedicineCount = 0;
        RefreshResourceTexts();

        StartCoroutine(ShowResultRoutine());
    }

    private IEnumerator ShowResultRoutine()
    {
        var inv = LootInventory.Instance;
        var items = inv?.Items;
        Debug.Log($"[LootResultUI] ShowResultRoutine 开始，LootInventory.Instance={(inv != null ? "存在" : "NULL")}, items.Count={items?.Count ?? -1}");

        if (items == null || items.Count == 0)
        {
            Debug.Log($"[LootResultUI] items 为空（null={items == null}, count={items?.Count ?? -1}），跳过卡片展示");
            // 即使没有物品也要执行减半逻辑
            if (isHalfResources) HalveResources();
            yield break;
        }

        // 1秒延迟后再开始展示
        yield return new WaitForSeconds(1f);

        Debug.Log($"[LootResultUI] 开始展示 {items.Count} 个物品卡片");

        // 逐一生成卡片
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            // 生成卡片
            if (itemCardPrefab != null && itemContainer != null)
            {
                GameObject card = Instantiate(itemCardPrefab, itemContainer);
                var img = card.GetComponent<Image>();
                if (img != null && item.icon != null)
                {
                    img.sprite = item.icon;
                    // 添加光晕（传入类型和等级）
                    AddOutline(card, item.type, item.level);
                    // 弹出膨胀效果
                    StartCoroutine(PopCard(card));
                }
            }

            // 同步增加资源（手雷/枪械不转资源）
            AddResourceFromItem(item);
            RefreshResourceTexts();

            // 按住左键加速到 0.1 秒
            float interval = Input.GetMouseButton(0) ? 0.1f : itemShowInterval;
            yield return new WaitForSeconds(interval);
        }

        // 卡片展示完毕，全灭则资源减半
        if (isHalfResources) HalveResources();
    }

    /// <summary>全灭惩罚：本关获得资源总量减半，更新 UI 和 ResourceManager</summary>
    private void HalveResources()
    {
        // 本关获得减半（向下取整），算出要扣回的量
        var toRemove = new Dictionary<LootItemType, int>();
        toRemove[LootItemType.Food]    = foodCount - foodCount / 2;
        toRemove[LootItemType.Parts]   = partsCount - partsCount / 2;
        toRemove[LootItemType.Money]   = moneyCount - moneyCount / 2;
        toRemove[LootItemType.Tools]   = toolsCount - toolsCount / 2;
        toRemove[LootItemType.Medicine] = medicineCount - medicineCount / 2;

        foodCount /= 2; partsCount /= 2; moneyCount /= 2; toolsCount /= 2; medicineCount /= 2;

        // 从 ResourceManager 批量扣除差额（只触发一次事件）
        ResourceManager.Instance?.DeductResources(toRemove);

        // 刷新 UI 显示减半后的数量
        prevFoodCount = prevPartsCount = prevMoneyCount = prevToolsCount = prevMedicineCount = 0;
        RefreshResourceTexts();
    }

    private void AddResourceFromItem(LootItemRecord item)
    {
        int value = item.level switch { 1 => 1, 2 => 3, 3 => 6, _ => 0 };

        switch (item.type)
        {
            case LootItemType.Food: foodCount += value; break;
            case LootItemType.Parts: partsCount += value; break;
            case LootItemType.Money: moneyCount += value; break;
            case LootItemType.Tools: toolsCount += value; break;
            case LootItemType.Medicine: medicineCount += value; break;
        }
    }

    private void RefreshResourceTexts()
    {
        if (foodText != null && foodCount != prevFoodCount) { foodText.text = $"+{foodCount}"; PopText(foodText); }
        if (partsText != null && partsCount != prevPartsCount) { partsText.text = $"+{partsCount}"; PopText(partsText); }
        if (moneyText != null && moneyCount != prevMoneyCount) { moneyText.text = $"+{moneyCount}"; PopText(moneyText); }
        if (toolsText != null && toolsCount != prevToolsCount) { toolsText.text = $"+{toolsCount}"; PopText(toolsText); }
        if (medicineText != null && medicineCount != prevMedicineCount) { medicineText.text = $"+{medicineCount}"; PopText(medicineText); }

        prevFoodCount = foodCount;
        prevPartsCount = partsCount;
        prevMoneyCount = moneyCount;
        prevToolsCount = toolsCount;
        prevMedicineCount = medicineCount;
    }

    /// <summary>快速弹跳放大效果</summary>
    private void PopText(TextMeshProUGUI text)
    {
        StopCoroutine(PopRoutine(text));
        StartCoroutine(PopRoutine(text));
    }

    private IEnumerator PopRoutine(TextMeshProUGUI text)
    {
        float t = 0f;
        float duration = 0.15f;
        Vector3 originalScale = text.transform.localScale;

        // 快速放大到 1.4 倍
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            text.transform.localScale = originalScale * (1f + 0.4f * p);
            yield return null;
        }

        // 回弹
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            text.transform.localScale = originalScale * (1.4f - 0.4f * p);
            yield return null;
        }

        text.transform.localScale = originalScale;
    }

    private void ReturnToTrain()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
        LootInventory.Instance?.Clear();

        // 清理卡片
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // 重新显示战斗开始按钮
        if (battleStart != null)
            battleStart.ShowButton();

        // 返回列车界面
        TrainController.Instance?.EnterTrain();
    }

    /// <summary>给卡片添加光晕（枪械/手雷红色，其他按等级分色）</summary>
    private void AddOutline(GameObject card, LootItemType type, int level)
    {
        Color glowColor;

        if (type == LootItemType.Gun || type == LootItemType.Grenade)
        {
            glowColor = new Color(1f, 0.2f, 0.2f);   // 红色
        }
        else
        {
            glowColor = level switch
            {
                3 => new Color(1f, 0.85f, 0.3f),   // 金色
                2 => new Color(0.7f, 0.3f, 1f),     // 紫色
                _ => new Color(0.3f, 1f, 0.4f),     // 绿色
            };
        }

        // 创建光晕背景
        GameObject glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(card.transform, false);
        glow.transform.SetAsFirstSibling();

        var rect = glow.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(110f, 110f);

        var glowImg = glow.GetComponent<Image>();
        glowImg.raycastTarget = false;
        glowImg.sprite = CreateCircleSprite(64);
        glowImg.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.35f);

        // 光晕呼吸动画
        StartCoroutine(GlowPulse(glow, glowColor));
    }

    /// <summary>运行时生成纯色圆形 Sprite</summary>
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size / 2f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = dist <= radius ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator GlowPulse(GameObject glow, Color baseColor)
    {
        if (glow == null) yield break;
        var img = glow.GetComponent<Image>();
        if (img == null) yield break;

        float t = 0f;
        while (glow != null)
        {
            t += Time.deltaTime * 3f;
            float alpha = 0.25f + Mathf.Sin(t) * 0.1f; // 0.15 ~ 0.35
            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
    }

    /// <summary>卡片生成膨胀效果</summary>
    private IEnumerator PopCard(GameObject card)
    {
        float t = 0f;
        float duration = 0.25f;

        // 从 0.5 膨胀到 1.2
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float scale = 0.5f + 0.7f * p; // 0.5 -> 1.2
            card.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // 回弹到 1
        t = 0f;
        float bounceDuration = 0.12f;
        while (t < bounceDuration)
        {
            t += Time.deltaTime;
            float p = t / bounceDuration;
            card.transform.localScale = Vector3.one * (1.2f - 0.2f * p);
            yield return null;
        }

        card.transform.localScale = Vector3.one;
    }
}
