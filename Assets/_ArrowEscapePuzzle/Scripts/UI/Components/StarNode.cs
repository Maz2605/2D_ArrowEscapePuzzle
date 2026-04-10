using System.Collections.Generic;
using ArrowGame.UI.VFX;
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
                StarGlow.transform.localRotation = Quaternion.identity; // Đưa góc xoay về 0
                StarGlow.color = new Color(1, 1, 1, 0); // Reset Alpha về tàng hình
            }

            if (GlintImages != null)
            {
                foreach (var glint in GlintImages)
                {
                    if (glint == null) continue;
                    glint.gameObject.SetActive(false);
                    glint.transform.localScale = Vector3.zero;
                    glint.color = new Color(1, 1, 1, 0); 
                }
            }
        }
    }
}