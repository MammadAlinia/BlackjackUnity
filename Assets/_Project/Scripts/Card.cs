using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace _Project.Scripts
{
    public class Card : MonoBehaviour
    {
        Material material;
        MotionHandle flip;
        Vector3 originalScale;
        public int myCardIndex;
        public MeshRenderer meshRenderer;
        public void Init(Texture2D texture, Texture2D hiddenTexture, int cardIndex)
        {
            myCardIndex = cardIndex;
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            material = meshRenderer.material;
            originalScale = meshRenderer.transform.localScale;
            material.mainTexture = cardIndex == -1 ? hiddenTexture : texture;
            gameObject.name = cardIndex == -1 ? "Hidden card" : texture.name;
        }
        public void Reveal(Texture2D texture, int index)
        {
            if (myCardIndex == index) return;
            flip.TryCancel();
            flip = LMotion.Create(0f, 1f, 0.25f).Bind(progress =>
            {
                meshRenderer.transform.localScale = new Vector3(originalScale.x * Mathf.Abs(1f - 2f * progress), originalScale.y, originalScale.z);
                if (progress >= 0.5f) material.mainTexture = texture;
            }).AddTo(this);
            myCardIndex = index;
            gameObject.name = texture.name;
        }
        void OnDestroy() { flip.TryCancel(); if (material != null) Destroy(material); }
    }
}
