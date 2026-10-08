using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelRoot;
    public UIPanelAnimator panelAnimator;

    [Header("按钮")]
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button menuButton;

    [Header("设置面板")]
    public GameObject settingsPanel;

    [Header("动画")]
    public float transitionDelay = 0.2f;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (panelAnimator == null) panelAnimator = GetComponent<UIPanelAnimator>();
    }

    private void Start()
    {
        resumeButton?.onClick.AddListener(OnResumeClicked);
        restartButton?.onClick.AddListener(OnRestartClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);

        // 默认隐藏
        panelAnimator?.Hide(instant: true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        GameStateManager.OnStateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Paused)
        {
            panelAnimator?.Show();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
        else
        {
            panelAnimator?.Hide();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
    }

    private void OnResumeClicked()
    {
        AudioManager.Instance?.PlayClick();
        StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        panelAnimator?.Hide();
        yield return new WaitForSecondsRealtime(transitionDelay);
        GameStateManager.Instance?.ResumeGame();
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        panelAnimator?.Hide();
        yield return new WaitForSecondsRealtime(transitionDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayClick();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void OnMenuClicked()
    {
        AudioManager.Instance?.PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}