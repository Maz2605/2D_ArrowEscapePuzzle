using System.Collections.Generic;
using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Components
{
    public class StarNode : MonoBehaviour
    {
        [field: SerializeField] public RectTransform Container { get; private set; }
        [field: SerializeField] public Image FullStar { get; private set; }
        [field: SerializeField] public Image StarGlow { get; private set; }
        [field: SerializeField] public List<Image> GlintImages { get; private set; }

        [SerializeField] private UIParticle appearParticleHost;
        [SerializeField] private float appearParticleScaleDuration = 0.2f;
        [SerializeField] private float appearParticleFadeDuration = 0.16f;
        [SerializeField] private float appearParticleStartScale = 0.55f;
        [SerializeField] private float appearParticleOvershootScale = 1.08f;

        private readonly List<ParticleSystem> _appearParticles = new();

        private void Awake()
        {
            CacheAppearParticles();
        }

        private void OnDisable()
        {
            StopAndHideAppearParticle();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheAppearParticles();
        }
#endif

        public void ResetForAnimation()
        {
            if (Container != null) Container.localScale = Vector3.one;

            if (FullStar != null)
            {
                FullStar.gameObject.SetActive(false);
                FullStar.transform.localScale = Vector3.zero;
            }

            if (StarGlow != null)
            {
                StarGlow.gameObject.SetActive(false);
                StarGlow.transform.localScale = Vector3.zero;
                StarGlow.transform.localRotation = Quaternion.identity;
                StarGlow.color = new Color(1f, 1f, 1f, 0f);
            }

            StopAndHideAppearParticle();

            if (GlintImages == null) return;

            foreach (var glint in GlintImages)
            {
                if (glint == null) continue;
                glint.gameObject.SetActive(false);
                glint.transform.localScale = Vector3.zero;
                glint.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        public void PlayAppearParticle()
        {
            CacheAppearParticles();
            if (appearParticleHost == null || _appearParticles.Count == 0) return;

            DOTween.Kill(appearParticleHost);
            DOTween.Kill(appearParticleHost.rectTransform);

            appearParticleHost.gameObject.SetActive(true);
            appearParticleHost.rectTransform.localScale = Vector3.one * appearParticleStartScale;
            appearParticleHost.color = new Color(1f, 1f, 1f, 0f);
            appearParticleHost.RefreshParticles(_appearParticles);

            foreach (var particle in _appearParticles)
            {
                if (particle == null) continue;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(true);
            }

            appearParticleHost.Play();

            Sequence appearSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(appearParticleHost.gameObject);

            appearSequence.Append(appearParticleHost.rectTransform
                .DOScale(appearParticleOvershootScale, appearParticleScaleDuration)
                .SetEase(Ease.OutQuad));
            appearSequence.Append(appearParticleHost.rectTransform
                .DOScale(1f, 0.12f)
                .SetEase(Ease.OutBack));
            appearSequence.Join(appearParticleHost
                .DOFade(1f, appearParticleFadeDuration)
                .SetEase(Ease.OutSine));
        }

        private void CacheAppearParticles()
        {
            if (appearParticleHost == null)
            {
                appearParticleHost = GetComponentInChildren<UIParticle>(true);
            }

            _appearParticles.Clear();

            if (appearParticleHost != null)
            {
                appearParticleHost.GetComponentsInChildren(true, _appearParticles);
            }
        }

        private void StopAndHideAppearParticle()
        {
            CacheAppearParticles();
            if (appearParticleHost == null) return;

            DOTween.Kill(appearParticleHost);
            DOTween.Kill(appearParticleHost.rectTransform);

            appearParticleHost.Stop();
            appearParticleHost.Clear();
            appearParticleHost.color = new Color(1f, 1f, 1f, 0f);

            foreach (var particle in _appearParticles)
            {
                if (particle == null) continue;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Clear(true);
            }

            appearParticleHost.rectTransform.localScale = Vector3.zero;
            appearParticleHost.gameObject.SetActive(false);
        }
    }
}
