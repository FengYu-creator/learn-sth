using System.Collections;
using UnityEngine;

/// <summary>
/// 搜刮控制器 — 挂在单位身上，管理搜刮逻辑
/// 头顶贴图放在血条上方
/// </summary>
public class LootController : MonoBehaviour
{
    [SerializeField] private Transform healthBarTransform;     // 血条 Transform（用于定位）
    [SerializeField] private GameObject floatingIconPrefab;   // 头顶贴图预制体（带 Image 的 World Space UI）

    private Unit unit;
    private bool isSearching = false;
    private Coroutine searchCoroutine;
    private float searchTimer = 0f;
    private float nextItemInterval = 1f;

    /// <summary>搜刮时使用的数据库</summary>
    private LootItemDatabase itemDatabase;

    public bool IsSearching => isSearching;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    /// <summary>开始搜刮</summary>
    public void StartSearching(LootItemDatabase database)
    {
        if (isSearching) return;

        itemDatabase = database;
        isSearching = true;
        searchTimer = 0f;
        nextItemInterval = Random.Range(0.5f, 1.5f);
        searchCoroutine = StartCoroutine(SearchRoutine());
    }

    /// <summary>停止搜刮</summary>
    public void StopSearching()
    {
        isSearching = false;
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
    }

    private IEnumerator SearchRoutine()
    {
        while (isSearching)
        {
            yield return null;
            searchTimer += Time.deltaTime;

            if (searchTimer >= nextItemInterval)
            {
                searchTimer = 0f;
                nextItemInterval = Random.Range(0.5f, 1.5f);
                DiscoverItem();
            }
        }
    }

    /// <summary>发现一件物品</summary>
    private void DiscoverItem()
    {
        if (itemDatabase == null)
        {
            Debug.LogWarning("[LootController] DiscoverItem: itemDatabase 为 null");
            return;
        }

        var (type, level) = itemDatabase.GetRandomTypeAndLevel();
        Sprite icon = itemDatabase.GetRandomSprite(type, level);

        if (icon == null)
        {
            Debug.LogWarning($"[LootController] 没有找到 {type} Lv{level} 的贴图");
            return;
        }

        var record = new LootItemRecord { type = type, level = level, icon = icon };
        LootInventory.Instance?.AddItem(record);

        StartCoroutine(ShowFloatingIcon(icon, type, level));
    }

    /// <summary>在血条上方生成贴图，带光晕，1秒后消失</summary>
    private IEnumerator ShowFloatingIcon(Sprite icon, LootItemType type, int level)
    {
        if (floatingIconPrefab == null) yield break;

        Vector3 basePos = healthBarTransform != null
            ? healthBarTransform.position
            : transform.position + Vector3.up * 2.5f;
        Vector3 spawnPos = basePos + Vector3.up * 0.8f;

        GameObject floating = Instantiate(floatingIconPrefab, spawnPos, Quaternion.identity, transform);

        var sr = floating.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = icon;
            AddFloatingGlow(floating, icon, type, level);
        }

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            floating.transform.position = spawnPos + Vector3.up * (elapsed * 1.2f);

            // 面向摄像机
            if (Camera.main != null)
                floating.transform.forward = Camera.main.transform.forward;

            // 渐变透明
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - elapsed;
                sr.color = c;
            }

            yield return null;
        }

        Destroy(floating);
    }

    /// <summary>给头顶贴图添加圆形渐变光晕</summary>
    private void AddFloatingGlow(GameObject floating, Sprite icon, LootItemType type, int level)
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

        // 用圆形渐变贴图而不是重影
        Sprite circleSprite = CreateCircleSprite(64);

        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(floating.transform, false);
        glow.transform.localPosition = Vector3.back * 0.05f;
        glow.transform.localScale = Vector3.one * 6f;

        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = circleSprite;
        glowSr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.8f);
        glowSr.sortingOrder = -1;
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
}
