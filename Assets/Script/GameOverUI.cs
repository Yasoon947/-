using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelRoot;
    public UIPanelAnimator panelAnimator;

    [Header("文本")]
    public Text titleText;
    public Text reasonText;
    public Text survivedText;

    [Header("按钮")]
    public Button restartButton;
    public Button menuButton;

    [Header("动画")]
    public float showDelay = 0.6f;

    [Header("颜色")]
    public Color victoryColor = new Color(0.4f, 1f, 0.6f);
    public Color defeatColor = new Color(1f, 0.4f, 0.4f);

    private float gameStartTime;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (panelAnimator == null) panelAnimator = GetComponent<UIPanelAnimator>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        restartButton?.onClick.AddListener(OnRestartClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);

        // ★ 不 SetActive(false)，只用 CanvasGroup 隐藏
        if (panelAnimator != null)
            panelAnimator.Hide(instant: true);
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        GameStateManager.OnStateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                gameStartTime = Time.unscaledTime;
                panelAnimator?.Hide(instant: true);
                break;

            case GameState.GameOver:
                // ★ 直接启动协程，物体始终 active
                StartCoroutine(ShowGameOverRoutine());
                break;

            case GameState.Menu:
                panelAnimator?.Hide(instant: true);
                break;
        }
    }

    private IEnumerator ShowGameOverRoutine()
    {
        bool isVictory = CheckIsVictory();

        if (AudioManager.Instance != null)
        {
            if (isVictory) AudioManager.Instance.PlaySFX(AudioManager.Instance.victorySFX);
            else AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverSFX);
        }

        yield return new WaitForSecondsRealtime(showDelay);

        float survived = Time.unscaledTime - gameStartTime;

        if (titleText != null)
        {
            titleText.text = isVictory ? "胜利！" : "游戏结束";
            titleText.color = isVictory ? victoryColor : defeatColor;
        }

        if (reasonText != null)
            reasonText.text = isVictory ? "时间到，你活下来了！" : "被追上了...";

        if (survivedText != null)
            survivedText.text = $"存活 {survived:F1} 秒";

        panelAnimator?.Show();

        AudioManager.Instance?.PlayBGM(AudioManager.Instance.gameOverBGM);

        // 恢复 timeScale 让按钮响应
        Time.timeScale = 1f;
    }

    private bool CheckIsVictory()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            return gm.TimeRemaining <= 0.01f;
        return false;
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        panelAnimator?.Hide();
        yield return new WaitForSecondsRealtime(0.25f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMenuClicked()
    {
        AudioManager.Instance?.PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}