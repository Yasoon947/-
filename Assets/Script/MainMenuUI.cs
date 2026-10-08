using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("引用")]
    public GameObject panelRoot;
    public UIPanelAnimator panelAnimator;

    [Header("按钮")]
    public Button startButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("设置面板")]
    public GameObject settingsPanel;

    [Header("动画")]
    public float transitionDelay = 0.3f;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (panelAnimator == null) panelAnimator = GetComponent<UIPanelAnimator>();
    }

    private void Start()
    {
        startButton?.onClick.AddListener(OnStartClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);

        // 主菜单默认显示
        panelAnimator?.Show();

        // 关闭设置面板
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioManager.Instance.mainMenuBGM);

        GameStateManager.OnStateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Menu)
        {
            panelAnimator?.Show();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
    }

    private void OnStartClicked()
    {
        AudioManager.Instance?.PlayClick();
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        panelAnimator?.Hide();
        yield return new WaitForSecondsRealtime(transitionDelay);

        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioManager.Instance.gameBGM);

        GameStateManager.Instance?.StartGame();
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayClick();

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
        AudioManager.Instance?.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}