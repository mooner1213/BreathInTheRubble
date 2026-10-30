using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BreathInTheRubble.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class PhoneFocusPresenter : MonoBehaviour
    {
        [Header("Focused Pose")]
        [SerializeField, Min(0.15f)] float distanceFromEyes = 0.30f;
        [SerializeField] float horizontalOffset = 0.045f;
        [SerializeField] float verticalOffset = -0.035f;
        [SerializeField, Min(0.01f)] float transitionSeconds = 0.22f;

        XRGrabInteractable grabInteractable;
        Rigidbody body;
        Camera playerCamera;
        Collider[] phoneColliders;
        bool[] restColliderEnabled;

        Vector3 restPosition;
        Quaternion restRotation;
        Vector3 transitionStartPosition;
        Quaternion transitionStartRotation;
        Vector3 transitionTargetPosition;
        Quaternion transitionTargetRotation;
        float transitionElapsed;
        bool restIsKinematic;
        bool restUseGravity;
        bool restDetectCollisions;
        bool hasRestPose;
        bool isTransitioning;
        bool transitionToFocused;
        bool isFocused;

        public bool IsFocused => isFocused;
        public Vector3 FocusedPosition => transitionTargetPosition;

        void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            phoneColliders = GetComponentsInChildren<Collider>(true);
            restColliderEnabled = new bool[phoneColliders.Length];
        }

        void OnEnable()
        {
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();
            if (body == null)
                body = GetComponent<Rigidbody>();

            grabInteractable.selectEntered.AddListener(OnPhoneSelected);
            grabInteractable.selectExited.AddListener(OnPhoneReleased);
        }

        void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnPhoneSelected);
                grabInteractable.selectExited.RemoveListener(OnPhoneReleased);
            }

            if (hasRestPose && isFocused)
                RestoreRestState(true);
        }

        void LateUpdate()
        {
            if (isTransitioning)
            {
                if (transitionToFocused)
                    UpdateFocusedTarget();

                transitionElapsed += Time.deltaTime;
                var amount = Mathf.Clamp01(transitionElapsed / transitionSeconds);
                var eased = Mathf.SmoothStep(0f, 1f, amount);
                ApplyPose(
                    Vector3.Lerp(transitionStartPosition, transitionTargetPosition, eased),
                    Quaternion.Slerp(transitionStartRotation, transitionTargetRotation, eased));

                if (amount >= 1f)
                    CompleteTransition();
            }
            else if (isFocused)
            {
                UpdateFocusedTarget();
                KeepFocusedAndStable();
            }
        }

        public void ToggleFocus()
        {
            if (isFocused)
                ReturnToRest(false);
            else
                FocusNow(false);
        }

        public void FocusNow(bool immediate)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("Phone focus requires a Main Camera.", this);
                return;
            }

            if (!hasRestPose || !isFocused)
                CaptureRestState();

            isFocused = true;
            transitionToFocused = true;
            UpdateFocusedTarget();
            BeginTransition(immediate);
        }

        public void ReturnToRest(bool immediate)
        {
            if (!hasRestPose)
                return;

            transitionToFocused = false;
            transitionTargetPosition = restPosition;
            transitionTargetRotation = restRotation;
            BeginTransition(immediate);
        }

        void OnPhoneSelected(SelectEnterEventArgs args)
        {
            ToggleFocus();
        }

        void OnPhoneReleased(SelectExitEventArgs args)
        {
            if (isFocused)
                KeepFocusedAndStable();
        }

        void CaptureRestState()
        {
            restPosition = transform.position;
            restRotation = transform.rotation;
            restIsKinematic = body.isKinematic;
            restUseGravity = body.useGravity;
            restDetectCollisions = body.detectCollisions;
            for (var index = 0; index < phoneColliders.Length; index++)
                restColliderEnabled[index] = phoneColliders[index] != null && phoneColliders[index].enabled;
            hasRestPose = true;
        }

        void BeginTransition(bool immediate)
        {
            transitionStartPosition = transform.position;
            transitionStartRotation = transform.rotation;
            transitionElapsed = 0f;
            isTransitioning = !immediate;
            PrepareFocusedPhysics();

            if (immediate)
            {
                ApplyPose(transitionTargetPosition, transitionTargetRotation);
                CompleteTransition();
            }
        }

        void CompleteTransition()
        {
            isTransitioning = false;
            ApplyPose(transitionTargetPosition, transitionTargetRotation);

            if (transitionToFocused)
            {
                isFocused = true;
                KeepFocusedAndStable();
            }
            else
            {
                RestoreRestState(false);
            }
        }

        void KeepFocusedAndStable()
        {
            PrepareFocusedPhysics();
            ApplyPose(transitionTargetPosition, transitionTargetRotation);
        }

        void PrepareFocusedPhysics()
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;
            SetPhoneColliders(false);
        }

        void RestoreRestState(bool forcePose)
        {
            isTransitioning = false;
            isFocused = false;
            if (forcePose)
                ApplyPose(restPosition, restRotation);

            body.detectCollisions = restDetectCollisions;
            body.useGravity = restUseGravity;
            body.isKinematic = restIsKinematic;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            for (var index = 0; index < phoneColliders.Length; index++)
            {
                if (phoneColliders[index] != null)
                    phoneColliders[index].enabled = restColliderEnabled[index];
            }
            hasRestPose = false;
        }

        void UpdateFocusedTarget()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
            if (playerCamera == null)
                return;

            var cameraTransform = playerCamera.transform;
            transitionTargetPosition = cameraTransform.position
                + cameraTransform.forward * distanceFromEyes
                + cameraTransform.right * horizontalOffset
                + cameraTransform.up * verticalOffset;
            transitionTargetRotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
        }

        void SetPhoneColliders(bool enabled)
        {
            foreach (var phoneCollider in phoneColliders)
            {
                if (phoneCollider != null)
                    phoneCollider.enabled = enabled;
            }
        }

        void ApplyPose(Vector3 position, Quaternion rotation)
        {
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
