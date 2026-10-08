using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 8f;
    public float jumpForce = 13f;

    [Header("跳跃次数")]
    public int baseMaxJumpCount = 1;

    [Header("跳跃手感")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("地面判定")]
    public float groundNormalThreshold = 0.5f;

    private int extraJumps = 0;
    private float doubleJumpTimer = 0f;
    private bool hasShield = false;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool groundContactThisStep;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private int jumpsLeft;
    private bool isDead;

    private int CurrentMaxJumps => baseMaxJumpCount + extraJumps;

    public float DoubleJumpTimeRemaining => doubleJumpTimer;
    public bool HasShield => hasShield;
    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = CurrentMaxJumps;
    }

    private void Update()
    {
        if (isDead) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        moveInput = Input.GetAxisRaw("Horizontal");

        bool jumpPressed = Input.GetKeyDown(KeyCode.Space)
                        || Input.GetKeyDown(KeyCode.W)
                        || Input.GetKeyDown(KeyCode.UpArrow);
        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        bool jumpReleased = Input.GetKeyUp(KeyCode.Space)
                         || Input.GetKeyUp(KeyCode.W)
                         || Input.GetKeyUp(KeyCode.UpArrow);
        if (jumpReleased && rb.velocity.y > 0f)
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);

        if (doubleJumpTimer > 0f)
        {
            doubleJumpTimer -= Time.deltaTime;
            if (doubleJumpTimer <= 0f)
            {
                doubleJumpTimer = 0f;
                extraJumps = 0;
                if (jumpsLeft > CurrentMaxJumps) jumpsLeft = CurrentMaxJumps;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        if (rb.velocity.y > 0.01f)
        {
            isGrounded = false;
            groundContactThisStep = false;
        }
        else
        {
            isGrounded = groundContactThisStep;
            groundContactThisStep = false;
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            jumpsLeft = CurrentMaxJumps;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }

        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        bool canJump = false;
        if (jumpsLeft > 0)
        {
            if (jumpsLeft == CurrentMaxJumps)
                canJump = isGrounded || coyoteTimer > 0f;
            else
                canJump = true;
        }

        if (jumpBufferTimer > 0f && canJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpsLeft--;
            isGrounded = false;
            groundContactThisStep = false;

            // 音效
            AudioManager.Instance?.PlayJump();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > groundNormalThreshold)
            {
                groundContactThisStep = true;
                return;
            }
        }
    }

    // ===================== 道具接口 =====================

    public void ActivateDoubleJump(float duration)
    {
        if (doubleJumpTimer <= 0f) extraJumps = 1;
        doubleJumpTimer = duration;

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.doubleJumpSFX);
        Debug.Log($"[Player] 二段跳激活 {duration} 秒");
    }

    public void ActivateShield()
    {
        hasShield = true;
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.shieldSFX);
        Debug.Log("[Player] 获得护盾");
    }

    public bool ConsumeShield()
    {
        if (!hasShield) return false;
        hasShield = false;
        Debug.Log("[Player] 护盾被消耗");
        return true;
    }

    public void OnCaught()
    {
        if (isDead) return;

        if (ConsumeShield())
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.caughtSFX, 0.7f);
            return;
        }

        isDead = true;
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.caughtSFX);
        Debug.Log("[Player] 被抓住，游戏结束");

        if (GameManager.Instance != null)
            GameManager.Instance.EndGame();
        else
            GameStateManager.Instance?.EndGame();
    }
}