#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BreathInTheRubble.Prototype
{
    /// <summary>PC-only physical pipe preview: P holds/releases, J swings. Requires the existing desktop mode.</summary>
    [RequireComponent(typeof(AftershockPipeDrop))]
    public sealed class DesktopPipeTestDriver : MonoBehaviour
    {
        Rigidbody body;
        XRGrabInteractable grab;
        AftershockPipeDrop drop;
        DesktopPhoneTestDriver desktop;
        float swingElapsed = 1f;
        public bool IsHolding { get; private set; }
        void Awake()
        {
            body = GetComponent<Rigidbody>(); grab = GetComponent<XRGrabInteractable>(); drop = GetComponent<AftershockPipeDrop>();
            desktop = FindFirstObjectByType<DesktopPhoneTestDriver>();
        }
        bool CanPreview
        {
            get
            {
                // The existing PC window can create its phone adapter after the scene awakens.
                if (desktop == null) desktop = FindFirstObjectByType<DesktopPhoneTestDriver>();
                return desktop != null && desktop.DesktopEnabled && !DesktopPhoneTestDriver.HasPhysicalHmd && drop.IsAvailable;
            }
        }
        void OnEnable()
        {
            if (drop == null) drop = GetComponent<AftershockPipeDrop>();
            drop.Resetting += ReleasePreview;
        }
        void ReleasePreview() { if (IsHolding) SetHeld(false); }
        void Update()
        {
            if (!CanPreview) { if (IsHolding) SetHeld(false); return; }
            if (UnityEditor.EditorWindow.focusedWindow == null || UnityEditor.EditorWindow.focusedWindow.GetType().Name != "GameView") return;
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.pKey.wasPressedThisFrame) SetHeld(!IsHolding);
            if (keys.jKey.wasPressedThisFrame) Swing();
        }
        public void SetHeld(bool held)
        {
            if (held && (!CanPreview || grab.isSelected)) return;
            if (IsHolding == held) return;
            IsHolding = held;
            if (held)
            {
                drop.ReleaseRestingConstraint();grab.enabled=false;body.useGravity=false;swingElapsed=1f;
                // P is the editor's pickup shortcut; an untracked hand has no approach path.
                // Once attached, the normal physical servo and collisions drive every swing.
                if(Camera.main!=null){var view=Camera.main.transform;body.position=view.TransformPoint(new Vector3(.13f,-.17f,.23f));body.rotation=view.rotation*Quaternion.Euler(55f,-18f,-12f);body.linearVelocity=body.angularVelocity=Vector3.zero;}
            }
            else { body.useGravity = true; grab.enabled = drop.IsAvailable; }
        }
        public void Swing() { if (IsHolding && CanPreview) swingElapsed = 0f; }
        void FixedUpdate()
        {
            if (!IsHolding || !CanPreview || Camera.main == null) return;
            swingElapsed += Time.fixedDeltaTime;
            float strike = Mathf.Sin(Mathf.Clamp01(swingElapsed / 0.38f) * Mathf.PI);
            var view = Camera.main.transform;
            var target = view.TransformPoint(new Vector3(0.13f - strike * 0.05f, -0.17f + strike * 0.07f, 0.23f + strike * 0.14f));
            var rotation = view.rotation * Quaternion.Euler(55f + strike * 35f, -18f + strike * 20f, -12f);
            body.linearVelocity = Vector3.ClampMagnitude((target - body.position) / Time.fixedDeltaTime, 4f);
            var delta = rotation * Quaternion.Inverse(body.rotation);
            delta.ToAngleAxis(out float angle, out var axis);
            if (angle > 180f) angle -= 360f;
            body.angularVelocity = Vector3.ClampMagnitude(axis * (angle * Mathf.Deg2Rad / Time.fixedDeltaTime), 18f);
        }
        void OnDisable()
        {
            if (drop != null) drop.Resetting -= ReleasePreview;
            ReleasePreview();
        }
    }
}
#endif
