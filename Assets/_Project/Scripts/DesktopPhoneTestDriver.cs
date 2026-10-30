#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Editor-only desktop adapter. Calls the same phone actions as the XR interface.</summary>
    public sealed class DesktopPhoneTestDriver : MonoBehaviour
    {
        readonly List<Behaviour> suspended = new();
        readonly List<Renderer> hidden = new();
        Camera view;
        PhoneInterfaceController phone;
        Rigidbody body;
        XRGrabInteractable grab;
        Vector3 cameraPosition, restPosition;
        Quaternion cameraRotation, restRotation;
        bool restKinematic, restGravity, restCollisions, restGrab;
        float viewYaw, viewPitch;
        Vector3 phoneAngles;
        float distance = 0.34f;
        bool clickedScreenButton;

        public bool DesktopEnabled { get; private set; }
        public bool IsHolding { get; private set; }
        public bool AimWithView { get; private set; }
        public PhoneInterfaceController Phone => phone;
        public Vector3 PhoneAngles => phoneAngles;
        public static bool HasPhysicalHmd
        {
            get
            {
                foreach (var device in InputSystem.devices)
                    if (device is XRHMD && !device.layout.Contains("Simulated") && !device.name.Contains("Simulated")) return true;
                return false;
            }
        }

        public void SetDesktopMode(bool active)
        {
            if (active == DesktopEnabled) return;
            if (active)
            {
                if (HasPhysicalHmd) return;
                phone = FindFirstObjectByType<PhoneInterfaceController>(); view = Camera.main;
                if (phone == null || view == null) return;
                body = phone.GetComponent<Rigidbody>(); grab = phone.GetComponent<XRGrabInteractable>();
                cameraPosition = view.transform.position; cameraRotation = view.transform.rotation;
                var euler = cameraRotation.eulerAngles;
                viewYaw = euler.y; viewPitch = Mathf.DeltaAngle(0f, euler.x);
                var tracking = view.GetComponent<TrackedPoseDriver>();
                Suspend(tracking);
                foreach (var behaviour in FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
                    if (behaviour.GetType().Name.StartsWith("XRInteractionSimulator") || behaviour.GetType().Name == "XRDeviceSimulator" ||
                        (behaviour is Canvas && behaviour.transform.root.name.StartsWith("XR Interaction Simulator UI")) ||
                        (behaviour.transform.IsChildOf(view.transform.root) &&
                         (behaviour.GetType().Name == "XRInteractorLineVisual" || behaviour.GetType().Name == "CurveVisualController" || behaviour.GetType().Name == "XRInteractorReticleVisual"))) Suspend(behaviour);
                foreach (var renderer in view.transform.root.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled) { hidden.Add(renderer); renderer.enabled = false; }
                DesktopEnabled = true;
            }
            else
            {
                ReturnPhone();
                DesktopEnabled = false;
                if (view != null) view.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true;
                hidden.Clear();
                foreach (var behaviour in suspended) if (behaviour != null) behaviour.enabled = true;
                suspended.Clear();
            }
        }

        void Suspend(Behaviour behaviour)
        {
            if (behaviour == null || !behaviour.enabled) return;
            suspended.Add(behaviour); behaviour.enabled = false;
        }

        void Update()
        {
            if (!DesktopEnabled) return;
            if (HasPhysicalHmd) { SetDesktopMode(false); return; }
            // Inspector typing and shortcuts in other windows must not operate the phone.
            if (UnityEditor.EditorWindow.focusedWindow == null || UnityEditor.EditorWindow.focusedWindow.GetType().Name != "GameView") return;
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame) ToggleHold();
                if (keyboard.fKey.wasPressedThisFrame) ToggleFlashlight();
                if (keyboard.cKey.wasPressedThisFrame) EmergencyCall();
                if (keyboard.enterKey.wasPressedThisFrame) ConfirmEmergencyCall();
                if (keyboard.qKey.wasPressedThisFrame) ToggleAim();
                if (keyboard.rKey.wasPressedThisFrame) ResetPhonePose();
            }
            if (mouse == null) return;
            var delta = mouse.delta.ReadValue();
            if (mouse.rightButton.isPressed) Look(delta);
            if (mouse.leftButton.wasPressedThisFrame) clickedScreenButton = TryClickScreenButton(mouse.position.ReadValue());
            if (mouse.leftButton.wasReleasedThisFrame) clickedScreenButton = false;
            if (IsHolding && mouse.leftButton.isPressed && !clickedScreenButton) RotatePhone(delta);
            if (IsHolding) distance = Mathf.Clamp(distance + mouse.scroll.ReadValue().y * 0.0003f, 0.23f, 0.52f);
        }

        void LateUpdate()
        {
            if (!DesktopEnabled || view == null) return;
            // Desktop-only rotation. The seated camera position never changes.
            view.transform.SetPositionAndRotation(cameraPosition, Quaternion.Euler(viewPitch, viewYaw, 0f));
            if (IsHolding) ApplyHeldPose();
        }

        public void ToggleHold()
        {
            if (!DesktopEnabled || phone == null) return;
            if (IsHolding) { ReturnPhone(); return; }
            if (grab != null && grab.isSelected) return;
            restPosition = phone.transform.position; restRotation = phone.transform.rotation;
            restKinematic = body.isKinematic; restGravity = body.useGravity; restCollisions = body.detectCollisions;
            restGrab = grab != null && grab.enabled;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            if (grab != null) grab.enabled = false;
            body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
            IsHolding = true; ResetPhonePose(); phone.GetComponent<PhoneWakeSequence>()?.WakeNow();
            ApplyHeldPose();
        }

        public void ReturnPhone()
        {
            if (!IsHolding) return;
            IsHolding = false;
            if (phone == null || body == null) return;
            body.position = restPosition; body.rotation = restRotation;
            phone.transform.SetPositionAndRotation(restPosition, restRotation);
            body.useGravity = restGravity; body.detectCollisions = restCollisions; body.isKinematic = restKinematic;
            if (grab != null) grab.enabled = restGrab;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        }

        public void ToggleFlashlight()
        {
            if (!DesktopEnabled) return;
            // F always puts a usable light in the player's hand, even before the phone alarm.
            if (!IsHolding) ToggleHold();
            phone.GetComponent<PhoneWakeSequence>()?.WakeNow(); phone.ToggleFlashlight();
            if (phone.FlashlightEnabled) { AimWithView = true; phoneAngles = Vector3.zero; }
            ApplyHeldPose();
        }

        public void EmergencyCall()
        {
            if (!DesktopEnabled) return;
            if (!IsHolding) ToggleHold();
            phone.GetComponent<PhoneWakeSequence>()?.WakeNow(); phone.RequestEmergencyCall();
        }

        public void ConfirmEmergencyCall() { if (DesktopEnabled) phone.GetComponent<PhoneDialerView>()?.ConfirmCall(); }
        public void PreviewReception(int bars) { if (DesktopEnabled) phone.GetComponent<PhoneReceptionProgression>()?.PreviewSignal(bars); }

        public void ToggleAim() { if (!DesktopEnabled) return; if (!IsHolding) ToggleHold(); AimWithView = !AimWithView; phoneAngles = Vector3.zero; ApplyHeldPose(); }
        public void ResetPhonePose() { phoneAngles = Vector3.zero; distance = 0.34f; AimWithView = false; if (IsHolding) ApplyHeldPose(); }
        public void RotatePhone(Vector2 delta) { phoneAngles.x -= delta.y * 0.30f; phoneAngles.y += delta.x * 0.30f; if (IsHolding) ApplyHeldPose(); }
        public void Look(Vector2 delta) { viewYaw += delta.x * 0.18f; viewPitch = Mathf.Clamp(viewPitch - delta.y * 0.18f, -80f, 80f); }

        bool TryClickScreenButton(Vector2 position)
        {
            if (!IsHolding) return false;
            foreach (var button in phone.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if (!button.gameObject.activeInHierarchy) continue;
                if (button.IsInteractable() && RectTransformUtility.RectangleContainsScreenPoint(button.GetComponent<RectTransform>(), position, view))
                {
                    button.onClick.Invoke();
                    return true;
                }
            }
            return false;
        }

        void ApplyHeldPose()
        {
            if (!IsHolding || phone == null || view == null) return;
            var target = view.transform;
            var position = target.position + target.forward * distance + target.right * 0.075f - target.up * 0.065f;
            var rotation = target.rotation * Quaternion.Euler(phoneAngles);
            if (AimWithView && phone.FlashlightEnabled)
                rotation = Quaternion.LookRotation(target.position + target.forward * 1.8f - position, target.up) * Quaternion.Euler(phoneAngles);
            body.position = position; body.rotation = rotation; phone.transform.SetPositionAndRotation(position, rotation);
        }

        void OnDisable() => SetDesktopMode(false);
    }
}
#endif
