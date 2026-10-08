using UnityEngine;

/// <summary>
/// 设置管理器：音量、存档
/// 挂在场景空物体上（比如叫 SettingsManager）
/// 由 AudioManager 自动使用
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KEY_MASTER = "vol_master";
    private const string KEY_BGM = "vol_bgm";
    private const string KEY_SFX = "vol_sfx";

    public float MasterVolume { get; private set; } = 1f;
    public float BGMVolume { get; private set; } = 0.6f;
    public float SFXVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Load();
        ApplyToAudioManager();
    }

    public void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        BGMVolume = PlayerPrefs.GetFloat(KEY_BGM, 0.6f);
        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, MasterVolume);
        PlayerPrefs.SetFloat(KEY_BGM, BGMVolume);
        PlayerPrefs.SetFloat(KEY_SFX, SFXVolume);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float v)
    {
        MasterVolume = Mathf.Clamp01(v);
        ApplyToAudioManager();
        Save();
    }

    public void SetBGMVolume(float v)
    {
        BGMVolume = Mathf.Clamp01(v);
        ApplyToAudioManager();
        Save();
    }

    public void SetSFXVolume(float v)
    {
        SFXVolume = Mathf.Clamp01(v);
        ApplyToAudioManager();
        Save();
    }

    private void ApplyToAudioManager()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMasterVolume(MasterVolume);
        AudioManager.Instance.SetBGMVolume(BGMVolume);
        AudioManager.Instance.SetSFXVolume(SFXVolume);
    }
}