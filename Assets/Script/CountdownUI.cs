using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把 GameManager 的倒计时显示到 UI Text 上
/// 挂在一个 Text / TMP_Text 上
/// </summary>
public class CountdownUI : MonoBehaviour
{
    [SerializeField] private Text timerText;    // 如果用的是 TextMeshPro，改成 TMP_Text

    private void OnEnable()
    {
        GameManager.OnTimerChanged += UpdateTimer;
        GameManager.OnGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameManager.OnTimerChanged -= UpdateTimer;
        GameManager.OnGameOver -= OnGameOver;
    }

    private void UpdateTimer(float seconds)
    {
        if (timerText == null) return;

        if (seconds < 0f) seconds = 0f;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{m:00}:{s:00}";

        // 最后 10 秒变红
        timerText.color = seconds <= 10f ? Color.red : Color.white;
    }

    private void OnGameOver()
    {
        if (timerText != null)
            timerText.text = "时间到！按 R 重开";
    }
}