using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PipeImpactAudio : MonoBehaviour
    {
        [SerializeField] AudioSource impactSource;
        [SerializeField] AudioClip concreteClip;
        [SerializeField] AudioClip metalClip;
        [SerializeField, Min(0.01f)] float minimumSpeed = 0.18f;
        [SerializeField, Min(0.02f)] float retriggerSeconds = 0.12f;
        Rigidbody body;
        Vector3 previousVelocity, previousAngularVelocity;
        float nextImpactTime;
        public int ImpactCount { get; private set; }
        public PipeImpactSurface.SurfaceKind LastSurface { get; private set; }
        public bool IsArmed { get; set; }
        public event System.Action ImpactPlayed;

        void Awake() { body = GetComponent<Rigidbody>(); }
        void FixedUpdate()
        {
            previousVelocity = body.linearVelocity;
            previousAngularVelocity = body.angularVelocity;
        }
        void OnCollisionEnter(Collision collision)
        {
            if (!IsArmed || collision.contactCount == 0) return;
            var surface = collision.collider.GetComponentInParent<PipeImpactSurface>();
            if (surface == null) return;
            var contact = collision.GetContact(0);
            // Include rotation: a swung tip can move quickly while the grip barely translates.
            var tipVelocity = previousVelocity + Vector3.Cross(previousAngularVelocity, contact.point - body.worldCenterOfMass);
            float speed = Mathf.Max(collision.relativeVelocity.magnitude, Mathf.Abs(Vector3.Dot(tipVelocity, contact.normal)));
            TryPlayImpact(surface, contact.point, speed);
        }
        public bool TryPlayImpact(PipeImpactSurface surface, Vector3 point, float speed)
        {
            if (!IsArmed || surface == null || speed < minimumSpeed || Time.time < nextImpactTime || impactSource == null) return false;
            var clip = surface.Kind == PipeImpactSurface.SurfaceKind.Metal ? metalClip : concreteClip;
            if (clip == null) return false;
            nextImpactTime = Time.time + retriggerSeconds;
            // An emitter on the pipe (or an ancestor) must never teleport the physical prop.
            if(!transform.IsChildOf(impactSource.transform))impactSource.transform.position=point;
            impactSource.pitch = Mathf.Lerp(0.93f, 1.07f, Mathf.InverseLerp(minimumSpeed, 2.6f, speed));
            impactSource.PlayOneShot(clip, Mathf.Lerp(0.16f, 0.85f, Mathf.InverseLerp(minimumSpeed, 2.6f, speed)));
            LastSurface = surface.Kind; ImpactCount++;
            ImpactPlayed?.Invoke();
            return true;
        }
        public void ResetAudio()
        {
            IsArmed = false; ImpactCount = 0; nextImpactTime = 0f;
            previousVelocity = previousAngularVelocity = Vector3.zero;
            if (impactSource != null) impactSource.Stop();
        }
    }
}
