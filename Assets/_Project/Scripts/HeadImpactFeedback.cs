using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    public sealed class HeadImpactFeedback : MonoBehaviour
    {
        [SerializeField] MeshRenderer edgeRenderer;
        [SerializeField, Range(0f, 0.65f)] float strength = 0.48f;
        [SerializeField, Min(0f)] float holdSeconds = 0.5f;
        [SerializeField, Min(0.05f)] float fadeSeconds = 1.3f;
        MaterialPropertyBlock properties;
        float elapsed = 100f;
        public float Opacity { get; private set; }
        void Awake() { properties = new MaterialPropertyBlock(); Clear(); }
        void Update() { Advance(Time.deltaTime); }
        public void PlayHit() { elapsed = 0f; Apply(strength); }
        public void Advance(float seconds)
        {
            elapsed += Mathf.Max(0f, seconds);
            Apply(elapsed <= holdSeconds ? strength : strength * (1f - Mathf.Clamp01((elapsed - holdSeconds) / fadeSeconds)));
        }
        public void Clear() { elapsed = holdSeconds + fadeSeconds + 1f; Apply(0f); }
        void Apply(float opacity)
        {
            Opacity = opacity;
            if (edgeRenderer == null) return;
            edgeRenderer.enabled = opacity > 0.001f;
            if (!edgeRenderer.enabled) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetFloat("_Opacity", opacity);edgeRenderer.SetPropertyBlock(properties);
        }
        void OnDisable() { Clear(); }
    }
}
