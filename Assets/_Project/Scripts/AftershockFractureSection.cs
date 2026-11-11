using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Concrete cut from an existing wall falls away to uncover fixed, embedded steel.</summary>
    public sealed class AftershockFractureSection : MonoBehaviour
    {
        [SerializeField] Transform wallAnchor;
        [SerializeField] AftershockFallingChunk[] fallingChunks = System.Array.Empty<AftershockFallingChunk>();
        [SerializeField] Transform exposedRebar;
        [SerializeField] ParticleSystem fractureDust;
        [SerializeField, Min(0f)] float opensAtSeconds = 10.2f;
        [SerializeField, Min(0.1f)] float openingSeconds = 0.65f;
        [SerializeField, Min(0)] int releaseDustCount = 12;
        Quaternion anchorRotation;
        bool emitted;
        public float Progress { get; private set; }
        public float OpensAtSeconds => opensAtSeconds;
        public bool RebarVisible => Progress > 0.25f && exposedRebar != null && exposedRebar.gameObject.activeInHierarchy;

        void Awake()
        {
            if (wallAnchor != null) anchorRotation = wallAnchor.rotation;
            ResetEffect();
        }
        public void Apply(float elapsed)
        {
            FollowWall();
            float since = elapsed - opensAtSeconds;
            Progress = Mathf.Clamp01(since / openingSeconds);
            foreach (var chunk in fallingChunks)
                if (chunk != null) chunk.Apply(since);
            if (!emitted && since >= 0f)
            {
                emitted = true;
                if (fractureDust != null) { fractureDust.Play(); fractureDust.Emit(releaseDustCount); }
            }
        }
        public void ResetEffect()
        {
            Progress = 0f; emitted = false;
            FollowWall();
            foreach (var chunk in fallingChunks)
                if (chunk != null) chunk.ResetEffect();
            // Steel stays in the wall. Before failure the concrete skin occludes it.
            if (exposedRebar != null) exposedRebar.gameObject.SetActive(true);
            if (fractureDust != null) fractureDust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void FollowWall()
        {
            if (wallAnchor != null)
                transform.SetPositionAndRotation(wallAnchor.position, wallAnchor.rotation * Quaternion.Inverse(anchorRotation));
        }
    }
}
