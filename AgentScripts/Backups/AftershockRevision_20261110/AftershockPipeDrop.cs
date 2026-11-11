using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Authored ceiling release, followed by the same physical prop used for grabbing and striking.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class AftershockPipeDrop : MonoBehaviour
    {
        [SerializeField] AftershockDirector aftershock;
        [SerializeField] Transform stowedPose;
        [SerializeField] Transform lipPose;
        [SerializeField] Transform releasePose;
        [SerializeField] Transform landingPose;
        [SerializeField] Transform head;
        [SerializeField, Tooltip("Hand the pipe to physics once it clears the ceiling seam.")] bool resolveLandingOnRubble;
        [SerializeField] Collider[] ignoredColliders = System.Array.Empty<Collider>();
        [SerializeField] AudioSource movementSource;
        [SerializeField] AudioClip slideClip;
        [SerializeField] AudioClip landingClip;
        [SerializeField, Min(0f)] float startsAtSeconds = 4.95f;
        [SerializeField, Min(0.1f)] float slideSeconds = 1.6f;
        [SerializeField, Min(0.1f)] float tipSeconds = 0.5f;
        [SerializeField, Min(0.1f)] float fallSeconds = 0.4f;
        [SerializeField, Min(0.1f)] float settleSeconds = 0.24f;
        Rigidbody body;
        XRGrabInteractable grab;
        PipeImpactAudio impacts;
        Collider shape;
        Renderer[] surfaces;
        bool initialized, started, landingSoundPlayed;
        bool waitingForFirstGrab;
        RigidbodyConstraints freeConstraints;
        float originalLinearDamping, originalAngularDamping;
        bool pendingMotion;
        float pendingElapsed;
        public bool IsAvailable { get; private set; }
        public bool IsRevealed => started;
        public float AvailableAtSeconds => startsAtSeconds + slideSeconds + tipSeconds + (resolveLandingOnRubble ? 0f : fallSeconds + settleSeconds);
        public Transform LandingPose => landingPose;
        public event System.Action Resetting;

        void Awake() { Initialize(); ResetEffect(); }
        void Initialize()
        {
            if (initialized) return;
            body = GetComponent<Rigidbody>(); grab = GetComponent<XRGrabInteractable>();
            impacts = GetComponent<PipeImpactAudio>(); shape = GetComponent<Collider>();
            surfaces = GetComponentsInChildren<Renderer>(true); freeConstraints = body.constraints; initialized = true;
            originalLinearDamping=body.linearDamping;originalAngularDamping=body.angularDamping;
        }
        void OnEnable()
        {
            Initialize(); grab.selectEntered.AddListener(OnGrabbed);
            if (aftershock == null) return;
            aftershock.Advanced += Apply; aftershock.EffectReset += ResetEffect;
        }
        void OnDisable()
        {
            if (grab != null) grab.selectEntered.RemoveListener(OnGrabbed);
            if (aftershock == null) return;
            aftershock.Advanced -= Apply; aftershock.EffectReset -= ResetEffect;
        }
        public void ResetEffect()
        {
            Initialize();
            pendingMotion=false;
            Resetting?.Invoke();
            if (grab.isSelected && grab.interactionManager != null) grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            grab.enabled = false; shape.enabled = false;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            if(body.collisionDetectionMode==CollisionDetectionMode.ContinuousDynamic)body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true; body.useGravity = false;
            body.linearDamping=originalLinearDamping;body.angularDamping=originalAngularDamping;
            started = landingSoundPlayed = IsAvailable = waitingForFirstGrab = false;
            body.constraints = freeConstraints;
            // Rubble conceals the stowed prop; it never pops into the visible cavity.
            foreach (var surface in surfaces) surface.enabled = true;
            if (stowedPose != null) SetPose(stowedPose.position, stowedPose.rotation);
            if (impacts != null) impacts.ResetAudio();
            if (movementSource != null) movementSource.Stop();
        }
        public void Apply(float elapsed)
        {
            pendingElapsed=elapsed;pendingMotion=true;
        }
        void FixedUpdate(){if(pendingMotion){pendingMotion=false;ApplyMotion(pendingElapsed);}}
        void ApplyMotion(float elapsed)
        {
            Initialize();
            if (stowedPose == null || lipPose == null || releasePose == null || landingPose == null) return;
            if (IsAvailable)
            {
                // The bearing itself still rattles after landing. Keep the untouched pipe
                // seated on that surface until pickup, instead of freezing it in world space.
                if (!resolveLandingOnRubble && waitingForFirstGrab && !grab.isSelected) SetPose(landingPose.position, landingPose.rotation);
                return;
            }
            float since = elapsed - startsAtSeconds;
            if (since < 0f) { MoveAuthoredPose(stowedPose.position, stowedPose.rotation); return; }
            if (!started)
            {
                started = true;
                if(resolveLandingOnRubble){shape.enabled=true;foreach(var other in ignoredColliders)if(other!=null&&other!=shape)Physics.IgnoreCollision(shape,other);}
                if (movementSource != null && slideClip != null) movementSource.PlayOneShot(slideClip, 0.5f);
            }
            Vector3 position; Quaternion rotation;
            if(resolveLandingOnRubble&&since>=slideSeconds+tipSeconds)
            {
                BeginPhysicalFall(body.position,body.rotation);return;
            }
            if (since < slideSeconds)
            {
                float t = since / slideSeconds; t = t * t;
                position = Vector3.Lerp(stowedPose.position, lipPose.position, t);
                rotation = Quaternion.Slerp(stowedPose.rotation, lipPose.rotation, t);
            }
            else if (since < slideSeconds + tipSeconds)
            {
                float t = Mathf.SmoothStep(0f, 1f, (since - slideSeconds) / tipSeconds);
                position = Vector3.Lerp(lipPose.position, releasePose.position, t);
                rotation = Quaternion.Slerp(lipPose.rotation, releasePose.rotation, t);
            }
            else
            {
                float t = Mathf.Clamp01((since - slideSeconds - tipSeconds) / fallSeconds);
                position = Vector3.Lerp(releasePose.position, landingPose.position, t * t);
                rotation = Quaternion.Slerp(releasePose.rotation, landingPose.rotation, t);
                if (t >= 1f)
                {
                    float bounce = Mathf.Clamp01((since - slideSeconds - tipSeconds - fallSeconds) / settleSeconds);
                    // Settle above the contact pose. A full sine wave dipped through the
                    // visible bearing slab during the second half of the landing.
                    position += Vector3.up * (Mathf.Sin(bounce * Mathf.PI) * (1f - bounce) * 0.009f);
                    if (!landingSoundPlayed)
                    {
                        landingSoundPlayed = true;
                        if (movementSource != null && landingClip != null) movementSource.PlayOneShot(landingClip, 0.7f);
                    }
                }
            }
            // Keep the release away from a head leaning into the authored path; never move tracking.
            if (head != null && since < slideSeconds + tipSeconds + fallSeconds)
            {
                var away = new Vector2(position.x - head.position.x, position.z - head.position.z);
                if (away.magnitude < 0.29f && position.y > head.position.y - 0.2f)
                {
                    if (away.sqrMagnitude < 0.0001f) away = Vector2.up;
                    away = away.normalized * 0.29f;
                    position.x = head.position.x + away.x; position.z = head.position.z + away.y;
                }
            }
            if(resolveLandingOnRubble&&BlockedPose(position,rotation)){BeginPhysicalFall(body.position,body.rotation);return;}
            MoveAuthoredPose(position, rotation);
            if (since < slideSeconds + tipSeconds + fallSeconds + settleSeconds) return;
            IsAvailable = true; shape.enabled = true;
            foreach (var other in ignoredColliders) if (other != null && other != shape) Physics.IgnoreCollision(shape, other);
            body.isKinematic = false; body.useGravity = true;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            // A round pipe rolls down irregular rubble indefinitely. Keep its authored rest
            // until the first pickup; both XR and the PC adapter release this constraint.
            waitingForFirstGrab = true; body.constraints = RigidbodyConstraints.FreezeAll;
            grab.enabled = true;
            if (impacts != null) impacts.IsArmed = true;
        }
        void OnGrabbed(SelectEnterEventArgs args) { ReleaseRestingConstraint(); }
        bool BlockedPose(Vector3 position,Quaternion rotation)
        {
            var capsule=shape as CapsuleCollider;if(capsule==null)return false;var scale=transform.lossyScale;
            var center=position+rotation*Vector3.Scale(capsule.center,scale);float radius=capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));var axis=rotation*Vector3.up*(capsule.height*.5f-capsule.radius)*Mathf.Abs(scale.y);
            foreach(var other in Physics.OverlapCapsule(center-axis,center+axis,radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if(other==shape||other.GetComponentInParent<XRGrabInteractable>()!=null)continue;
                var renderer=other.GetComponentInParent<MeshRenderer>();if(renderer==null||!renderer.enabled)continue;
                if(Physics.ComputePenetration(shape,position,rotation,other,other.transform.position,other.transform.rotation,out var direction,out float depth)&&depth>.0005f)return true;
            }
            return false;
        }
        void BeginPhysicalFall(Vector3 position,Quaternion rotation)
        {
            shape.enabled=true;foreach(var other in ignoredColliders)if(other!=null&&other!=shape)Physics.IgnoreCollision(shape,other);
            body.isKinematic=false;body.useGravity=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.constraints=freeConstraints;body.linearDamping=2.5f;body.angularDamping=5f;
            body.linearVelocity=Vector3.ProjectOnPlane(releasePose.position-lipPose.position,Vector3.up).normalized*2.5f+Vector3.down*.10f;body.angularVelocity=new Vector3(.5f,.2f,-.3f);body.WakeUp();
            IsAvailable=waitingForFirstGrab=true;grab.enabled=true;if(impacts!=null)impacts.IsArmed=true;
        }
        public void ReleaseRestingConstraint()
        {
            if (!waitingForFirstGrab) return;
            waitingForFirstGrab = false; body.constraints = freeConstraints;body.linearDamping=originalLinearDamping;body.angularDamping=originalAngularDamping; body.WakeUp();
        }
        void SetPose(Vector3 position, Quaternion rotation)
        {
            body.position = position; body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }
        void MoveAuthoredPose(Vector3 position,Quaternion rotation)
        {
            if(resolveLandingOnRubble&&body.isKinematic){body.MovePosition(position);body.MoveRotation(rotation);}
            else SetPose(position,rotation);
        }
    }
}
