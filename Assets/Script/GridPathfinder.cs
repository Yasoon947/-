using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder : MonoBehaviour
{
    public static GridPathfinder Instance { get; private set; }

    [Header("网格范围（世界坐标，覆盖整个可玩区）")]
    public Vector2 worldMin = new Vector2(-20f, -1f);
    public Vector2 worldMax = new Vector2(20f, 20f);
    public float cellSize = 1.5f;   // ★ 建议 1.5，路径更直

    [Header("障碍层（地面、平台、墙）")]
    public LayerMask obstacleLayer;

    [Header("检测盒缩放（0.8~0.9 避免边缘误判）")]
    [Range(0.5f, 1f)] public float checkBoxScale = 0.85f;

    [Header("跳跃能力（格子数）")]
    public int maxJumpUp = 8;            // ★ 从 5 加大到 8
    public int maxJumpDown = 15;
    public int maxJumpHorizontal = 5;    // ★ 从 3 加大到 5

    [Header("可视化")]
    public bool drawGizmos = true;

    private int width;
    private int height;
    private bool[,] blocked;
    private bool[,] standable;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildGrid();
    }

    public void BuildGrid()
    {
        width = Mathf.CeilToInt((worldMax.x - worldMin.x) / cellSize);
        height = Mathf.CeilToInt((worldMax.y - worldMin.y) / cellSize);
        blocked = new bool[width, height];
        standable = new bool[width, height];

        Vector2 boxSize = new Vector2(cellSize * checkBoxScale, cellSize * checkBoxScale);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                blocked[x, y] = Physics2D.OverlapBox(
                    CellToWorld(x, y), boxSize, 0f, obstacleLayer) != null;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (!blocked[x, y] && (y == 0 || blocked[x, y - 1]))
                    standable[x, y] = true;
    }

    public Vector2 CellToWorld(int x, int y) =>
        new Vector2(worldMin.x + (x + 0.5f) * cellSize,
                    worldMin.y + (y + 0.5f) * cellSize);

    public Vector2Int WorldToCell(Vector2 pos) =>
        new Vector2Int(
            Mathf.FloorToInt((pos.x - worldMin.x) / cellSize),
            Mathf.FloorToInt((pos.y - worldMin.y) / cellSize));

    public bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
    public bool IsBlocked(int x, int y) => InBounds(x, y) && blocked[x, y];
    public bool IsStandable(int x, int y) => InBounds(x, y) && standable[x, y];

    public List<Vector2Int> FindPathCells(Vector2 startWorld, Vector2 goalWorld)
    {
        Vector2Int start = WorldToCell(startWorld);
        Vector2Int goal = WorldToCell(goalWorld);

        start = FindNearestStandable(start);
        goal = FindNearestStandable(goal);
        if (start.x < 0 || goal.x < 0) return null;

        var openSet = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [start] = 0f };
        var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };

        int safety = 8000;

        while (openSet.Count > 0 && safety-- > 0)
        {
            int bestIdx = 0;
            for (int i = 1; i < openSet.Count; i++)
                if (fScore[openSet[i]] < fScore[openSet[bestIdx]]) bestIdx = i;

            Vector2Int current = openSet[bestIdx];
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.RemoveAt(bestIdx);

            foreach (var neighbor in GetNeighbors(current))
            {
                float stepCost = GetStepCost(current, neighbor);
                float tentative = gScore[current] + stepCost;
                if (!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    fScore[neighbor] = tentative + Heuristic(neighbor, goal);
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return null;
    }

    /// <summary>不同移动方式的代价（跳跃比走路略贵，但不至于让 AI 绕远）</summary>
    private float GetStepCost(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(b.x - a.x);
        int dy = b.y - a.y;

        // 水平/斜向移动：正常代价
        if (dx <= 1 && Mathf.Abs(dy) <= 1)
            return Vector2.Distance(a, b);

        // 跳跃：稍微贵一点点，鼓励 AI 能走就不跳
        return Vector2.Distance(a, b) * 1.1f;
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        // 1. 相邻 8 方向
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var n = new Vector2Int(cell.x + dx, cell.y + dy);
                if (IsStandable(n.x, n.y) && IsStraightClear(cell, n))
                    yield return n;
            }

        // 2. 向上跳跃
        for (int dx = -maxJumpHorizontal; dx <= maxJumpHorizontal; dx++)
        {
            // 允许 dx == 0 的垂直跳跃
            for (int dy = 2; dy <= maxJumpUp; dy++)
            {
                var n = new Vector2Int(cell.x + dx, cell.y + dy);
                if (!IsStandable(n.x, n.y)) continue;
                if (!IsJumpClear(cell, n)) continue;
                yield return n;
                break;
            }

            for (int dy = 2; dy <= maxJumpUp; dy++)
            {
                var n = new Vector2Int(cell.x + dx, cell.y + dy);
                if (!IsStandable(n.x, n.y)) continue;
                if (!IsJumpClear(cell, n)) continue;
                yield return n;
                break;   // 每个水平方向只取最近落点
            }
        }

        // 3. 下落
        for (int dx = -maxJumpHorizontal; dx <= maxJumpHorizontal; dx++)
        {
            for (int dy = -2; dy >= -maxJumpDown; dy--)
            {
                var n = new Vector2Int(cell.x + dx, cell.y + dy);
                if (!IsStandable(n.x, n.y)) continue;
                if (!IsFallClear(cell, n)) continue;
                yield return n;
                break;
            }
        }
    }

    private bool IsStraightClear(Vector2Int a, Vector2Int b)
    {
        int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t));
            if (IsBlocked(x, y)) return false;
        }
        return true;
    }

    /// <summary>
    /// ★ 关键修复：跳跃检测只看起跳空间和落点，中间不管
    /// 抛物线是绕的，不是"垂直起跳再平移"，中间格子被堵不影响跳跃
    /// </summary>
    private bool IsJumpClear(Vector2Int from, Vector2Int to)
    {
        // 起跳空间：上方 1~2 格不能有障碍（防止顶头）
        if (IsBlocked(from.x, from.y + 1)) return false;
        if (IsBlocked(from.x, from.y + 2)) return false;

        // 落点本体和头顶必须为空
        if (IsBlocked(to.x, to.y)) return false;
        if (IsBlocked(to.x, to.y + 1)) return false;

        // 落点下方要有支撑（可站立格本身已经保证了这点）
        // 中间不管 → 允许跳过任何中间障碍
        return true;
    }

    private bool IsFallClear(Vector2Int from, Vector2Int to)
    {
        // 下落路径：从起跳点正下方开始，检查下落列是否通畅
        for (int y = from.y - 1; y >= to.y; y--)
            if (IsBlocked(to.x, y)) return false;
        return true;
    }

    private Vector2Int FindNearestStandable(Vector2Int cell)
    {
        if (IsStandable(cell.x, cell.y)) return cell;
        for (int r = 1; r < 20; r++)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    int nx = cell.x + dx, ny = cell.y + dy;
                    if (IsStandable(nx, ny)) return new Vector2Int(nx, ny);
                }
        }
        return new Vector2Int(-1, -1);
    }

    /// <summary>
    /// ★ 启发式：曼哈顿距离 × 加权
    /// 比欧氏距离更"激进"，路径更直，不会偏斜绕路
    /// 加权 1.2 略大于 1，A* 会更快逼近目标
    /// </summary>
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) * 1.2f;
    }

    private List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || blocked == null) return;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector2 c = CellToWorld(x, y);
                Vector3 size = Vector3.one * cellSize * 0.9f;

                if (blocked[x, y])
                {
                    Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
                    Gizmos.DrawCube(c, size);
                }
                else if (standable[x, y])
                {
                    Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.35f);
                    Gizmos.DrawCube(c, size);
                }
                else
                {
                    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f);
                    Gizmos.DrawWireCube(c, size);
                }
            }
    }
}