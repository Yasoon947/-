using UnityEngine;

/// <summary>
/// 方块挤压拉伸动画器
/// 挂在玩家/AI 根物体上，动画的是子物体的 Sprite
/// 所有动画都用缩放/旋转实现，不影响 Collider
/// </summary>
public class BlockAnimator : MonoBehaviour
{
    [Header("动画目标")]
    [Tooltip("要动画的视觉子物体（SpriteRenderer 所在物体），不要和根物体相同")]
    public Transform spriteTransform;

    [Header("风格")]
    [Tooltip("AI 勾选，玩家不勾")]
    public bool isAI = false;

    [Header("Idle 呼吸")]
    public float idleBreathAmount = 0.025f;
    public float idleBreathSpeed = 2.5f;

    [Header("走路摇摆")]
    public float walkBobAmount = 0.1f;
    public float walkTiltAmount = 6f;
    public float walkFrequency = 8f;
    public float walkSpeedThreshold = 0.5f;

    [Header("跳跃拉伸")]
    public float jumpStretchY = 1.3f;
    public float jumpSquashX = 0.75f;
    public float fallStretchY = 1.1f;
    public float fallSquashX = 0.92f;

    [Header("落地压扁")]
    public float landSquashY = 0.65f;
    public float landStretchX = 1.3f;
    public float landRecoverTime = 0.18f;

    [Header("眩晕摇晃（AI 专用）")]
    public float stunTiltAmount = 25f;
    public float stunShakeFrequency = 12f;

    [Header("平滑速度")]
    public float smoothSpeed = 15f;

    private Rigidbody2D rb;
    private PlayerMove playerMove;
    private ChaserAI chaserAI;

    private Vector3 baseScale;
    private Vector3 currentScale;
    private float currentTiltZ;

    private bool wasGoingDown = false;
    private float landTimer = 0f;
    private float walkPhase = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMove = GetComponent<PlayerMove>();
        chaserAI = GetComponent<ChaserAI>();

        if (spriteTransform == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) spriteTransform = sr.transform;
            else
            {
                Debug.LogWarning($"[BlockAnimator] {name} 没找到 SpriteRenderer 子物体，动画将作用在自己身上");
                spriteTransform = transform;
            }
        }

        baseScale = spriteTransform.localScale;
        currentScale = baseScale;
    }

    private void Update()
    {
        if (spriteTransform == null) return;

        float dt = Time.deltaTime;

        bool isStunned = chaserAI != null && chaserAI.IsStunned;
        float vx = Mathf.Abs(rb.velocity.x);
        float vy = rb.velocity.y;

        bool goingUp = vy > 0.5f;
        bool goingDown = vy < -0.5f;
        bool isWalking = vx > walkSpeedThreshold && !goingUp && !goingDown;

        // 落地检测：从"下落"变"接近静止"的瞬间
        if (wasGoingDown && !goingDown && Mathf.Abs(vy) < 0.5f)
            landTimer = landRecoverTime;
        wasGoingDown = goingDown;

        Vector3 targetScale = baseScale;
        float targetTilt = 0f;

        if (isStunned)
        {
            // 眩晕：左右剧烈摇晃
            targetTilt = Mathf.Sin(Time.time * stunShakeFrequency * Mathf.PI * 2f) * stunTiltAmount;
        }
        else if (landTimer > 0f)
        {
            // 落地压扁（快速恢复）
            landTimer -= dt;
            float t = Mathf.Clamp01(landTimer / landRecoverTime);
            targetScale = new Vector3(
                baseScale.x * Mathf.Lerp(1f, landStretchX, t),
                baseScale.y * Mathf.Lerp(1f, landSquashY, t),
                baseScale.z);
        }
        else if (goingUp)
        {
            // 起跳：纵向拉伸 + 横向压缩
            targetScale = new Vector3(
                baseScale.x * jumpSquashX,
                baseScale.y * jumpStretchY,
                baseScale.z);
        }
        else if (goingDown)
        {
            // 下落：轻微拉伸
            targetScale = new Vector3(
                baseScale.x * fallSquashX,
                baseScale.y * fallStretchY,
                baseScale.z);
        }
        else if (isWalking)
        {
            // 走路：上下弹跳 + 摇摆
            walkPhase += dt * walkFrequency;
            float s = Mathf.Sin(walkPhase * Mathf.PI * 2f);
            targetScale = new Vector3(
                baseScale.x,
                baseScale.y + s * walkBobAmount,
                baseScale.z);
            targetTilt = s * walkTiltAmount;
        }
        else
        {
            // Idle：呼吸
            float breath = Mathf.Sin(Time.time * idleBreathSpeed) * idleBreathAmount;
            targetScale = new Vector3(
                baseScale.x,
                baseScale.y + breath,
                baseScale.z);
        }

        currentScale = Vector3.Lerp(currentScale, targetScale, dt * smoothSpeed);
        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTilt, dt * smoothSpeed);

        spriteTransform.localScale = currentScale;
        spriteTransform.localRotation = Quaternion.Euler(0f, 0f, currentTiltZ);
    }
}