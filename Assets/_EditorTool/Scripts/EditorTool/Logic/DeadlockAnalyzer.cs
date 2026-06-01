using System.Collections.Generic;
using System.Text;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Logic
{
    // =========================================================================
    //  Public result types
    // =========================================================================

    /// <summary>Kết quả phân tích deadlock cho toàn bộ map.</summary>
    public class DeadlockAnalysisResult
    {
        /// <summary>Map có thể giải được không?</summary>
        public bool IsSolvable;

        /// <summary>Danh sách nhóm mũi tên chặn nhau vòng tròn.</summary>
        public List<DeadlockGroup> DeadlockGroups = new List<DeadlockGroup>();

        /// <summary>Mũi tên có thể thoát ngay từ trạng thái ban đầu (không bị ai chặn).</summary>
        public List<string> InitiallyFreeArrows = new List<string>();

        /// <summary>Thông điệp tổng hợp dành cho hiển thị UI.</summary>
        public string Summary;
    }

    /// <summary>
    /// Một nhóm mũi tên bị khoá lẫn nhau theo vòng tròn:
    /// không mũi tên nào trong nhóm có thể thoát ra trước khi ít nhất
    /// một mũi tên trong nhóm bị loại bỏ bằng cách khác.
    /// </summary>
    public class DeadlockGroup
    {
        /// <summary>ID của các mũi tên trong vòng khoá.</summary>
        public List<string> ArrowIds = new List<string>();

        /// <summary>Mô tả chuỗi khoá: A chặn B, B chặn C... C chặn A.</summary>
        public string BlockChainDescription;

        /// <summary>Nhóm này có khả năng giải được sau khi tháo mũi tên khác không?</summary>
        public bool IsResolvable;
    }

    // =========================================================================
    //  DeadlockAnalyzer — public static entry point
    // =========================================================================

    /// <summary>
    /// Phân tích trạng thái bảng để phát hiện "level chết":
    /// trạng thái mà không có bất kỳ thứ tự thao tác nào giải phóng được tất cả mũi tên.
    ///
    /// Thuật toán (simulation-based):
    ///   1. Xây SimGrid — lưới mô phỏng nhẹ, không cần MonoBehaviour.
    ///   2. Lặp: tìm mũi tên có thể thoát → loại → cập nhật → lặp tiếp.
    ///   3. Nếu còn mũi tên sau khi không tiến thêm được → deadlock.
    ///   4. Xây đồ thị chặn, tìm cycle bằng DFS → tạo DeadlockGroup.
    /// </summary>
    public static class DeadlockAnalyzer
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────

        public static DeadlockAnalysisResult Analyze(GridSystem editorGrid)
        {
            DeadlockAnalysisResult result = new DeadlockAnalysisResult();

            List<ArrowSaveData> arrows = editorGrid.GetSaveData();
            if (arrows == null || arrows.Count == 0)
            {
                result.IsSolvable = true;
                result.Summary = "Bản đồ trống — không có gì để kiểm tra.";
                return result;
            }

            // ── Xây lưới mô phỏng ────────────────────────────────────────────
            SimGrid sim = new SimGrid(editorGrid.Width, editorGrid.Height);
            foreach (ArrowSaveData arrow in arrows)
            {
                if (arrow?.Path == null || arrow.Path.Count < 1) continue;
                sim.PlaceArrow(arrow);
            }

            bool hasSpecialCells = editorGrid.GetSpecialSaveData()?.Count > 0;

            // ── Mô phỏng giải level (greedy simulation) ───────────────────────
            HashSet<string> remaining = new HashSet<string>(sim.GetAllArrowIds());
            List<string> escapedOrder = new List<string>();

            bool progress = true;
            while (progress && remaining.Count > 0)
            {
                progress = false;
                List<string> canEscape = new List<string>();

                foreach (string id in remaining)
                {
                    if (sim.CanEscape(id, remaining)) canEscape.Add(id);
                }

                foreach (string id in canEscape)
                {
                    // Ghi lại mũi tên thoát đầu tiên (chưa bị chặn lần nào)
                    if (!sim.WasBlockedAtLeastOnce(id))
                        result.InitiallyFreeArrows.Add(id);

                    remaining.Remove(id);
                    escapedOrder.Add(id);
                    sim.RemoveArrow(id);
                    progress = true;
                }
            }

            // ── Kết quả: giải được ───────────────────────────────────────────
            if (remaining.Count == 0)
            {
                result.IsSolvable = true;
                result.Summary = BuildSolvableSummary(escapedOrder, hasSpecialCells);
                return result;
            }

            // ── Kết quả: deadlock ────────────────────────────────────────────
            Dictionary<string, string> blockGraph = BuildBlockGraph(remaining, sim);
            List<DeadlockGroup> cycles = FindCycles(remaining, blockGraph);

            result.IsSolvable = false;
            result.DeadlockGroups = cycles;
            result.Summary = BuildDeadlockSummary(remaining, cycles, hasSpecialCells);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static Dictionary<string, string> BuildBlockGraph(HashSet<string> remaining, SimGrid sim)
        {
            Dictionary<string, string> graph = new Dictionary<string, string>();
            foreach (string id in remaining)
            {
                string blocker = sim.GetBlockerId(id, remaining);
                if (!string.IsNullOrEmpty(blocker) && remaining.Contains(blocker))
                    graph[id] = blocker;
            }
            return graph;
        }

        private static List<DeadlockGroup> FindCycles(HashSet<string> remaining,
            Dictionary<string, string> blockGraph)
        {
            List<DeadlockGroup> groups = new List<DeadlockGroup>();
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> inCycle = new HashSet<string>();

            foreach (string start in remaining)
            {
                if (visited.Contains(start)) continue;

                List<string> path = new List<string>();
                HashSet<string> onPath = new HashSet<string>();
                string current = start;

                while (current != null && !visited.Contains(current))
                {
                    if (onPath.Contains(current))
                    {
                        int cycleStart = path.IndexOf(current);
                        if (cycleStart >= 0)
                        {
                            List<string> cycleIds = path.GetRange(cycleStart, path.Count - cycleStart);

                            bool alreadyFound = false;
                            foreach (string cid in cycleIds)
                            {
                                if (inCycle.Contains(cid)) { alreadyFound = true; break; }
                            }

                            if (!alreadyFound)
                            {
                                foreach (string cid in cycleIds) inCycle.Add(cid);
                                groups.Add(new DeadlockGroup
                                {
                                    ArrowIds = new List<string>(cycleIds),
                                    BlockChainDescription = BuildChainDescription(cycleIds, blockGraph),
                                    IsResolvable = false
                                });
                            }
                        }
                        break;
                    }

                    path.Add(current);
                    onPath.Add(current);
                    blockGraph.TryGetValue(current, out string next);
                    current = next;
                }

                foreach (string p in path) visited.Add(p);
            }

            // Fallback: nếu không tìm được cycle rõ ràng → gom tất cả remaining
            if (groups.Count == 0 && remaining.Count > 0)
            {
                groups.Add(new DeadlockGroup
                {
                    ArrowIds = new List<string>(remaining),
                    BlockChainDescription = "Các mũi tên chặn nhau theo cách phức tạp (không có vòng tuyến tính rõ ràng).",
                    IsResolvable = false
                });
            }

            return groups;
        }

        private static string BuildChainDescription(List<string> cycle, Dictionary<string, string> blockGraph)
        {
            if (cycle == null || cycle.Count == 0) return string.Empty;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < cycle.Count; i++)
            {
                string cur = cycle[i];
                string nxt = cycle[(i + 1) % cycle.Count];
                sb.Append($"[{cur}] bị [{nxt}] chặn");
                if (i < cycle.Count - 1) sb.Append(" → ");
            }
            sb.Append(" → (lặp)");
            return sb.ToString();
        }

        private static string BuildSolvableSummary(List<string> order, bool hasSpecialCells)
        {
            string orderStr = order.Count > 0 ? string.Join(" → ", order) : "N/A";
            string note = hasSpecialCells
                ? "\n⚠️ Map có Special Cells — kiểm tra thêm trong Play Mode."
                : string.Empty;
            return $"✅ Map giải được!\nThứ tự gợi ý: [{orderStr}]{note}";
        }

        private static string BuildDeadlockSummary(HashSet<string> stuck,
            List<DeadlockGroup> groups, bool hasSpecialCells)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"❌ LEVEL CHẾT! {stuck.Count} mũi tên kẹt: [{string.Join(", ", stuck)}]");

            for (int i = 0; i < groups.Count; i++)
            {
                DeadlockGroup g = groups[i];
                sb.AppendLine($"🔒 Nhóm {i + 1} ({g.ArrowIds.Count} mũi tên): {g.BlockChainDescription}");
            }

            if (hasSpecialCells)
                sb.AppendLine("⚠️ Map có Special Cells — chúng có thể thay đổi kết quả khi chạy.");

            return sb.ToString().TrimEnd();
        }
    }

    // =========================================================================
    //  SimGrid — lưới mô phỏng nội bộ (internal)
    // =========================================================================

    /// <summary>
    /// Lưới mô phỏng nhẹ: chỉ theo dõi ô nào thuộc arrow nào.
    /// Không phụ thuộc MonoBehaviour, có thể chạy trong Editor code.
    /// </summary>
    internal class SimGrid
    {
        private readonly int _w;
        private readonly int _h;
        private readonly string[,] _cells;
        private readonly Dictionary<string, ArrowSaveData> _arrows = new Dictionary<string, ArrowSaveData>();
        private readonly HashSet<string> _everBlocked = new HashSet<string>();

        public SimGrid(int width, int height)
        {
            _w = width;
            _h = height;
            _cells = new string[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[x, y] = string.Empty;
        }

        public void PlaceArrow(ArrowSaveData arrow)
        {
            if (arrow?.Path == null) return;
            _arrows[arrow.ArrowID] = arrow;
            foreach (Vector2Int pos in arrow.Path)
                if (InBounds(pos)) _cells[pos.x, pos.y] = arrow.ArrowID;
        }

        public void RemoveArrow(string id)
        {
            if (!_arrows.TryGetValue(id, out ArrowSaveData arrow)) return;
            foreach (Vector2Int pos in arrow.Path)
                if (InBounds(pos) && _cells[pos.x, pos.y] == id)
                    _cells[pos.x, pos.y] = string.Empty;
            _arrows.Remove(id);
        }

        public List<string> GetAllArrowIds() => new List<string>(_arrows.Keys);

        public bool WasBlockedAtLeastOnce(string id) => _everBlocked.Contains(id);

        /// <summary>Kiểm tra arrow có thể thoát không (đường thẳng từ endpoint ra ngoài bảng).</summary>
        public bool CanEscape(string id, HashSet<string> activeSet)
        {
            if (!_arrows.TryGetValue(id, out ArrowSaveData arrow)) return false;

            foreach ((int pathIndex, Direction4 dir) ep in ResolveEndpoints(arrow))
            {
                Vector2Int startPos = arrow.Path[ep.pathIndex];
                Vector2Int step = ep.dir.ToVector2Int();
                int cx = startPos.x + step.x;
                int cy = startPos.y + step.y;

                bool blocked = false;
                HashSet<string> loopGuard = new HashSet<string>();

                while (InBounds(cx, cy))
                {
                    string key = $"{cx}:{cy}:{step.x}:{step.y}";
                    if (!loopGuard.Add(key)) { blocked = true; break; }

                    string occupant = _cells[cx, cy];
                    if (!string.IsNullOrEmpty(occupant) && occupant != id && activeSet.Contains(occupant))
                    {
                        blocked = true;
                        break;
                    }

                    cx += step.x;
                    cy += step.y;
                }

                if (!blocked) return true;
            }

            _everBlocked.Add(id);
            return false;
        }

        /// <summary>Trả về ID arrow đang trực tiếp chặn arrow này.</summary>
        public string GetBlockerId(string id, HashSet<string> activeSet)
        {
            if (!_arrows.TryGetValue(id, out ArrowSaveData arrow)) return null;

            foreach ((int pathIndex, Direction4 dir) ep in ResolveEndpoints(arrow))
            {
                Vector2Int startPos = arrow.Path[ep.pathIndex];
                Vector2Int step = ep.dir.ToVector2Int();
                int cx = startPos.x + step.x;
                int cy = startPos.y + step.y;

                while (InBounds(cx, cy))
                {
                    string occupant = _cells[cx, cy];
                    if (!string.IsNullOrEmpty(occupant) && occupant != id && activeSet.Contains(occupant))
                        return occupant;
                    cx += step.x;
                    cy += step.y;
                }
            }
            return null;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static List<(int pathIndex, Direction4 dir)> ResolveEndpoints(ArrowSaveData arrow)
        {
            var result = new List<(int, Direction4)>();
            if (arrow.Path == null || arrow.Path.Count == 0) return result;

            if (arrow.Endpoints != null && arrow.Endpoints.Count > 0)
            {
                foreach (ArrowEndpointSaveData ep in arrow.Endpoints)
                {
                    if (ep != null) result.Add((ep.PathIndex, ep.ExitDirection));
                }
            }
            else
            {
                // Legacy fallback
                int pathCount = arrow.Path.Count;
                int primaryIdx = arrow.IsHeadFirst ? 0 : pathCount - 1;
                result.Add((primaryIdx, CalcDirection(arrow.Path, primaryIdx)));
            }

            return result;
        }

        private static Direction4 CalcDirection(List<Vector2Int> path, int endpointIdx)
        {
            if (path == null || path.Count <= 1) return Direction4.Up;
            int safeIdx = Mathf.Clamp(endpointIdx, 0, path.Count - 1);
            int neighborIdx = safeIdx == 0 ? 1 : path.Count - 2;
            Vector2Int delta = path[safeIdx] - path[neighborIdx];
            return Direction4Extensions.FromVector(delta);
        }

        private bool InBounds(Vector2Int p) => p.x >= 0 && p.x < _w && p.y >= 0 && p.y < _h;
        private bool InBounds(int x, int y) => x >= 0 && x < _w && y >= 0 && y < _h;
    }
}
