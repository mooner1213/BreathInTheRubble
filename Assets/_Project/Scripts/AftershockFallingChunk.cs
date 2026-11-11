using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Fails at an attachment edge, then falls and rolls against existing rubble.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(MeshCollider))]
    public sealed class AftershockFallingChunk : MonoBehaviour
    {
        [SerializeField] Transform detachedRoot;
        [SerializeField] Collider[] ignoreColliders = System.Array.Empty<Collider>();
        [SerializeField] Transform head;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform rightHand;
        [SerializeField, Min(0f)] float delaySeconds;
        [SerializeField, Min(0.05f)] float attachmentSeconds = 0.18f;
        [SerializeField] bool colliderWhileAttached = true;
        [SerializeField, Min(0f)] float headClearanceRadius = .34f;
        [SerializeField, Min(0f)] float handClearanceRadius = .14f;
        Rigidbody body;
        Collider shape;
        Collider[] contactShapes;
        Renderer surface;
        Transform originalParent;
        Vector3 originalPosition;
        Vector3 originalScale;
        Quaternion originalRotation;
        bool initialized;
        bool released;
        public bool Released => released;
        public Vector3 ReleaseVelocity { get; private set; }

        void Awake() { Initialize(); ResetEffect(); }
        void Initialize()
        {
            if (initialized) return;
            body = GetComponent<Rigidbody>();
            var fittedBox = GetComponent<BoxCollider>();
            shape = fittedBox != null ? (Collider)fittedBox : GetComponent<MeshCollider>();
            contactShapes = fittedBox != null ? GetComponents<BoxCollider>() : new Collider[]{shape};
            surface = GetComponent<Renderer>();
            originalParent = transform.parent;
            originalPosition = transform.localPosition; originalRotation = transform.localRotation; originalScale = transform.localScale;
            initialized = true;
        }
        public void Apply(float sinceFracture)
        {
            Initialize();
            if (released || originalParent == null) return;
            float since = sinceFracture - delaySeconds;
            // The attachment holds at its real rest pose. Once it fails, only PhysX moves it.
            // Rotating a kinematic mesh around an arbitrary point made it look suspended.
            if (since < attachmentSeconds) return;
            ReleaseFromSupport(Vector3.zero, Vector3.zero);
        }
        public void ReleaseFromSupport(Vector3 velocity, Vector3 spin)
        {
            Initialize();if(released)return;
            transform.SetParent(detachedRoot, true);
            foreach(var contact in contactShapes)
            {
                contact.enabled=true;
                foreach (var other in ignoreColliders)
                    if (other != null && other != contact) Physics.IgnoreCollision(contact, other, true);
            }
            body.isKinematic = false; body.useGravity = true;
            // Embedded collision hulls must be fitted in the scene, not explosively separated by PhysX.
            body.maxDepenetrationVelocity = .18f;
            if (shape is BoxCollider) body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ReleaseVelocity=Vector3.ClampMagnitude(velocity,.16f);
            body.linearVelocity = ReleaseVelocity; body.angularVelocity = Vector3.ClampMagnitude(spin,.5f);
            body.WakeUp();
            released = true;
        }
        void FixedUpdate()
        {
            if (!released || surface == null || body.isKinematic || body.IsSleeping()) return;
            // Leave fallen rubble visible. Steer a falling piece around tracked poses rather
            // than erasing it when a controller happens to pass near its resting bounds.
            KeepClear(head, headClearanceRadius); KeepClear(leftHand, handClearanceRadius); KeepClear(rightHand, handClearanceRadius);
        }
        void KeepClear(Transform target, float radius)
        {
            if (target == null || radius <= 0f || surface.bounds.max.y < target.position.y-.18f) return;
            float gap=Vector3.Distance(surface.bounds.ClosestPoint(target.position),target.position);
            if(gap>=radius)return;
            var away=Vector3.ProjectOnPlane(surface.bounds.center-target.position,Vector3.up);
            if(away.sqrMagnitude<.0001f)away=Vector3.forward;
            // Keep the safety response gentle; the tracked contact collider handles actual contact.
            float outward=Vector3.Dot(body.linearVelocity,away.normalized);
            if(outward<.12f)body.AddForce(away.normalized*Mathf.Clamp01((radius-gap)/radius)*.6f,ForceMode.Acceleration);
        }
        public void ResetEffect()
        {
            Initialize();
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.useGravity = false;
            if(body.collisionDetectionMode==CollisionDetectionMode.ContinuousDynamic)body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;
            if (!Application.isPlaying || !gameObject.activeInHierarchy || (originalParent != null && !originalParent.gameObject.activeInHierarchy)) return;
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.localPosition = originalPosition; transform.localRotation = originalRotation; transform.localScale = originalScale;
                // Detaching preserves world scale. Restore local scale before teleporting the
                // body so repeated previews cannot multiply the former parent's scale.
                body.position = transform.position; body.rotation = transform.rotation;
            }
            foreach(var contact in contactShapes)contact.enabled=colliderWhileAttached;
            if (surface != null) surface.enabled = true;
            // Reapply after a safety hide/re-enable; collider activation can reset pair filtering.
            foreach(var contact in contactShapes)foreach (var other in ignoreColliders)
                if (other != null && other != contact) Physics.IgnoreCollision(contact, other, true);
            released = false;ReleaseVelocity=Vector3.zero;
        }
        void OnDisable()
        {
            // Reparenting is unsafe during scene teardown.
            if (body != null)
            {
                body.useGravity = false;
                if(body.collisionDetectionMode==CollisionDetectionMode.ContinuousDynamic)body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                body.isKinematic = true;
            }
        }
    }
}
