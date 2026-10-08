using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("倒计时")]
    public float totalTime = 60f;

    [Header("结束设置")]
    public bool pauseOnGameOver = true;
    public KeyCode restartKey = KeyCode.R;

    public static event Action<float> OnTimerChanged;
    public static event Action OnGameOver;

    private float timeRemaining;
    private bool isGameOver;

    public static GameManager Instance { get; private set; }

    public float TimeRemaining => timeRemaining;
    public float TimeProgress01 => totalTime > 0f ? timeRemaining / totalTime : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        timeRemaining = totalTime;
    }

    private void Update()
    {
        // 只在游戏中更新倒计时
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        if (isGameOver) return;

        timeRemaining -= Time.deltaTime;
        OnTimerChanged?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("游戏结束！倒计时归零。");

        GameStateManager.Instance?.EndGame();
        OnGameOver?.Invoke();
    }

    public void EndGame()
    {
        TriggerGameOver();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}