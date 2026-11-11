using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>A single opening-eye fade. Changes only an overlay, never the tracked camera pose.</summary>
    [DisallowMultipleComponent]
    public sealed class AwakeningReveal : MonoBehaviour
    {
        [SerializeField] MeshRenderer eyelidRenderer;
        [SerializeField, Min(0f)] float closedSeconds = 0.6f;
        [SerializeField, Min(0.1f)] float openingSeconds = 3.4f;
        MaterialPropertyBlock properties;
        float elapsed;
        public float Opening { get; private set; }
        public bool IsComplete => Opening >= 1f;
        public float DurationSeconds => closedSeconds + openingSeconds;

        void OnEnable() { Replay(); }
        void Update() { Advance(Time.deltaTime); }
        [ContextMenu("Replay Opening Eyes (Play Mode)")]
        public void Replay() { elapsed = 0f; Apply(0f); }
        public void Advance(float seconds)
        {
            elapsed += Mathf.Max(0f, seconds);
            Apply(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - closedSeconds) / openingSeconds)));
        }
        public void Complete() { elapsed = DurationSeconds; Apply(1f); }
        void Apply(float opening)
        {
            Opening = opening;
            if (eyelidRenderer == null) return;
            eyelidRenderer.enabled = !IsComplete;
            if (IsComplete) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetFloat("_Opening", opening);
            eyelidRenderer.SetPropertyBlock(properties);
        }
        void OnDisable() { if (eyelidRenderer != null) eyelidRenderer.enabled = false; }
    }
}
