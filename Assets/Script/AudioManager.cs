using UnityEngine;

/// <summary>
/// 音频管理器（单例）
/// 挂在场景里的空物体上，需要两个 AudioSource 子物体
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音源（自动查找）")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("音量")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("BGM 音轨")]
    public AudioClip mainMenuBGM;
    public AudioClip gameBGM;
    public AudioClip gameOverBGM;

    [Header("SFX 音效")]
    public AudioClip clickSFX;
    public AudioClip jumpSFX;
    public AudioClip pickupSFX;
    public AudioClip stunSFX;
    public AudioClip shieldSFX;
    public AudioClip doubleJumpSFX;
    public AudioClip caughtSFX;
    public AudioClip countdownTickSFX;   // 最后 10 秒每秒滴答
    public AudioClip gameOverSFX;
    public AudioClip victorySFX;         // 时间到、玩家胜利

    // 便捷访问
    public AudioClip ClickSFX => clickSFX;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---------------- BGM ----------------
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume * masterVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // ---------------- SFX ----------------
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
    }

    public void PlayClick() => PlaySFX(clickSFX);
    public void PlayJump() => PlaySFX(jumpSFX);
    public void PlayPickup() => PlaySFX(pickupSFX);

    // ---------------- 音量 ----------------
    public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); UpdateVolumes(); }
    public void SetBGMVolume(float v) { bgmVolume = Mathf.Clamp01(v); UpdateVolumes(); }
    public void SetSFXVolume(float v) { sfxVolume = Mathf.Clamp01(v); }

    private void UpdateVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = bgmVolume * masterVolume;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}