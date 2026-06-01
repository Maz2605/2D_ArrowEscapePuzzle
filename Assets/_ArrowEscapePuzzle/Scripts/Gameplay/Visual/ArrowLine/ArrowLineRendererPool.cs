using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal sealed class ArrowLineRendererPool
    {
        private readonly ArrowLineViewContext _context;
        private readonly List<LineRenderer> _extraBodyRenderers = new List<LineRenderer>();
        private readonly List<LineRenderer> _extraDirectionRenderers = new List<LineRenderer>();
        private readonly List<LineRenderer> _extraSecondaryDirectionRenderers = new List<LineRenderer>();

        public ArrowLineRendererPool(ArrowLineViewContext context)
        {
            _context = context;
        }

        public LineRenderer GetBodyRenderer(int slot)
        {
            return slot == 0
                ? _context.BodyRenderer
                : GetOrCreateExtraRenderer(_extraBodyRenderers, _context.BodyRenderer, slot - 1, false);
        }

        public LineRenderer GetDirectionRenderer(int slot, bool secondary)
        {
            LineRenderer primary = secondary ? _context.SecondaryDirectionRenderer : _context.PrimaryDirectionRenderer;
            List<LineRenderer> extras = secondary ? _extraSecondaryDirectionRenderers : _extraDirectionRenderers;
            return slot == 0 ? primary : GetOrCreateExtraRenderer(extras, primary, slot - 1, true);
        }

        public void DisableUnusedBodyRenderers(int activeCount)
        {
            if (_context.BodyRenderer != null && activeCount == 0)
            {
                _context.BodyRenderer.positionCount = 0;
            }

            DisableRenderersFrom(_extraBodyRenderers, Mathf.Max(0, activeCount - 1));
        }

        public void DisableUnusedDirectionRenderers(bool secondary, int activeCount)
        {
            LineRenderer primary = secondary ? _context.SecondaryDirectionRenderer : _context.PrimaryDirectionRenderer;
            if (primary != null && activeCount == 0)
            {
                primary.positionCount = 0;
            }

            DisableRenderersFrom(secondary ? _extraSecondaryDirectionRenderers : _extraDirectionRenderers,
                Mathf.Max(0, activeCount - 1));
        }

        public void DisableExtraDirectionRenderers(bool secondary)
        {
            DisableRenderersFrom(secondary ? _extraSecondaryDirectionRenderers : _extraDirectionRenderers, 0);
        }

        public void ClearBodyRenderers()
        {
            DisableRenderer(_context.BodyRenderer);
            DisableRenderersFrom(_extraBodyRenderers, 0);
        }

        public void ClearDirectionRenderers()
        {
            DisableRenderer(_context.PrimaryDirectionRenderer);
            DisableRenderer(_context.SecondaryDirectionRenderer);
            DisableRenderersFrom(_extraDirectionRenderers, 0);
            DisableRenderersFrom(_extraSecondaryDirectionRenderers, 0);
        }

        public void ClearAll()
        {
            ClearBodyRenderers();
            ClearDirectionRenderers();
        }

        public void ForEachRenderer(Action<LineRenderer> action)
        {
            Invoke(action, _context.BodyRenderer);
            Invoke(action, _context.PrimaryDirectionRenderer);
            Invoke(action, _context.SecondaryDirectionRenderer);
            Invoke(action, _extraBodyRenderers);
            Invoke(action, _extraDirectionRenderers);
            Invoke(action, _extraSecondaryDirectionRenderers);
        }

        public void ForEachBodyRenderer(Action<LineRenderer> action)
        {
            Invoke(action, _context.BodyRenderer);
            Invoke(action, _extraBodyRenderers);
        }

        public void ForEachDirectionRenderer(Action<LineRenderer> action)
        {
            Invoke(action, _context.PrimaryDirectionRenderer);
            Invoke(action, _context.SecondaryDirectionRenderer);
            Invoke(action, _extraDirectionRenderers);
            Invoke(action, _extraSecondaryDirectionRenderers);
        }

        private static void Invoke(Action<LineRenderer> action, LineRenderer renderer)
        {
            if (renderer != null)
            {
                action(renderer);
            }
        }

        private static void Invoke(Action<LineRenderer> action, List<LineRenderer> renderers)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    action(renderers[i]);
                }
            }
        }

        private static LineRenderer GetOrCreateExtraRenderer(List<LineRenderer> cache, LineRenderer template, int slot, bool useWorldSpace)
        {
            if (template == null) return null;

            while (cache.Count <= slot)
            {
                LineRenderer clone = UnityEngine.Object.Instantiate(template, template.transform.parent);
                clone.name = template.name + "_Extra_" + cache.Count;
                clone.positionCount = 0;
                clone.enabled = false;
                cache.Add(clone);
            }

            LineRenderer renderer = cache[slot];
            if (renderer == null) return null;

            renderer.enabled = true;
            renderer.useWorldSpace = useWorldSpace;
            return renderer;
        }

        private static void DisableRenderer(LineRenderer renderer)
        {
            if (renderer == null) return;
            renderer.enabled = false;
            renderer.positionCount = 0;
        }

        private static void DisableRenderersFrom(List<LineRenderer> renderers, int activeCount)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                LineRenderer renderer = renderers[i];
                if (renderer == null) continue;

                bool shouldStayActive = i < activeCount;
                renderer.enabled = shouldStayActive;
                if (!shouldStayActive)
                {
                    renderer.positionCount = 0;
                }
            }
        }
    }
}
