using UnityEngine;
using UnityEngine.UI;

public class StatusHUD : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Text statusText;

    private PlayerMove player;
    private Color originalTimerColor;
    private int lastTickSecond = -1;

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponentInChildren<Text>();

        if (timerText == null)
            Debug.LogError("[HUD] timerText 没赋值！请拖入一个 UI Text。");
        else
            originalTimerColor = timerText.color;
    }

    private void Start()
    {
        player = FindObjectOfType<PlayerMove>();
    }

    private void Update()
    {
        // 非游戏状态时清空 HUD
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            if (timerText != null) timerText.text = "";
            if (statusText != null) statusText.text = "";
            return;
        }

        // ---------- 倒计时 ----------
        if (timerText != null && GameManager.Instance != null)
        {
            float seconds = GameManager.Instance.TimeRemaining;
            if (seconds < 0f) seconds = 0f;

            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"{m:00}:{s:00}";
            timerText.color = seconds <= 10f ? Color.red : originalTimerColor;

            // 最后 10 秒每秒滴答
            int secInt = Mathf.CeilToInt(seconds);
            if (seconds <= 10f && secInt != lastTickSecond)
            {
                lastTickSecond = secInt;
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.countdownTickSFX, 0.6f);
            }
        }

        // ---------- 玩家状态 ----------
        if (statusText != null && player != null)
        {
            string s = "";
            if (player.DoubleJumpTimeRemaining > 0f)
                s += $"二段跳: {player.DoubleJumpTimeRemaining:F1}s\n";
            if (player.HasShield)
                s += "护盾: 有\n";
            statusText.text = s;
        }
    }
}