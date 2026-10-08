using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ChaserAI : MonoBehaviour
{
    [Header("目标")]
    public Transform target;

    [Header("速度曲线")]
    public float startSpeed = 3f;
    public float endSpeed = 8f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("移动手感")]
    public float accelSmoothTime = 0.15f;

    [Header("抓捕判定")]
    public float catchDistance = 1.2f;
    public float directChaseDistance = 2.5f;
    public float directChaseMaxDy = 1.5f;

    [Header("跳跃")]
    public float jumpForce = 14f;
    public float jumpHeightThreshold = 0.5f;
    public float jumpCooldown = 0.4f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("寻路")]
    public float repathInterval = 0.3f;
    public float waypointReachDistance = 1.2f;

    [Header("晃动 / 卡住检测")]
    public float wobbleCheckInterval = 0.4f;
    public int wobbleFlipThreshold = 3;
    public float stuckMoveThreshold = 0.3f;
    public float forcedDirDuration = 1.2f;
    public float edgeSearchStep = 0.5f;
    public float edgeSearchMax = 12f;

    [Header("调试")]
    public bool debugLog = false;

    private Rigidbody2D rb;
    private float velocityRefX;
    private bool gameOver;
    private bool isGrounded;
    private float stunTimer;
    private bool hasJumpedInAir;
    private float jumpCooldownTimer;

    private List<Vector2Int> currentPath;
    private float repathTimer;

    private float wobbleTimer;
    private Vector2 lastStuckCheckPos;
    private float lastMoveXSign;
    private int wobbleFlipCount;

    private float forcedDirTimer;
    private float forcedDir;

    private float debugTimer;

    public bool IsStunned => stunTimer > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastStuckCheckPos = rb.position;
        if (target == null)
        {
            var player = FindObjectOfType<PlayerMove>();
            if (player != null) target = player.transform;
        }
    }

    private void OnEnable() => GameManager.OnGameOver += HandleGameOver;
    private void OnDisable() => GameManager.OnGameOver -= HandleGameOver;

    private void HandleGameOver()
    {
        gameOver = true;
        rb.velocity = Vector2.zero;
    }

    private void Update()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;
        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;

        if (gameOver || target == null || IsStunned) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        if (Vector2.Distance(rb.position, target.position) <= catchDistance)
            TryCatch();
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryCatchFrom(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryCatchFrom(collision);

    private void TryCatchFrom(Collision2D collision)
    {
        if (gameOver || IsStunned) return;
        var player = collision.collider.GetComponent<PlayerMove>();
        if (player == null) return;
        TryCatch(player);
    }

    private void TryCatch(PlayerMove player = null)
    {
        if (gameOver) return;
        if (player == null && target != null)
            player = target.GetComponent<PlayerMove>();
        if (player == null || player.IsDead) return;

        player.OnCaught();

        if (!player.IsDead)
        {
            Vector2 knockDir = (rb.position - (Vector2)target.position).normalized;
            rb.velocity = knockDir * 8f;
            stunTimer = Mathf.Max(stunTimer, 1.2f);
        }
    }

    private void FixedUpdate()
    {
        if (gameOver || target == null) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        bool groundedNow = false;
        if (groundCheck != null)
        {
            var hits = Physics2D.OverlapCircleAll(
                groundCheck.position, groundCheckRadius, groundLayer);
            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody == rb) continue;
                groundedNow = true;
                break;
            }
        }
        if (rb.velocity.y > 0.01f) groundedNow = false;

        isGrounded = groundedNow;
        if (isGrounded) hasJumpedInAir = false;

        if (IsStunned)
        {
            float nvx = Mathf.MoveTowards(rb.velocity.x, 0f, Time.fixedDeltaTime * 20f);
            rb.velocity = new Vector2(nvx, rb.velocity.y);
            return;
        }

        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f || currentPath == null || currentPath.Count == 0)
        {
            repathTimer = repathInterval;
            if (GridPathfinder.Instance != null)
                currentPath = GridPathfinder.Instance.FindPathCells(
                    rb.position, target.position);
        }

        float moveX = 0f;
        float jumpTargetY = rb.position.y;
        bool wantJump = false;

        float distToPlayer = Vector2.Distance(rb.position, target.position);
        float dyToPlayer = target.position.y - rb.position.y;

        if (forcedDirTimer > 0f)
        {
            forcedDirTimer -= Time.fixedDeltaTime;
            moveX = forcedDir;
        }
        else if (distToPlayer < directChaseDistance
              && Mathf.Abs(dyToPlayer) < directChaseMaxDy)
        {
            float dx = target.position.x - rb.position.x;
            if (Mathf.Abs(dx) > 0.05f) moveX = Mathf.Sign(dx);
            if (dyToPlayer > jumpHeightThreshold)
            {
                jumpTargetY = target.position.y;
                wantJump = true;
            }
        }
        else if (currentPath != null && currentPath.Count > 0 && GridPathfinder.Instance != null)
        {
            int nearestIdx = 0;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < currentPath.Count; i++)
            {
                Vector2 wp = GridPathfinder.Instance.CellToWorld(
                    currentPath[i].x, currentPath[i].y);
                float d = Vector2.Distance(rb.position, wp);
                if (d < nearestDist) { nearestDist = d; nearestIdx = i; }
            }

            int targetIdx = nearestIdx + 1;
            while (targetIdx < currentPath.Count - 1)
            {
                Vector2 wp = GridPathfinder.Instance.CellToWorld(
                    currentPath[targetIdx].x, currentPath[targetIdx].y);
                if (Vector2.Distance(rb.position, wp) < waypointReachDistance)
                    targetIdx++;
                else break;
            }

            if (targetIdx < currentPath.Count)
            {
                Vector2 wp = GridPathfinder.Instance.CellToWorld(
                    currentPath[targetIdx].x, currentPath[targetIdx].y);
                float dx = wp.x - rb.position.x;
                if (Mathf.Abs(dx) > 0.1f) moveX = Mathf.Sign(dx);
            }
            else
            {
                float dx = target.position.x - rb.position.x;
                if (Mathf.Abs(dx) > 0.05f) moveX = Mathf.Sign(dx);
            }

            for (int i = targetIdx; i < currentPath.Count; i++)
            {
                Vector2 wp = GridPathfinder.Instance.CellToWorld(
                    currentPath[i].x, currentPath[i].y);
                if (wp.y - rb.position.y > jumpHeightThreshold)
                {
                    jumpTargetY = wp.y;
                    wantJump = true;
                    break;
                }
            }
        }
        else
        {
            float dx = target.position.x - rb.position.x;
            if (Mathf.Abs(dx) > 0.05f) moveX = Mathf.Sign(dx);
        }

        float currentSpeed = CalculateCurrentSpeed();
        float newVX = Mathf.SmoothDamp(
            rb.velocity.x, moveX * currentSpeed, ref velocityRefX, accelSmoothTime);
        rb.velocity = new Vector2(newVX, rb.velocity.y);

        UpdateMovementDiagnostics(moveX);

        if (wantJump && isGrounded && !hasJumpedInAir
            && jumpCooldownTimer <= 0f && forcedDirTimer <= 0f)
        {
            bool ceilingBlocked = false;
            float halfWidth = 0.4f;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 o = (Vector2)transform.position + new Vector2(i * halfWidth, 0f);
                if (Physics2D.Raycast(o, Vector2.up, 2.5f, groundLayer).collider != null)
                {
                    ceilingBlocked = true;
                    break;
                }
            }

            if (!ceilingBlocked)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                hasJumpedInAir = true;
                jumpCooldownTimer = jumpCooldown;

                if (debugLog)
                    Debug.Log($"[AI] 跳（固定跳力 {jumpForce}）");
            }
        }

        if (debugLog)
        {
            debugTimer -= Time.fixedDeltaTime;
            if (debugTimer <= 0f)
            {
                debugTimer = 0.5f;
                Debug.Log($"[AI] dist={distToPlayer:F2}, dy={dyToPlayer:F2}, " +
                          $"pathLen={currentPath?.Count ?? 0}, moveX={moveX:F2}, " +
                          $"wantJump={wantJump}, forced={forcedDirTimer:F2}, " +
                          $"flip={wobbleFlipCount}");
            }
        }
    }

    private void UpdateMovementDiagnostics(float currentMoveX)
    {
        if (forcedDirTimer > 0f)
        {
            wobbleTimer = 0f;
            wobbleFlipCount = 0;
            lastMoveXSign = 0f;
            lastStuckCheckPos = rb.position;
            return;
        }

        wobbleTimer += Time.fixedDeltaTime;

        float s = Mathf.Sign(currentMoveX);
        if (Mathf.Abs(currentMoveX) > 0.01f)
        {
            if (lastMoveXSign != 0f && s != lastMoveXSign)
                wobbleFlipCount++;
            lastMoveXSign = s;
        }

        if (wobbleTimer < wobbleCheckInterval) return;

        bool wobbling = wobbleFlipCount >= wobbleFlipThreshold;
        bool stuck = Vector2.Distance(rb.position, lastStuckCheckPos) < stuckMoveThreshold;

        if (wobbling || stuck)
        {
            float edgeDir = FindNearestOpenEdge();
            if (edgeDir == 0f)
                edgeDir = lastMoveXSign >= 0f ? -1f : 1f;

            forcedDir = edgeDir;
            forcedDirTimer = forcedDirDuration;

            if (debugLog)
                Debug.Log($"[AI] 触发 {(wobbling ? "晃动" : "卡住")} " +
                          $"(翻转={wobbleFlipCount}, 位移={Vector2.Distance(rb.position, lastStuckCheckPos):F2})，" +
                          $"强制朝 {(edgeDir > 0 ? "右" : "左")} 走 {forcedDirDuration}s");
        }

        wobbleTimer = 0f;
        wobbleFlipCount = 0;
        lastMoveXSign = 0f;
        lastStuckCheckPos = rb.position;
    }

    private float FindNearestOpenEdge()
    {
        for (float d = edgeSearchStep; d <= edgeSearchMax; d += edgeSearchStep)
        {
            Vector2 lp = (Vector2)transform.position + new Vector2(-d, 0f);
            bool leftGround = Physics2D.Raycast(lp, Vector2.down, 3f, groundLayer).collider != null;
            bool leftCeiling = Physics2D.Raycast(lp, Vector2.up, 3f, groundLayer).collider != null;
            if (leftGround && !leftCeiling) return -1f;
            if (!leftGround) break;

            Vector2 rp = (Vector2)transform.position + new Vector2(d, 0f);
            bool rightGround = Physics2D.Raycast(rp, Vector2.down, 3f, groundLayer).collider != null;
            bool rightCeiling = Physics2D.Raycast(rp, Vector2.up, 3f, groundLayer).collider != null;
            if (rightGround && !rightCeiling) return 1f;
            if (!rightGround) break;
        }
        return 0f;
    }

    private float CalculateCurrentSpeed()
    {
        float progress01 = 0f;
        if (GameManager.Instance != null)
            progress01 = 1f - Mathf.Clamp01(GameManager.Instance.TimeProgress01);

        float t = Mathf.Clamp01(speedCurve.Evaluate(progress01));
        return Mathf.Lerp(startSpeed, endSpeed, t);
    }

    public void ApplyStun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.stunSFX);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, directChaseDistance);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (currentPath != null && currentPath.Count > 0 && GridPathfinder.Instance != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Vector2 a = GridPathfinder.Instance.CellToWorld(currentPath[i].x, currentPath[i].y);
                Vector2 b = GridPathfinder.Instance.CellToWorld(currentPath[i + 1].x, currentPath[i + 1].y);
                Gizmos.DrawLine(a, b);
            }
        }
    }
}