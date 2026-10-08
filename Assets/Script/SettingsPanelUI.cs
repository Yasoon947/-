using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板 UI：音量滑块
/// 主菜单和暂停菜单都可以复用这个面板
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelRoot;

    [Header("滑块")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("文本（可选）")]
    public Text masterValueText;
    public Text bgmValueText;
    public Text sfxValueText;

    [Header("按钮")]
    public Button closeButton;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
    }

    private void Start()
    {
        var sm = SettingsManager.Instance;
        if (sm == null)
        {
            Debug.LogError("[Settings] 场景里没有 SettingsManager！");
            return;
        }

        // 初始化滑块值
        if (masterSlider != null) masterSlider.value = sm.MasterVolume;
        if (bgmSlider != null) bgmSlider.value = sm.BGMVolume;
        if (sfxSlider != null) sfxSlider.value = sm.SFXVolume;

        UpdateValueText();

        // 绑定事件
        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider?.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        closeButton?.onClick.AddListener(OnCloseClicked);
    }

    private void OnMasterChanged(float v)
    {
        SettingsManager.Instance?.SetMasterVolume(v);
        UpdateValueText();
    }

    private void OnBGMChanged(float v)
    {
        SettingsManager.Instance?.SetBGMVolume(v);
        UpdateValueText();
    }

    private void OnSFXChanged(float v)
    {
        SettingsManager.Instance?.SetSFXVolume(v);
        UpdateValueText();

        // 拖动时播放一下点击音，让玩家听到效果
        AudioManager.Instance?.PlayClick();
    }

    private void UpdateValueText()
    {
        if (masterValueText != null && masterSlider != null)
            masterValueText.text = $"{Mathf.RoundToInt(masterSlider.value * 100)}%";
        if (bgmValueText != null && bgmSlider != null)
            bgmValueText.text = $"{Mathf.RoundToInt(bgmSlider.value * 100)}%";
        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100)}%";
    }

    private void OnCloseClicked()
    {
        AudioManager.Instance?.PlayClick();
        panelRoot.SetActive(false);
    }
}