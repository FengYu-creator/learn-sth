using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理器 — 全局单例
/// 支持 BGM 队列播放（当前曲播完自动播下一曲）+ 音效并发播放
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ================================================================
    //  BGM（背景音乐）— 队列播放，一首播完才播下一首
    // ================================================================
    [Header("—— BGM 背景音乐 ——")]
    [Tooltip("列车界面背景音乐（循环）")]
    public AudioClip bgmTrainClip;
    [Tooltip("战斗背景音乐（循环）")]
    public AudioClip bgmBattleClip;
    [Tooltip("撤离音乐（完整播完才切列车BGM）")]
    public AudioClip bgmEvacClip;
    [Tooltip("关卡完成音乐")]
    public AudioClip bgmLevelCompleteClip;

    // ================================================================
    //  SFX（音效）— 可同时播放
    // ================================================================
    [Header("—— 战斗音效 ——")]
    [Tooltip("射击（玩家与敌人共用）")]
    public AudioClip shootClip;
    [Tooltip("换弹")]
    public AudioClip reloadClip;
    [Tooltip("子弹命中敌人")]
    public AudioClip bulletHitClip;
    [Tooltip("死亡（玩家与敌人共用）")]
    public AudioClip deathClip;

    [Header("—— UI 音效 ——")]
    [Tooltip("点击按钮")]
    public AudioClip uiClickClip;
    [Tooltip("卡片弹出")]
    public AudioClip cardPopupClip;


    [Header("—— 场景音效 ——")]
    [Tooltip("脚步声")]
    public AudioClip footstepClip;
    [Tooltip("搜刮物品")]
    public AudioClip lootSearchClip;

    [Header("—— 配置 ——")]
    [Tooltip("背景音乐音量")]
    [SerializeField] private float bgmVolume = 0.7f;
    [Tooltip("音效音量")]
    [SerializeField] private float sfxVolume = 1f;
    [Tooltip("同时播放音效的最大数量")]
    [SerializeField] private int maxSfxSources = 10;

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    // ================================================================
    //  内部
    // ================================================================
    private AudioSource bgmSource;
    private List<AudioSource> sfxPool;
    private Queue<AudioClip> bgmQueue = new();
    private bool isPlayingBgmQueue;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM 专用 AudioSource
        var bgmGo = new GameObject("BGM_Source");
        bgmGo.transform.SetParent(transform);
        bgmSource = bgmGo.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = bgmVolume;

        // SFX 对象池
        sfxPool = new List<AudioSource>(maxSfxSources);
        for (int i = 0; i < maxSfxSources; i++)
        {
            var go = new GameObject($"SFX_Source_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = sfxVolume;
            sfxPool.Add(src);
        }

        // 确保 AudioManager 上有 AudioListener（避免 "no audio listeners" 错误）
        if (GetComponent<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        SubscribeEvents();
    }

    private void Update()
    {
        // BGM 队列自动推进
        if (!bgmSource.isPlaying && bgmQueue.Count > 0)
        {
            PlayNextBgm();
        }
        else if (!bgmSource.isPlaying && isPlayingBgmQueue)
        {
            isPlayingBgmQueue = false;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ================================================================
    //  事件订阅
    // ================================================================
    private float lastFootstepTime;
    private const float FOOTSTEP_INTERVAL = 0.4f; // 脚步声最小间隔
    private AudioSource footstepSource; // 脚步声专用音源

    private float lastLootSearchTime;
    private const float LOOT_SEARCH_INTERVAL = 0.8f; // 搜刮音效最小间隔
    private AudioSource lootSearchSource; // 搜刮专用音源

    private float lastBulletHitTime;
    private const float BULLET_HIT_INTERVAL = 0.1f; // 命中音效最小间隔（霰弹枪多弹丸同时命中）

    private void SubscribeEvents()
    {
        Events.OnShoot       += () => PlaySfx(shootClip);
        Events.OnBulletHit   += PlayBulletHit;
        Events.OnReload      += () => PlaySfx(reloadClip);
        Events.OnDeath       += (_) => PlaySfx(deathClip);
        Events.OnUIClick     += () => PlaySfx(uiClickClip);
        Events.OnCardPopup   += () => PlaySfx(cardPopupClip);

        Events.OnFootstep    += PlayFootstep;
        Events.OnFootstepStop += StopFootstep;
        Events.OnLootSearch  += PlayLootSearch;
        Events.OnLootSearchStop += StopLootSearch;
    }

    private void UnsubscribeEvents()
    {
        Events.OnShoot       -= () => PlaySfx(shootClip);
        Events.OnBulletHit   -= PlayBulletHit;
        Events.OnReload      -= () => PlaySfx(reloadClip);
        Events.OnDeath       -= (_) => PlaySfx(deathClip);
        Events.OnUIClick     -= () => PlaySfx(uiClickClip);
        Events.OnCardPopup   -= () => PlaySfx(cardPopupClip);

        Events.OnFootstep    -= PlayFootstep;
        Events.OnFootstepStop -= StopFootstep;
        Events.OnLootSearch  -= PlayLootSearch;
        Events.OnLootSearchStop -= StopLootSearch;
    }

    private void PlayBulletHit(Unit attacker, Unit target, int damage)
    {
        if (Time.time - lastBulletHitTime < BULLET_HIT_INTERVAL) return;
        lastBulletHitTime = Time.time;
        PlaySfx(bulletHitClip);
    }

    private void PlayFootstep()
    {
        if (footstepSource != null && footstepSource.isPlaying) return;
        if (Time.time - lastFootstepTime < FOOTSTEP_INTERVAL) return;
        lastFootstepTime = Time.time;

        footstepSource ??= CreateLoopSource("Footstep_Source");
        footstepSource.pitch = Random.Range(0.95f, 1.05f);
        footstepSource.PlayOneShot(footstepClip);
    }

    private void StopFootstep()
    {
        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Stop();
        lastFootstepTime = 0f;
    }

    private void PlayLootSearch()
    {
        if (Time.time - lastLootSearchTime < LOOT_SEARCH_INTERVAL) return;
        lastLootSearchTime = Time.time;

        lootSearchSource ??= CreateLoopSource("LootSearch_Source");
        lootSearchSource.pitch = Random.Range(0.95f, 1.05f);
        lootSearchSource.PlayOneShot(lootSearchClip);
    }

    private void StopLootSearch()
    {
        if (lootSearchSource != null && lootSearchSource.isPlaying)
            lootSearchSource.Stop();
        lastLootSearchTime = 0f;
    }

    /// <summary>创建循环/持续音效专用的 AudioSource</summary>
    private AudioSource CreateLoopSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = sfxVolume;
        return src;
    }

    // ================================================================
    //  BGM API
    // ================================================================

    /// <summary>立即播放 BGM（打断当前 BGM）</summary>
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null) return;
        bgmQueue.Clear();
        isPlayingBgmQueue = false;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>停止 BGM</summary>
    public void StopBgm()
    {
        bgmSource.Stop();
        bgmQueue.Clear();
        isPlayingBgmQueue = false;
    }

    /// <summary>队列播放 BGM：当前曲播完后自动播下一曲</summary>
    public void QueueBgm(params AudioClip[] clips)
    {
        foreach (var clip in clips)
        {
            if (clip != null)
                bgmQueue.Enqueue(clip);
        }

        // 如果当前没有在播 BGM，立即开始队列
        if (!bgmSource.isPlaying)
        {
            PlayNextBgm();
        }
        isPlayingBgmQueue = true;
    }

    /// <summary>播放撤离 BGM，播完后自动切列车 BGM</summary>
    public void PlayEvacThenTrainBgm()
    {
        StopBgm();
        QueueBgm(bgmEvacClip, bgmTrainClip);
    }

    /// <summary>开始战斗 BGM</summary>
    public void PlayBattleBgm()
    {
        PlayBgm(bgmBattleClip);
    }

    /// <summary>播放关卡完成音乐（不循环，播完后自动恢复战斗BGM或列车BGM）</summary>
    public void PlayLevelCompleteBgm()
    {
        StopBgm();
        // 关卡完成音乐播完 → 队列结束（由调用方决定后续播什么）
        bgmSource.loop = false;
        bgmSource.clip = bgmLevelCompleteClip;
        bgmSource.Play();
    }

    private void PlayNextBgm()
    {
        if (bgmQueue.Count == 0) return;
        var clip = bgmQueue.Dequeue();

        // 队列最后一个设为循环
        bgmSource.loop = (bgmQueue.Count == 0);
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // ================================================================
    //  SFX API
    // ================================================================

    /// <summary>播放音效（clip 为空则静默跳过）</summary>
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        var src = GetFreeSfxSource();
        if (src == null) return;
        src.PlayOneShot(clip);
    }

    /// <summary>带随机音调的音效（适合脚步声、搜刮等重复音效）</summary>
    public void PlaySfxWithPitch(AudioClip clip, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (clip == null) return;
        var src = GetFreeSfxSource();
        if (src == null) return;
        src.pitch = Random.Range(minPitch, maxPitch);
        src.PlayOneShot(clip);
    }

    /// <summary>在 3D 位置播放音效</summary>
    public void PlaySfxAt(AudioClip clip, Vector3 worldPos)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, worldPos, sfxVolume);
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (var s in sfxPool)
        {
            if (!s.isPlaying) return s;
        }
        return sfxPool[0]; // 全满时抢占最早的
    }

    // ================================================================
    //  音量控制
    // ================================================================

    /// <summary>设置 BGM 音量（0~1）</summary>
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    /// <summary>设置 SFX 音量（0~1）</summary>
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (var s in sfxPool)
            s.volume = sfxVolume;
        if (footstepSource != null)
            footstepSource.volume = sfxVolume;
        if (lootSearchSource != null)
            lootSearchSource.volume = sfxVolume;
    }
}
