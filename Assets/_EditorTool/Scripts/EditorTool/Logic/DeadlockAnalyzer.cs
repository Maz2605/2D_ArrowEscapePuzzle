using System;
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
    /// Phân tích trạng thái bảng để phát hiện "level chết" có tính đến Special Cells và Link Groups:
    /// trạng thái mà không có bất kỳ thứ tự thao tác nào giải phóng được tất cả mũi tên.
    /// </summary>
    public static class DeadlockAnalyzer
    {
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

            List<SpecialCellSaveData> specialCells = editorGrid.GetSpecialSaveData();
            sim.LoadSpecialCells(specialCells);

            bool hasSpecialCells = specialCells != null && specialCells.Count > 0;

            // ── Phân nhóm mũi tên theo LinkGroupId ─────────────────────────
            Dictionary<string, List<SimArrow>> groupsDict = new Dictionary<string, List<SimArrow>>();
            List<List<SimArrow>> allGroups = new List<List<SimArrow>>();

            foreach (SimArrow arrow in sim.GetArrows())
            {
                if (string.IsNullOrWhiteSpace(arrow.LinkGroupId))
                {
                    allGroups.Add(new List<SimArrow> { arrow });
                }
                else
                {
                    if (!groupsDict.TryGetValue(arrow.LinkGroupId, out var group))
                    {
                        group = new List<SimArrow>();
                        groupsDict[arrow.LinkGroupId] = group;
                        allGroups.Add(group);
                    }
                    group.Add(arrow);
                }
            }

            // ── Mô phỏng giải level (greedy simulation) ───────────────────────
            HashSet<string> remaining = new HashSet<string>(sim.GetAllArrowIds());
            List<string> escapedOrder = new List<string>();

            bool progress = true;
            bool isFirstIteration = true;

            while (progress && remaining.Count > 0)
            {
                progress = false;
                List<List<SimArrow>> groupsToEscape = new List<List<SimArrow>>();

                foreach (var group in allGroups)
                {
                    if (group.Count > 0 && remaining.Contains(group[0].ArrowId))
                    {
                        if (CanGroupEscape(group, remaining, sim))
                        {
                            groupsToEscape.Add(group);
                        }
                        else if (isFirstIteration)
                        {
                            // Nếu không thoát được ở bước đầu tiên, đánh dấu là bị chặn để loại khỏi InitiallyFreeArrows
                            foreach (var arrow in group)
                            {
                                sim.MarkAsBlocked(arrow.ArrowId);
                            }
                        }
                    }
                }

                if (groupsToEscape.Count > 0)
                {
                    foreach (var group in groupsToEscape)
                    {
                        int escapeCount = 0;
                        foreach (var arrow in group)
                        {
                            if (remaining.Contains(arrow.ArrowId))
                            {
                                if (!sim.WasBlockedAtLeastOnce(arrow.ArrowId))
                                {
                                    result.InitiallyFreeArrows.Add(arrow.ArrowId);
                                }

                                remaining.Remove(arrow.ArrowId);
                                escapedOrder.Add(arrow.ArrowId);
                                sim.RemoveArrow(arrow.ArrowId);
                                escapeCount++;
                            }
                        }

                        // Giảm đếm tất cả CounterBlock tương ứng với số lượng mũi tên thoát
                        for (int i = 0; i < escapeCount; i++)
                        {
                            sim.DecrementCounterBlocks();
                        }
                    }
                    progress = true;
                }

                isFirstIteration = false;
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

        private static bool CanGroupEscape(List<SimArrow> group, HashSet<string> activeSet, SimGrid sim)
        {
            if (group == null || group.Count == 0) return false;

            // Một nhóm di chuyển được nếu tồn tại 1 trigger arrow thoát được bất kỳ đầu nào của nó,
            // và toàn bộ các mũi tên khác trong nhóm thoát được bằng Primary Endpoint.
            foreach (var trigger in group)
            {
                bool triggerCanEscape = false;
                foreach (var ep in trigger.Endpoints)
                {
                    if (sim.TraceEscape(trigger.ArrowId, ep, activeSet))
                    {
                        triggerCanEscape = true;
                        break;
                    }
                }

                if (!triggerCanEscape) continue;

                bool othersCanEscape = true;
                foreach (var other in group)
                {
                    if (other.ArrowId == trigger.ArrowId) continue;

                    if (other.PrimaryEndpoint == null || !sim.TraceEscape(other.ArrowId, other.PrimaryEndpoint, activeSet))
                    {
                        othersCanEscape = false;
                        break;
                    }
                }

                if (othersCanEscape) return true;
            }

            return false;
        }

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
                ? "\n✅ (Đã phân tích cả Special Cells và Link Groups)"
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
                sb.AppendLine("⚠️ (Đã phân tích cả Special Cells và Link Groups)");

            return sb.ToString().TrimEnd();
        }
    }

    // =========================================================================
    //  SimArrow / SimSpecialCell — Lớp dữ liệu mô phỏng
    // =========================================================================

    internal class SimArrow
    {
        public string ArrowId;
        public List<Vector2Int> Path;
        public string LinkGroupId;
        public List<ArrowEndpointSaveData> Endpoints;
        public ArrowEndpointSaveData PrimaryEndpoint;

        public SimArrow(ArrowSaveData arrow)
        {
            ArrowId = arrow.ArrowID;
            Path = new List<Vector2Int>(arrow.Path);
            LinkGroupId = (arrow.LinkGroupId ?? string.Empty).Trim();
            Endpoints = new List<ArrowEndpointSaveData>();

            if (arrow.Endpoints != null && arrow.Endpoints.Count > 0)
            {
                foreach (var ep in arrow.Endpoints)
                {
                    if (ep != null)
                    {
                        var cloneEp = ep.Clone();
                        Endpoints.Add(cloneEp);
                        if (cloneEp.IsPrimary)
                        {
                            PrimaryEndpoint = cloneEp;
                        }
                    }
                }
            }

            // Fallback nếu không có endpoint dữ liệu
            if (Endpoints.Count == 0 && Path.Count > 0)
            {
                int pathCount = Path.Count;
                int primaryIdx = arrow.IsHeadFirst ? 0 : Math.Max(0, pathCount - 1);
                var ep = new ArrowEndpointSaveData(primaryIdx, CalcDirection(Path, primaryIdx), true);
                Endpoints.Add(ep);
                PrimaryEndpoint = ep;
            }

            if (PrimaryEndpoint == null && Endpoints.Count > 0)
            {
                PrimaryEndpoint = Endpoints[0];
            }
        }

        private static Direction4 CalcDirection(List<Vector2Int> path, int endpointIdx)
        {
            if (path == null || path.Count <= 1) return Direction4.Up;
            int safeIdx = Mathf.Clamp(endpointIdx, 0, path.Count - 1);
            int neighborIdx = safeIdx == 0 ? 1 : path.Count - 2;
            Vector2Int delta = path[safeIdx] - path[neighborIdx];
            return Direction4Extensions.FromVector(delta);
        }
    }

    internal class SimSpecialCell
    {
        public Vector2Int Position;
        public BoardSpecialType Type;
        public Direction4 ExitDirection;
        public Direction4 PortalDirection => ExitDirection;
        public int Counter;
        public string PortalId;
        public string Id;
        public List<Vector2Int> OccupiedPositions;

        public SimSpecialCell(SpecialCellSaveData cell)
        {
            Position = cell.Position;
            Type = cell.Type;
            ExitDirection = cell.ExitDirection;
            Counter = cell.Counter;
            PortalId = (cell.PortalId ?? string.Empty).Trim();
            Id = cell.Id;
            OccupiedPositions = new List<Vector2Int>();
            foreach (var pos in CounterBlockUtility.GetOccupiedPositions(cell))
            {
                OccupiedPositions.Add(pos);
            }
        }
    }

    // =========================================================================
    //  SimGrid — lưới mô phỏng nội bộ nâng cao
    // =========================================================================

    internal class SimGrid
    {
        private readonly int _w;
        private readonly int _h;
        private readonly string[,] _cells;
        private readonly Dictionary<string, SimArrow> _arrows = new Dictionary<string, SimArrow>();
        private readonly Dictionary<Vector2Int, SimSpecialCell> _specialCells = new Dictionary<Vector2Int, SimSpecialCell>();
        private readonly List<SimSpecialCell> _counterBlocks = new List<SimSpecialCell>();
        private readonly Dictionary<string, List<SimSpecialCell>> _portalGroups = new Dictionary<string, List<SimSpecialCell>>();
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

        public void PlaceArrow(ArrowSaveData arrowData)
        {
            if (arrowData == null) return;
            var arrow = new SimArrow(arrowData);
            _arrows[arrow.ArrowId] = arrow;
            foreach (Vector2Int pos in arrow.Path)
            {
                if (InBounds(pos))
                {
                    _cells[pos.x, pos.y] = arrow.ArrowId;
                }
            }
        }

        public void LoadSpecialCells(List<SpecialCellSaveData> specialCells)
        {
            if (specialCells == null) return;
            foreach (var cellData in specialCells)
            {
                if (cellData == null) continue;
                var cell = new SimSpecialCell(cellData);

                foreach (var pos in cell.OccupiedPositions)
                {
                    if (InBounds(pos))
                    {
                        _specialCells[pos] = cell;
                    }
                }

                if (cell.Type == BoardSpecialType.CounterBlock)
                {
                    _counterBlocks.Add(cell);
                }
                else if (cell.Type == BoardSpecialType.Portal)
                {
                    if (!_portalGroups.TryGetValue(cell.PortalId, out var group))
                    {
                        group = new List<SimSpecialCell>();
                        _portalGroups[cell.PortalId] = group;
                    }
                    group.Add(cell);
                }
            }
        }

        public List<SimArrow> GetArrows() => new List<SimArrow>(_arrows.Values);

        public List<string> GetAllArrowIds() => new List<string>(_arrows.Keys);

        public void RemoveArrow(string id)
        {
            if (!_arrows.TryGetValue(id, out var arrow)) return;
            foreach (Vector2Int pos in arrow.Path)
            {
                if (InBounds(pos) && _cells[pos.x, pos.y] == id)
                {
                    _cells[pos.x, pos.y] = string.Empty;
                }
            }
            _arrows.Remove(id);
        }

        public void DecrementCounterBlocks()
        {
            foreach (var cb in _counterBlocks)
            {
                if (cb.Counter > 0)
                {
                    cb.Counter--;
                }
            }
        }

        public void MarkAsBlocked(string id)
        {
            _everBlocked.Add(id);
        }

        public bool WasBlockedAtLeastOnce(string id)
        {
            return _everBlocked.Contains(id);
        }

        public SimSpecialCell FindTwinPortal(SimSpecialCell portal)
        {
            if (string.IsNullOrEmpty(portal.PortalId)) return null;
            if (_portalGroups.TryGetValue(portal.PortalId, out var group) && group.Count == 2)
            {
                if (group[0].Position == portal.Position) return group[1];
                if (group[1].Position == portal.Position) return group[0];
            }
            return null;
        }

        public bool TraceEscape(string arrowId, ArrowEndpointSaveData ep, HashSet<string> activeSet)
        {
            if (!_arrows.TryGetValue(arrowId, out var arrow)) return false;
            if (ep.PathIndex < 0 || ep.PathIndex >= arrow.Path.Count) return false;

            Vector2Int currentPos = arrow.Path[ep.PathIndex];
            Direction4 exitDir = ep.ExitDirection;
            Vector2Int dir = exitDir.ToVector2Int();

            int cx = currentPos.x + dir.x;
            int cy = currentPos.y + dir.y;
            HashSet<string> loopGuard = new HashSet<string>();

            while (true)
            {
                if (!InBounds(cx, cy))
                {
                    return true; // Thoát thành công ra ngoài biên
                }

                string key = $"{cx}:{cy}:{dir.x}:{dir.y}";
                if (!loopGuard.Add(key))
                {
                    return false; // Lặp vô tận
                }

                string occupant = _cells[cx, cy];
                if (!string.IsNullOrEmpty(occupant) && occupant != arrowId && activeSet.Contains(occupant))
                {
                    return false; // Bị mũi tên khác chặn
                }

                Vector2Int pos = new Vector2Int(cx, cy);

                if (_specialCells.TryGetValue(pos, out var special))
                {
                    if (special.Type == BoardSpecialType.Redirect)
                    {
                        exitDir = special.ExitDirection;
                        dir = exitDir.ToVector2Int();
                        cx = pos.x + dir.x;
                        cy = pos.y + dir.y;
                        continue;
                    }
                    else if (special.Type == BoardSpecialType.Portal)
                    {
                        Direction4 entryDir = Direction4Extensions.FromVector(dir);
                        if (!CanEnterPortal(special, entryDir))
                        {
                            return false; // Cannot enter through the portal's exit face
                        }

                        var twin = FindTwinPortal(special);
                        if (twin == null)
                        {
                            return false;
                        }

                        Vector2Int exitPosition = twin.Position;
                        Direction4 portalExitDirection = twin.PortalDirection;

                        dir = portalExitDirection.ToVector2Int();
                        cx = exitPosition.x + dir.x;
                        cy = exitPosition.y + dir.y;
                        continue;
                    }
                    else if (special.Type == BoardSpecialType.CounterBlock)
                    {
                        if (special.Counter > 0)
                        {
                            return false; // Bị CounterBlock chặn
                        }
                    }
                }

                cx += dir.x;
                cy += dir.y;
            }
        }

        public string GetBlockerId(string arrowId, HashSet<string> activeSet)
        {
            if (!_arrows.TryGetValue(arrowId, out var arrow)) return null;

            foreach (var ep in arrow.Endpoints)
            {
                string blocker = GetBlockerId(arrowId, ep, activeSet);
                if (!string.IsNullOrEmpty(blocker))
                    return blocker;
            }
            return null;
        }

        public string GetBlockerId(string arrowId, ArrowEndpointSaveData ep, HashSet<string> activeSet)
        {
            if (!_arrows.TryGetValue(arrowId, out var arrow)) return null;
            if (ep.PathIndex < 0 || ep.PathIndex >= arrow.Path.Count) return null;

            Vector2Int currentPos = arrow.Path[ep.PathIndex];
            Direction4 exitDir = ep.ExitDirection;
            Vector2Int dir = exitDir.ToVector2Int();

            int cx = currentPos.x + dir.x;
            int cy = currentPos.y + dir.y;
            HashSet<string> loopGuard = new HashSet<string>();

            while (true)
            {
                if (!InBounds(cx, cy))
                {
                    return null;
                }

                string key = $"{cx}:{cy}:{dir.x}:{dir.y}";
                if (!loopGuard.Add(key))
                {
                    return null;
                }

                string occupant = _cells[cx, cy];
                if (!string.IsNullOrEmpty(occupant) && occupant != arrowId && activeSet.Contains(occupant))
                {
                    return occupant; // Bị occupant chặn
                }

                Vector2Int pos = new Vector2Int(cx, cy);

                if (_specialCells.TryGetValue(pos, out var special))
                {
                    if (special.Type == BoardSpecialType.Redirect)
                    {
                        exitDir = special.ExitDirection;
                        dir = exitDir.ToVector2Int();
                        cx = pos.x + dir.x;
                        cy = pos.y + dir.y;
                        continue;
                    }
                    else if (special.Type == BoardSpecialType.Portal)
                    {
                        Direction4 entryDir = Direction4Extensions.FromVector(dir);
                        if (!CanEnterPortal(special, entryDir))
                        {
                            return null;
                        }

                        var twin = FindTwinPortal(special);
                        if (twin == null)
                        {
                            return null;
                        }

                        Vector2Int exitPosition = twin.Position;
                        Direction4 portalExitDirection = twin.PortalDirection;

                        dir = portalExitDirection.ToVector2Int();
                        cx = exitPosition.x + dir.x;
                        cy = exitPosition.y + dir.y;
                        continue;
                    }
                    else if (special.Type == BoardSpecialType.CounterBlock)
                    {
                        if (special.Counter > 0)
                        {
                            return null;
                        }
                    }
                }

                cx += dir.x;
                cy += dir.y;
            }
        }

        private static bool CanEnterPortal(SimSpecialCell portal, Direction4 entryDirection)
        {
            return portal != null && entryDirection != portal.PortalDirection;
        }

        private bool InBounds(Vector2Int p) => p.x >= 0 && p.x < _w && p.y >= 0 && p.y < _h;
        private bool InBounds(int x, int y) => x >= 0 && x < _w && y >= 0 && y < _h;
    }
}
