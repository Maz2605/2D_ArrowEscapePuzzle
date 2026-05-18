using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual.GridComponents
{
    public class ArrowLinkVisuals : MonoBehaviour
    {
        [Header("--- REFERENCES ---")]
        [SerializeField] private ArrowLinkGroupView linkGroupPrefab;

        private readonly List<ArrowLinkGroupView> _activeLinkGroups = new List<ArrowLinkGroupView>();
        private readonly Dictionary<string, HashSet<ArrowLinkGroupView>> _linksByArrowId =
            new Dictionary<string, HashSet<ArrowLinkGroupView>>();

        private Transform _linkRoot;

        public void Initialize(Transform linkRoot)
        {
            _linkRoot = linkRoot;
            enabled = false;
        }

        public void RebuildLinks(GridSystem logic, IReadOnlyDictionary<string, ArrowLineView> activeLines)
        {
            ClearLinks();
            if (_linkRoot == null || linkGroupPrefab == null || logic == null || activeLines == null) return;

            List<List<string>> linkedGroups = logic.GetLinkedGroups();
            if (linkedGroups == null) return;

            for (int i = 0; i < linkedGroups.Count; i++)
            {
                List<string> groupIds = linkedGroups[i];
                List<ArrowLineView> viewsInGroup = new List<ArrowLineView>(groupIds.Count);

                for (int j = 0; j < groupIds.Count; j++)
                {
                    string id = groupIds[j];
                    if (activeLines.TryGetValue(id, out ArrowLineView view) && view != null)
                    {
                        viewsInGroup.Add(view);
                    }
                }

                if (viewsInGroup.Count < 2) continue;

                ArrowLinkGroupView linkView = PoolingManager.Instance.Spawn(linkGroupPrefab, Vector3.zero, Quaternion.identity, _linkRoot);
                linkView.Setup(viewsInGroup);
                _activeLinkGroups.Add(linkView);

                for (int j = 0; j < groupIds.Count; j++)
                {
                    RegisterLink(groupIds[j], linkView);
                }
            }

            enabled = _activeLinkGroups.Count > 0;
        }

        public void RemoveArrow(string arrowId, ArrowLineView view)
        {
            if (view == null || string.IsNullOrEmpty(arrowId)) return;
            if (!_linksByArrowId.TryGetValue(arrowId, out HashSet<ArrowLinkGroupView> linkSet)) return;

            List<ArrowLinkGroupView> pendingCleanup = new List<ArrowLinkGroupView>(linkSet);
            for (int i = 0; i < pendingCleanup.Count; i++)
            {
                ArrowLinkGroupView link = pendingCleanup[i];
                if (link == null)
                {
                    continue;
                }

                link.RemoveArrow(view);
                if (!link.gameObject.activeInHierarchy || link.ArrowCount < 2)
                {
                    UnregisterLinkGroup(link);
                }
                else
                {
                    link.RefreshColors();
                }
            }

            _linksByArrowId.Remove(arrowId);
            enabled = _activeLinkGroups.Count > 0;
        }

        public void RefreshColors()
        {
            for (int i = _activeLinkGroups.Count - 1; i >= 0; i--)
            {
                ArrowLinkGroupView link = _activeLinkGroups[i];
                if (link == null || !link.gameObject.activeInHierarchy)
                {
                    _activeLinkGroups.RemoveAt(i);
                    continue;
                }

                link.RefreshColors();
            }

            enabled = _activeLinkGroups.Count > 0;
        }

        private void LateUpdate()
        {
            for (int i = _activeLinkGroups.Count - 1; i >= 0; i--)
            {
                ArrowLinkGroupView link = _activeLinkGroups[i];
                if (link == null || !link.gameObject.activeInHierarchy)
                {
                    UnregisterLinkGroup(link);
                    continue;
                }

                link.RefreshPositionsIfNeeded();
            }

            enabled = _activeLinkGroups.Count > 0;
        }

        private void RegisterLink(string arrowId, ArrowLinkGroupView linkView)
        {
            if (!_linksByArrowId.TryGetValue(arrowId, out HashSet<ArrowLinkGroupView> linkSet))
            {
                linkSet = new HashSet<ArrowLinkGroupView>();
                _linksByArrowId[arrowId] = linkSet;
            }

            linkSet.Add(linkView);
        }

        private void UnregisterLinkGroup(ArrowLinkGroupView linkView)
        {
            if (linkView == null)
            {
                return;
            }

            _activeLinkGroups.Remove(linkView);

            List<string> emptyKeys = null;
            foreach (KeyValuePair<string, HashSet<ArrowLinkGroupView>> kvp in _linksByArrowId)
            {
                if (!kvp.Value.Remove(linkView) || kvp.Value.Count > 0) continue;
                emptyKeys ??= new List<string>();
                emptyKeys.Add(kvp.Key);
            }

            if (emptyKeys != null)
            {
                for (int i = 0; i < emptyKeys.Count; i++)
                {
                    _linksByArrowId.Remove(emptyKeys[i]);
                }
            }
        }

        private void ClearLinks()
        {
            for (int i = _activeLinkGroups.Count - 1; i >= 0; i--)
            {
                ArrowLinkGroupView link = _activeLinkGroups[i];
                if (link != null)
                {
                    PoolingManager.Instance.Despawn(link.gameObject);
                }
            }

            _activeLinkGroups.Clear();
            _linksByArrowId.Clear();
            enabled = false;
        }
    }
}
