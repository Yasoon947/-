using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 相机跟随
/// 挂在 Main Camera 上，把 Target 拖成你的方块
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("跟随参数")]
    public Vector3 offset = new Vector3(0f, 1f, 0f); // 相机相对目标的偏移
    public float smoothTime = 0.15f;                 // 平滑时间，越小跟得越紧
    public bool followX = true;                      // 是否跟随 X 轴
    public bool followY = true;                      // 是否跟随 Y 轴

    [Header("边界限制")]
    public bool useBounds = false;                   // 是否限制相机移动范围
    public Vector2 minBounds = new Vector2(-20f, -10f); // 相机中心最小 X/Y
    public Vector2 maxBounds = new Vector2(20f, 10f);   // 相机中心最大 X/Y

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        // 目标位置 + 偏移
        Vector3 desiredPosition = target.position + offset;

        // 如果不需要跟随某个轴，就保持相机当前坐标
        if (!followX) desiredPosition.x = transform.position.x;
        if (!followY) desiredPosition.y = transform.position.y;

        // 边界限制
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        // 2D 游戏中保持相机原本的 Z 轴位置，通常为 -10
        desiredPosition.z = transform.position.z;

        // 平滑移动到目标位置
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }
}