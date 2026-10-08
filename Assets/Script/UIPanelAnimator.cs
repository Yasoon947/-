using System.Collections;
using UnityEngine;

/// <summary>
/// UI 面板动画器：淡入淡出 / 缩放 / 滑动
/// 挂在任意有 CanvasGroup 的 UI 物体上
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanelAnimator : MonoBehaviour
{
    public enum AnimType { None, Fade, Scale, SlideUp, SlideDown, SlideLeft, SlideRight }

    [Header("入场")]
    public AnimType showType = AnimType.Fade;
    public float showDuration = 0.3f;
    public float showDelay = 0f;

    [Header("退场")]
    public AnimType hideType = AnimType.Fade;
    public float hideDuration = 0.2f;

    [Header("缓动曲线")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("缩放动画的起始值")]
    public float scaleFrom = 0.7f;

    [Header("滑入/滑出的距离（像素）")]
    public float slideDistance = 200f;

    private CanvasGroup canvasGroup;
    private RectTransform rect;
    private Vector2 originalPos;
    private Vector3 originalScale;
    private Coroutine currentAnim;

    public bool IsAnimating => currentAnim != null;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            originalPos = rect.anchoredPosition;
            originalScale = rect.localScale == Vector3.zero ? Vector3.one : rect.localScale;
        }

        // ★ 如果 CanvasGroup 是 null，自动加一个（防止忘加）
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.LogWarning($"[UIPanelAnimator] {name} 上自动添加了 CanvasGroup");
        }
    }
    public void Show(bool instant = false)
    {
        StopAnim();
        if (instant) { SetFinalState(); return; }
        currentAnim = StartCoroutine(PlayAnim(showType, showDuration, showDelay, true));
    }

    public void Hide(bool instant = false)
    {
        StopAnim();
        if (instant) { SetHiddenState(); return; }
        currentAnim = StartCoroutine(PlayAnim(hideType, hideDuration, 0f, false));
    }

    private IEnumerator PlayAnim(AnimType type, float duration, float delay, bool showing)
    {
        // 初始状态
        SetStartState(type, showing);

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float eased = easeCurve.Evaluate(Mathf.Clamp01(t));
            ApplyState(type, showing, eased);
            yield return null;
        }

        if (showing) SetFinalState();
        else SetHiddenState();

        currentAnim = null;
    }

    private void SetStartState(AnimType type, bool showing)
    {
        switch (type)
        {
            case AnimType.Fade:
                canvasGroup.alpha = showing ? 0f : 1f;
                break;
            case AnimType.Scale:
                if (rect != null) rect.localScale = showing ? Vector3.one * scaleFrom : originalScale;
                canvasGroup.alpha = showing ? 0f : 1f;
                break;
            case AnimType.SlideUp:
            case AnimType.SlideDown:
            case AnimType.SlideLeft:
            case AnimType.SlideRight:
                canvasGroup.alpha = showing ? 0f : 1f;
                break;
        }
    }

    private void ApplyState(AnimType type, bool showing, float t)
    {
        float p = showing ? t : 1f - t;

        switch (type)
        {
            case AnimType.Fade:
                canvasGroup.alpha = p;
                break;

            case AnimType.Scale:
                if (rect != null)
                    rect.localScale = Vector3.Lerp(Vector3.one * scaleFrom, originalScale, p);
                canvasGroup.alpha = p;
                break;

            case AnimType.SlideUp:
            case AnimType.SlideDown:
            case AnimType.SlideLeft:
            case AnimType.SlideRight:
                if (rect != null)
                {
                    Vector2 offset = Vector2.zero;
                    switch (type)
                    {
                        case AnimType.SlideUp: offset = new Vector2(0, -slideDistance); break;
                        case AnimType.SlideDown: offset = new Vector2(0, slideDistance); break;
                        case AnimType.SlideLeft: offset = new Vector2(slideDistance, 0); break;
                        case AnimType.SlideRight: offset = new Vector2(-slideDistance, 0); break;
                    }
                    rect.anchoredPosition = Vector2.Lerp(originalPos + offset, originalPos, p);
                }
                canvasGroup.alpha = p;
                break;
        }
    }

    private void SetFinalState()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        if (rect != null)
        {
            rect.anchoredPosition = originalPos;
            rect.localScale = originalScale;
        }
    }

    private void SetHiddenState()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void StopAnim()
    {
        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            currentAnim = null;
        }
    }
}