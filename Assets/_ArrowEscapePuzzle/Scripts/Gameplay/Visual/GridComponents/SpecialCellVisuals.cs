using System;
using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual.GridComponents
{
    public class SpecialCellVisuals : MonoBehaviour
    {
        [Header("--- REFERENCES ---")]
        [SerializeField] private List<SpecialCellVisualPrefabSlot> specialCellVisualPrefabs =
            new List<SpecialCellVisualPrefabSlot>();

        private readonly Dictionary<Vector2Int, SpecialCellViewBase> _specialCellViews =
            new Dictionary<Vector2Int, SpecialCellViewBase>();
        
        private readonly Dictionary<string, int> _portalVariantMap = new Dictionary<string, int>();

        private GridSystem _logic;
        private Transform _specialMarkerRoot;
        private float _cellSize;

        public void Initialize(GridSystem logic, float cellSize, Transform specialMarkerRoot)
        {
            _logic = logic;
            _cellSize = cellSize;
            _specialMarkerRoot = specialMarkerRoot;

            SpawnSpecialMarkers();
        }

        public IEnumerable<SpecialCellViewBase> GetUniqueViews()
        {
            return new HashSet<SpecialCellViewBase>(_specialCellViews.Values);
        }

        public void SpawnSpecialMarkers()
        {
            ClearSpecialMarkers();
            if (_logic == null || _specialMarkerRoot == null || _logic.SpecialCells == null) return;

            // BƯỚC QUAN TRỌNG: Quét và cấp phát màu duy nhất trước khi render
            MapPortalVariantsForCurrentLevel();

            foreach (SpecialCellSaveData specialCell in _logic.SpecialCells)
            {
                if (specialCell == null) continue;

                GameObject markerPrefab = GetSpecialMarkerPrefab(specialCell.Type);
                if (markerPrefab == null) continue;

                GameObject markerObject = PoolingManager.Instance.Spawn(markerPrefab, Vector3.zero, Quaternion.identity, _specialMarkerRoot);
                markerObject.name = $"Special_{specialCell.Type}_{specialCell.Position.x}_{specialCell.Position.y}";
        
                SpecialCellViewBase markerView = GetExistingSpecialCellView(markerObject, specialCell.Type);
                if (markerView == null)
                {
                    PoolingManager.Instance.Despawn(markerObject);
                    continue;
                }

                markerView.Setup(specialCell, _cellSize, GetSpecialCellColor(specialCell));
                RegisterSpecialCellView(specialCell, markerView);

                // BƯỚC QUAN TRỌNG: Ép Portal dùng đúng màu đã được cấp phát
                if (markerView is PortalSpecialCellView portalView)
                {
                    int allocatedIndex = _portalVariantMap.TryGetValue(specialCell.PortalId, out int val) ? val : 0;
                    portalView.SetExactVariant(allocatedIndex);
                }
            }
        }

        public void ClearSpecialMarkers()
        {
            HashSet<SpecialCellViewBase> uniqueViews = new HashSet<SpecialCellViewBase>(_specialCellViews.Values);
            foreach (SpecialCellViewBase view in uniqueViews)
            {
                if (view != null)
                {
                    PoolingManager.Instance.Despawn(view.gameObject);
                }
            }

            _specialCellViews.Clear();
        }

        public void HandleSpecialCellChanged((SpecialCellSaveData data, Vector2Int dir) payload)
        {
            if (payload.data != null && _specialCellViews.TryGetValue(payload.data.Position, out SpecialCellViewBase view) &&
                view is CounterBlockView counterView)
            {
                counterView.UpdateCounter(payload.data.Counter);
                counterView.PlayArrowExitFeedback(payload.dir);
            }
        }

        /// <summary>
        /// Xử lý khi một Mystery Box được mở khóa: despawn hộp cũ và spawn ô được hé lộ (nếu có).
        /// </summary>
        public void HandleMysteryBoxOpened((Vector2Int pos, SpecialCellSaveData revealedCell) payload)
        {
            Vector2Int pos = payload.pos;

            // Despawn Mystery Box view cũ
            if (_specialCellViews.TryGetValue(pos, out SpecialCellViewBase boxView) && boxView != null)
            {
                RemoveSpecialCellViewReferences(boxView);
                PoolingManager.Instance.Despawn(boxView.gameObject);
            }

            // Nếu có ô ẩn bên trong, spawn visual của nó
            if (payload.revealedCell != null)
            {
                SpawnRevealedCell(payload.revealedCell);
            }
        }

        private void SpawnRevealedCell(SpecialCellSaveData revealedCell)
        {
            GameObject markerPrefab = GetSpecialMarkerPrefab(revealedCell.Type);
            if (markerPrefab == null)
            {
                Debug.LogWarning($"[SpecialCellVisuals] Không tìm thấy prefab cho loại {revealedCell.Type} được hé lộ từ Mystery Box.");
                return;
            }

            GameObject markerObject = PoolingManager.Instance.Spawn(markerPrefab, Vector3.zero, Quaternion.identity, _specialMarkerRoot);
            markerObject.name = $"Special_{revealedCell.Type}_{revealedCell.Position.x}_{revealedCell.Position.y}";

            SpecialCellViewBase markerView = GetExistingSpecialCellView(markerObject, revealedCell.Type);
            if (markerView == null)
            {
                PoolingManager.Instance.Despawn(markerObject);
                return;
            }

            // Nếu là Portal, tính lại variant màu
            if (markerView is PortalSpecialCellView portalView)
            {
                // Đảm bảo portal mới được thêm vào map variant
                if (!string.IsNullOrEmpty(revealedCell.PortalId) && !_portalVariantMap.ContainsKey(revealedCell.PortalId))
                {
                    int nextIndex = _portalVariantMap.Count;
                    _portalVariantMap[revealedCell.PortalId] = nextIndex;
                }

                markerView.Setup(revealedCell, _cellSize, GetSpecialCellColor(revealedCell));
                int allocatedIndex = _portalVariantMap.TryGetValue(revealedCell.PortalId, out int val) ? val : 0;
                portalView.SetExactVariant(allocatedIndex);
            }
            else
            {
                markerView.Setup(revealedCell, _cellSize, GetSpecialCellColor(revealedCell));
            }

            RegisterSpecialCellView(revealedCell, markerView);
            markerView.PlaySpawnAnimation(0f, 0.3f);
        }

        public void HandleSpecialCellDestroyed(Vector2Int pos, Action<List<Vector2Int>> onCounterBlockDestroyed)
        {
            if (!_specialCellViews.TryGetValue(pos, out SpecialCellViewBase view) || view == null) return;

            List<Vector2Int> destroyedFootprint = GetSpecialCellFootprintPositions(view.BoundSpecialCell);
            RemoveSpecialCellViewReferences(view);

            if (view is CounterBlockView counterBlockView)
            {
                counterBlockView.PlayDestroyAnimation(() =>
                {
                    onCounterBlockDestroyed?.Invoke(destroyedFootprint);
                    PoolingManager.Instance.Despawn(counterBlockView.gameObject);
                });
                return;
            }

            PoolingManager.Instance.Despawn(view.gameObject);
        }

        public void HandleArrowPassedGridPosition(ArrowPathVisualTrigger trigger)
        {
            if (!_specialCellViews.TryGetValue(trigger.GridPos, out SpecialCellViewBase view) || view == null)
            {
                return;
            }

            if (view is PortalSpecialCellView portalView)
            {
                if (trigger.TriggerType == ArrowPathVisualTriggerType.PortalEntry)
                {
                    portalView.PlayPortalEntryEffect(trigger.TravelDirection);
                    return;
                }

                if (trigger.TriggerType == ArrowPathVisualTriggerType.PortalExit)
                {
                    portalView.PlayPortalExitEffect(trigger.TravelDirection);
                    return;
                }
            }

            view.PlayHighlight();
        }

        public bool TryPlaySpecialCellRejection(Vector2Int gridPos)
        {
            if (_specialCellViews.TryGetValue(gridPos, out SpecialCellViewBase view) && view != null)
            {
                view.PlayRejectionAnimation();
                return true;
            }

            return false;
        }

        public void ApplyTheme(ThemeConfigSO newTheme)
        {
            HashSet<SpecialCellViewBase> updatedViews = new HashSet<SpecialCellViewBase>(_specialCellViews.Values);
            foreach (SpecialCellViewBase view in updatedViews)
            {
                if (view?.BoundSpecialCell == null) continue;
                view.RefreshVisualColor(GetSpecialCellColor(view.BoundSpecialCell, newTheme));
            }
        }

        public bool TryGetCounterBlockView(Vector2Int gridPos, out CounterBlockView counterBlockView)
        {
            counterBlockView = null;
            if (!_specialCellViews.TryGetValue(gridPos, out SpecialCellViewBase view)) return false;

            counterBlockView = view as CounterBlockView;
            return counterBlockView != null;
        }

        private void RegisterSpecialCellView(SpecialCellSaveData specialCell, SpecialCellViewBase markerView)
        {
            _specialCellViews[specialCell.Position] = markerView;
            if (specialCell.OccupiedOffsets == null) return;

            foreach (Vector2Int offset in specialCell.OccupiedOffsets)
            {
                if (offset == Vector2Int.zero) continue;
                Vector2Int targetPos = specialCell.Position + offset;
                _specialCellViews[targetPos] = markerView;
            }
        }

        private void RemoveSpecialCellViewReferences(SpecialCellViewBase targetView)
        {
            if (targetView == null || _specialCellViews.Count == 0) return;

            List<Vector2Int> keysToRemove = null;
            foreach (KeyValuePair<Vector2Int, SpecialCellViewBase> kvp in _specialCellViews)
            {
                if (kvp.Value != targetView) continue;

                keysToRemove ??= new List<Vector2Int>();
                keysToRemove.Add(kvp.Key);
            }

            if (keysToRemove == null) return;

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _specialCellViews.Remove(keysToRemove[i]);
            }
        }

        private Color GetSpecialCellColor(SpecialCellSaveData specialCell, ThemeConfigSO themeOverride = null)
        {
            ThemeConfigSO theme = themeOverride ?? ThemeManager.Instance.CurrentTheme;

            if (specialCell.Type == BoardSpecialType.Redirect)
            {
                if (theme != null && theme.isRandomRedirectColor && theme.redirectColorPalette != null &&
                    theme.redirectColorPalette.Count > 0)
                {
                    int seed = Mathf.Abs(specialCell.Position.GetHashCode());
                    return theme.redirectColorPalette[seed % theme.redirectColorPalette.Count];
                }

                return theme != null ? theme.redirectDefaultColor : Color.white;
            }

            if (specialCell.Type == BoardSpecialType.CounterBlock)
            {
                return theme != null ? theme.blockerCounterColor : Color.gray;
            }

            if (specialCell.Type == BoardSpecialType.Portal)
            {
                return ResolvePortalPairColor(specialCell, theme);
            }

            if (specialCell.Type == BoardSpecialType.Key)
            {
                // Màu vàng ánh kim cho chìa khóa
                return new Color(1f, 0.85f, 0.1f);
            }

            if (specialCell.Type == BoardSpecialType.MysteryBox)
            {
                // Màu nâu tối cho hộp bí ẩn
                return new Color(0.45f, 0.28f, 0.1f);
            }

            return Color.white;
        }

        private GameObject GetSpecialMarkerPrefab(BoardSpecialType type)
        {
            for (int i = 0; i < specialCellVisualPrefabs.Count; i++)
            {
                SpecialCellVisualPrefabSlot slot = specialCellVisualPrefabs[i];
                if (slot != null && slot.Type == type && slot.Prefab != null)
                {
                    return slot.Prefab;
                }
            }
            return null;
        }

        private static SpecialCellViewBase GetExistingSpecialCellView(GameObject markerObject, BoardSpecialType type)
        {
            SpecialCellViewBase view = markerObject.GetComponent<SpecialCellViewBase>();
            if (view == null)
            {
                Debug.LogError($"[SpecialCellVisuals] Prefab for {type} is missing a SpecialCellViewBase-derived component. Please add and assign it manually.");
            }

            return view;
        }

        private static Color ResolvePortalPairColor(SpecialCellSaveData specialCell, ThemeConfigSO theme)
        {
            Color baseColor = theme != null ? theme.portalDefaultColor : Color.magenta;
            string portalId = specialCell != null ? specialCell.PortalId : string.Empty;
            if (string.IsNullOrEmpty(portalId))
            {
                return baseColor;
            }

            Color.RGBToHSV(baseColor, out float baseHue, out float baseSaturation, out float baseValue);
            int hash = portalId.GetHashCode() & 0x7fffffff;
            float hueOffset = (hash % 360) / 360f;
            float saturation = Mathf.Clamp01(Mathf.Max(baseSaturation, 0.68f) + (((hash / 17) % 9) - 4) * 0.018f);
            float value = Mathf.Clamp(Mathf.Max(baseValue, 0.92f) + (((hash / 29) % 11) - 5) * 0.02f, 0f, 1.35f);
            float hue = Mathf.Repeat(baseHue + hueOffset, 1f);
            return Color.HSVToRGB(hue, saturation, value, true);
        }

        public static List<Vector2Int> GetSpecialCellFootprintPositions(SpecialCellSaveData specialCell)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (specialCell == null) return positions;

            HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>();
            foreach (Vector2Int occupiedPos in CounterBlockUtility.GetOccupiedPositions(specialCell))
            {
                if (uniquePositions.Add(occupiedPos))
                {
                    positions.Add(occupiedPos);
                }
            }

            positions.Sort((a, b) =>
            {
                int yCompare = b.y.CompareTo(a.y);
                return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
            });

            return positions;
        }
        private void MapPortalVariantsForCurrentLevel()
        {
            _portalVariantMap.Clear();
            if (_logic == null || _logic.SpecialCells == null) return;

            int currentIndex = 0;
            foreach (SpecialCellSaveData cell in _logic.SpecialCells)
            {
                if (cell.Type == BoardSpecialType.Portal && !string.IsNullOrEmpty(cell.PortalId))
                {
                    // Nếu phát hiện Portal ID mới, cấp phát cho nó 1 con số độc nhất
                    if (!_portalVariantMap.ContainsKey(cell.PortalId))
                    {
                        _portalVariantMap[cell.PortalId] = currentIndex;
                        currentIndex++;
                    }
                }
            }
        }

        public void PlayRestoreFromLoseAnimation(float duration)
        {
            ThemeConfigSO theme = ThemeManager.Instance.CurrentTheme;
            HashSet<SpecialCellViewBase> uniqueViews = new HashSet<SpecialCellViewBase>(_specialCellViews.Values);
            foreach (SpecialCellViewBase view in uniqueViews)
            {
                if (view == null || view.BoundSpecialCell == null) continue;
                Color originColor = GetSpecialCellColor(view.BoundSpecialCell, theme);
                view.PlayRestoreFromLoseAnimation(duration, originColor);
            }
        }
    }
}
