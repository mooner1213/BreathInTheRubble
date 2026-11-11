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
        [SerializeField] Vector3 attachmentPoint;
        [SerializeField] Vector3 attachmentAxis = Vector3.right;
        [SerializeField] Vector3 releaseVelocity;
        [SerializeField] Vector3 releaseAngularVelocity;
        [SerializeField, Min(0f)] float delaySeconds;
        [SerializeField, Min(0.05f)] float attachmentSeconds = 0.18f;
        [SerializeField] float peelDegrees = 5f;
        [SerializeField] bool colliderWhileAttached = true;
        [SerializeField, Min(0f)] float headClearanceRadius = .34f;
        [SerializeField, Min(0f)] float handClearanceRadius = .14f;
        Rigidbody body;
        Collider shape;
        Renderer surface;
        Transform originalParent;
        Vector3 originalPosition;
        Vector3 originalScale;
        Quaternion originalRotation;
        bool initialized;
        bool released;
        public bool Released => released;

        void Awake() { Initialize(); ResetEffect(); }
        void Initialize()
        {
            if (initialized) return;
            body = GetComponent<Rigidbody>();
            var fittedBox = GetComponent<BoxCollider>();
            shape = fittedBox != null ? (Collider)fittedBox : GetComponent<MeshCollider>();
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
            var velocity = originalParent.TransformDirection(releaseVelocity);
            var spin = originalParent.TransformDirection(releaseAngularVelocity);
            transform.SetParent(detachedRoot, true);
            shape.enabled = true;
            foreach (var other in ignoreColliders)
                if (other != null && other != shape) Physics.IgnoreCollision(shape, other, true);
            body.isKinematic = false; body.useGravity = true;
            if (shape is BoxCollider) body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = velocity; body.angularVelocity = spin;
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
            body.AddForce(away.normalized*Mathf.Clamp01((radius-gap)/radius)*12f,ForceMode.Acceleration);
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
            shape.enabled = colliderWhileAttached; if (surface != null) surface.enabled = true;
            // Reapply after a safety hide/re-enable; collider activation can reset pair filtering.
            foreach (var other in ignoreColliders)
                if (other != null && other != shape) Physics.IgnoreCollision(shape, other, true);
            released = false;
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
