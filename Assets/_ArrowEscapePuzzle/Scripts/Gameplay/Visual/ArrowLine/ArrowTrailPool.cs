using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal sealed class ArrowTrailPool
    {
        private readonly TrailRenderer _primaryTrail;
        private readonly List<TrailRenderer> _extraTrails = new List<TrailRenderer>();
        private int _activeSegmentIndex = -1;

        public ArrowTrailPool(TrailRenderer primaryTrail)
        {
            _primaryTrail = primaryTrail;
        }

        public void UpdateEscapeTrail(bool segmented, int segmentIndex, Vector3 localPosition)
        {
            if (_primaryTrail == null) return;

            if (segmented)
            {
                if (segmentIndex < 0) return;

                if (segmentIndex != _activeSegmentIndex)
                {
                    StopAll();
                    TrailRenderer activeTrail = GetOrCreateSegmentTrail(segmentIndex);
                    if (activeTrail != null)
                    {
                        activeTrail.transform.localPosition = localPosition;
                        activeTrail.Clear();
                        activeTrail.emitting = true;
                    }

                    _activeSegmentIndex = segmentIndex;
                    return;
                }

                TrailRenderer currentTrail = GetOrCreateSegmentTrail(segmentIndex);
                if (currentTrail != null)
                {
                    currentTrail.transform.localPosition = localPosition;
                }

                return;
            }

            _activeSegmentIndex = -1;
            if (!_primaryTrail.emitting)
            {
                _primaryTrail.Clear();
                _primaryTrail.emitting = true;
            }

            _primaryTrail.transform.localPosition = localPosition;
        }

        public void ResetIdlePosition(Vector3 localPosition)
        {
            _activeSegmentIndex = -1;
            if (_primaryTrail == null) return;

            _primaryTrail.emitting = false;
            _primaryTrail.transform.localPosition = localPosition;
        }

        public void StopAll()
        {
            if (_primaryTrail != null)
            {
                _primaryTrail.emitting = false;
            }

            for (int i = 0; i < _extraTrails.Count; i++)
            {
                if (_extraTrails[i] != null)
                {
                    _extraTrails[i].emitting = false;
                }
            }
        }

        public void ClearAll()
        {
            _activeSegmentIndex = -1;

            if (_primaryTrail != null)
            {
                _primaryTrail.emitting = false;
                _primaryTrail.Clear();
            }

            for (int i = 0; i < _extraTrails.Count; i++)
            {
                TrailRenderer trail = _extraTrails[i];
                if (trail == null) continue;
                trail.emitting = false;
                trail.Clear();
            }
        }

        public void ForEachTrail(Action<TrailRenderer> action)
        {
            if (_primaryTrail != null)
            {
                action(_primaryTrail);
            }

            for (int i = 0; i < _extraTrails.Count; i++)
            {
                if (_extraTrails[i] != null)
                {
                    action(_extraTrails[i]);
                }
            }
        }

        private TrailRenderer GetOrCreateSegmentTrail(int segmentIndex)
        {
            if (_primaryTrail == null) return null;
            if (segmentIndex <= 0) return _primaryTrail;

            int slot = segmentIndex - 1;
            while (_extraTrails.Count <= slot)
            {
                TrailRenderer clone = UnityEngine.Object.Instantiate(_primaryTrail, _primaryTrail.transform.parent);
                clone.name = _primaryTrail.name + "_Extra_" + _extraTrails.Count;
                clone.emitting = false;
                clone.Clear();
                _extraTrails.Add(clone);
            }

            return _extraTrails[slot];
        }
    }
}
