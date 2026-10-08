using System;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, GameOver }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public static event Action<GameState> OnStateChanged;

    [Header("³õÊ¼×´Ì¬")]
    public GameState initialState = GameState.Menu;

    [Header("ÔÝÍ£ÉèÖÃ")]
    public bool pauseWithTimeScale = true;
    public KeyCode pauseKey = KeyCode.Escape;

    public GameState CurrentState { get; private set; }
    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsGameOver => CurrentState == GameState.GameOver;
    public bool IsPaused => CurrentState == GameState.Paused;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentState = initialState;
        ApplyState(initialState);
    }

    private void Update()
    {
        // Esc ¼üÇÐ»»ÔÝÍ£
        if (Input.GetKeyDown(pauseKey))
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        ApplyState(newState);
        OnStateChanged?.Invoke(newState);
    }

    private void ApplyState(GameState state)
    {
        if (pauseWithTimeScale)
            Time.timeScale = (state == GameState.Playing) ? 1f : 0f;
    }

    public void StartGame() => SetState(GameState.Playing);
    public void PauseGame() => SetState(GameState.Paused);
    public void ResumeGame() => SetState(GameState.Playing);
    public void EndGame() => SetState(GameState.GameOver);
    public void ReturnToMenu() => SetState(GameState.Menu);

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}