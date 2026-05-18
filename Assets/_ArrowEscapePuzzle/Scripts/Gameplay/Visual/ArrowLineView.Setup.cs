using System.Collections.Generic;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public partial class ArrowLineView
    {
        public void Setup(ArrowModel arrowModel, List<ArrowData> sortedPath, float cellSize, ThemeConfigSO theme)
        {
            if (_currentState == ArrowState.Escaping) return;
            KillAllActiveTweens();

            _currentState = ArrowState.Idle;
            _isMarkedAsWrong = false;
            _cellSize = cellSize;

            ArrowID = arrowModel != null ? arrowModel.ArrowId : (sortedPath != null && sortedPath.Count > 0 ? sortedPath[0].ID : "unknown");

            ResolveBaseColor(theme);

            if (arrowModel != null && arrowModel.Path != null && arrowModel.Path.Count > 0)
            {
                _gridPath.Clear();
                _gridPath.AddRange(arrowModel.Path);
                _availableEndpoints.Clear();
                
                if (arrowModel.Endpoints != null)
                {
                    for (int i = 0; i < arrowModel.Endpoints.Count; i++)
                    {
                        if (arrowModel.Endpoints[i] != null) _availableEndpoints.Add(arrowModel.Endpoints[i]);
                    }
                }

                _primaryEndpointPathIndex = arrowModel.PrimaryEndpoint != null ? arrowModel.PrimaryEndpoint.PathIndex : _gridPath.Count - 1;
                RecalculateBoundsFromGridPath();
                ConfigureEndpointVisual(arrowModel.PrimaryEndpoint);
            }
            else if (sortedPath != null && sortedPath.Count > 0)
            {
                _gridPath.Clear();
                _availableEndpoints.Clear();
                _primaryEndpointPathIndex = -1;
                _activeEndpointPathIndex = -1;
                if (_secondaryEndpointMarker != null) _secondaryEndpointMarker.enabled = false;

                List<ArrowData> orderedPath = GetVisualOrderedPath(sortedPath);

                Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, 0f);
                Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, 0f);

                foreach (ArrowData node in orderedPath)
                {
                    Vector3 pos = new Vector3(node.X * cellSize, node.Y * cellSize, 0f);
                    minBounds = Vector3.Min(minBounds, pos);
                    maxBounds = Vector3.Max(maxBounds, pos);
                }

                _centerPivot = (minBounds + maxBounds) / 2f;
                transform.localPosition = _centerPivot;

                _bodyPoints = new Vector3[orderedPath.Count];
                for (int i = 0; i < orderedPath.Count; i++)
                {
                    _bodyPoints[i] = new Vector3(orderedPath[i].X * cellSize, orderedPath[i].Y * cellSize, 0f) - _centerPivot;
                }

                ArrowData headData = orderedPath[orderedPath.Count - 1];
                _currentHeadRotation = Quaternion.Euler(0f, 0f, GetHeadRotation(headData.Type));
                headTransform.localRotation = _currentHeadRotation;
                _defaultEscapeDirection = GetDirectionVector(headData.Type);
            }

            ClearTraceRoute();
            UpdateSnakeBody();

            if (lineDirection != null) lineDirection.enabled = false;
            if (_secondaryLineDirection != null) _secondaryLineDirection.enabled = false;
        }

        private void ResolveBaseColor(ThemeConfigSO theme)
        {
            if (theme == null) return;

            if (!theme.isRandomArrowColor)
            {
                _baseColor = theme.arrowDefaultColor;
            }
            else if (theme.arrowColorPalette != null && theme.arrowColorPalette.Count > 0)
            {
                int seed = Mathf.Abs(ArrowID.GetHashCode());
                _baseColor = theme.arrowColorPalette[seed % theme.arrowColorPalette.Count];
            }
            else
            {
                _baseColor = theme.arrowDefaultColor;
            }

            _blockedColor = theme.arrowBlockedColor;
            _loseColor = theme.arrowLoseColor;

            ResetColor();
        }

        public void UpdateThemeColor(ThemeConfigSO newTheme)
        {
            ResolveBaseColor(newTheme);

            if (_currentState == ArrowState.Idle)
            {
                ChangeColorSmooth(_baseColor, 0.4f);
            }

            if (_secondaryEndpointMarker != null && _secondaryEndpointMarker.enabled)
            {
                _secondaryEndpointMarker.color = _baseColor;
            }
        }

        public void SetTraceRoute(EscapeTraceResult traceResult)
        {
            SetTraceRoutes(traceResult, null);
        }

        public void SetTraceRoutes(EscapeTraceResult traceResult, EscapeTraceResult secondaryGuideTraceResult)
        {
            _activeTraceResult = traceResult;
            _secondaryGuideTraceResult = secondaryGuideTraceResult;

            if (_bodyPoints == null || _bodyPoints.Length == 0) return;

            if (_gridPath.Count > 0)
            {
                ConfigureEndpointVisual(traceResult != null ? ResolveEndpoint(traceResult.StartPathIndex) : ResolveEndpoint(_primaryEndpointPathIndex));
            }

            _escapeDirection = traceResult != null ? traceResult.FinalDirection.ToVector3() : _defaultEscapeDirection;
            BuildMovementPath(traceResult);
            UpdateSecondaryEndpointStateForTrace(traceResult);
            UpdateSnakeBody();
            UpdateDirectionLineIfEnabled();
        }

        public void UpdateDirectionLineIfEnabled()
        {
            if (lineDirection != null && lineDirection.enabled) UpdateDirectionLine();
        }

        public void ClearTraceRoute()
        {
            _activeTraceResult = null;
            _secondaryGuideTraceResult = null;
            if (_gridPath.Count > 0)
            {
                int targetPathIndex = _activeEndpointPathIndex >= 0 ? _activeEndpointPathIndex : _primaryEndpointPathIndex;
                ConfigureEndpointVisual(ResolveEndpoint(targetPathIndex));
            }

            _escapeDirection = _defaultEscapeDirection;
            BuildMovementPath(null);
        }

        private void UpdateSecondaryEndpointStateForTrace(EscapeTraceResult traceResult)
        {
            if (_secondaryEndpointMarker == null) return;
            _secondaryEndpointMarker.enabled = GetSecondaryEndpoint(_activeEndpointPathIndex) != null;
        }

        private List<ArrowData> GetVisualOrderedPath(List<ArrowData> path)
        {
            if (path == null || path.Count <= 1) return path;

            bool firstIsHead = IsHeadType(path[0].Type);
            bool lastIsHead = IsHeadType(path[path.Count - 1].Type);

            if (firstIsHead && !lastIsHead)
            {
                List<ArrowData> reversed = new List<ArrowData>(path);
                reversed.Reverse();
                return reversed;
            }

            return path;
        }

        private bool IsHeadType(CellType type) =>
            type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown ||
            type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;

        private void RecalculateBoundsFromGridPath()
        {
            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, 0f);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, 0f);

            for (int i = 0; i < _gridPath.Count; i++)
            {
                Vector3 pos = new Vector3(_gridPath[i].x * _cellSize, _gridPath[i].y * _cellSize, 0f);
                minBounds = Vector3.Min(minBounds, pos);
                maxBounds = Vector3.Max(maxBounds, pos);
            }

            _centerPivot = (minBounds + maxBounds) / 2f;
            transform.localPosition = _centerPivot;
        }

        private ArrowEndpoint ResolveEndpoint(int pathIndex)
        {
            for (int i = 0; i < _availableEndpoints.Count; i++)
            {
                if (_availableEndpoints[i].PathIndex == pathIndex) return _availableEndpoints[i];
            }
            return _availableEndpoints.Count > 0 ? _availableEndpoints[0] : null;
        }

        private ArrowEndpoint GetSecondaryEndpoint(int activeEndpointPathIndex)
        {
            for (int i = 0; i < _availableEndpoints.Count; i++)
            {
                ArrowEndpoint candidate = _availableEndpoints[i];
                if (candidate != null && candidate.PathIndex != activeEndpointPathIndex) return candidate;
            }
            return null;
        }

        private void ConfigureEndpointVisual(ArrowEndpoint endpoint)
        {
            if (_gridPath.Count == 0) return;

            endpoint ??= ResolveEndpoint(_primaryEndpointPathIndex);
            int endpointPathIndex = endpoint != null ? endpoint.PathIndex : Mathf.Max(0, _gridPath.Count - 1);
            _activeEndpointPathIndex = endpointPathIndex;

            bool reversePath = _gridPath.Count > 1 && endpointPathIndex == 0;
            int count = _gridPath.Count;
            _bodyPoints = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Vector2Int source = reversePath ? _gridPath[count - 1 - i] : _gridPath[i];
                _bodyPoints[i] = new Vector3(source.x * _cellSize, source.y * _cellSize, 0f) - _centerPivot;
            }

            _defaultEscapeDirection = endpoint != null ? endpoint.ExitDirection.ToVector3() : Vector3.up;
            _currentHeadRotation = GetHeadRotationFromDirection(_defaultEscapeDirection);
            headTransform.localRotation = _currentHeadRotation;
            UpdateSecondaryEndpointMarker(endpointPathIndex);
        }

        private void UpdateSecondaryEndpointMarker(int activeEndpointPathIndex)
        {
            if (_secondaryEndpointMarker == null) return;

            if (headSpriteRenderer != null)
            {
                _secondaryEndpointMarker.sprite = headSpriteRenderer.sprite;
                _secondaryEndpointMarker.sharedMaterial = headSpriteRenderer.sharedMaterial;
                _secondaryEndpointMarker.sortingLayerID = headSpriteRenderer.sortingLayerID;
                _secondaryEndpointMarker.sortingOrder = headSpriteRenderer.sortingOrder;
            }
            
            ArrowEndpoint secondaryEndpoint = GetSecondaryEndpoint(activeEndpointPathIndex);

            if (secondaryEndpoint == null)
            {
                _secondaryEndpointMarker.enabled = false;
                return;
            }

            _secondaryEndpointMarker.enabled = true;
            _secondaryEndpointMarker.transform.localPosition = GridToLocalPoint(secondaryEndpoint.Position);
            _secondaryEndpointMarker.transform.localRotation = GetHeadRotationFromDirection(secondaryEndpoint.ExitDirection.ToVector3());
            _secondaryEndpointMarker.transform.localScale = headTransform != null ? headTransform.localScale : Vector3.one;
            _secondaryEndpointMarker.color = _baseColor;
        }

        private void BuildMovementPath(EscapeTraceResult traceResult)
        {
            var routeWaypoints = traceResult?.RouteWaypoints;
            if (_bodyPoints == null || _bodyPoints.Length == 0)
            {
                _movementPoints = null;
                _movementDistances = null;
                _bodyLength = 0f;
                _movementLength = 0f;
                return;
            }

            List<Vector3> points = new List<Vector3>(_bodyPoints.Length + (routeWaypoints?.Count ?? 0));
            List<float> distances = new List<float>(_bodyPoints.Length + (routeWaypoints?.Count ?? 0));

            for (int i = 0; i < _bodyPoints.Length; i++)
            {
                points.Add(_bodyPoints[i]);
                distances.Add(i * _cellSize);
            }

            _bodyLength = (_bodyPoints.Length - 1) * _cellSize;
            float currentDistance = _bodyLength;

            _pathTriggers.Clear();
            _currentTriggerIndex = 0;

            if (routeWaypoints != null)
            {
                for (int i = 0; i < routeWaypoints.Count; i++)
                {
                    EscapeTraceWaypoint waypoint = routeWaypoints[i];
                    currentDistance += waypoint.StepCost * _cellSize;
                    points.Add(GridToLocalPoint(waypoint.Position));
                    distances.Add(currentDistance);
                    _pathTriggers.Add(new PathTrigger { distance = currentDistance, gridPos = waypoint.Position });
                }
            }

            _movementPoints = points.ToArray();
            _movementDistances = distances.ToArray();
            _movementLength = currentDistance;
        }

        private Vector3 GridToLocalPoint(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * _cellSize, gridPosition.y * _cellSize, 0f) - _centerPivot;
        }
        
        public void SetActiveEndpoint(int selectedPathIndex)
        {
            if (_gridPath == null || _gridPath.Count == 0) return;

            ArrowEndpoint selectedEndpoint = ResolveEndpoint(selectedPathIndex);
            if (selectedEndpoint != null)
            {
                ConfigureEndpointVisual(selectedEndpoint);
                BuildMovementPath(null);
                UpdateSnakeBody();
            }
        }
    }
}
