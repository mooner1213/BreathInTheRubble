using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace BreathInTheRubble.Prototype
{
    [DefaultExecutionOrder(-50)]
    public sealed class HeadProtectionDetector : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform rightHand;
        [SerializeField, Min(0.1f)] float handRadius = 0.38f;
        [SerializeField] Vector2 relativeHandHeight = new Vector2(-0.08f, 0.32f);
        [SerializeField, Min(0f)] float holdSeconds = 0.15f;
        [SerializeField] bool requireTrackedDevices = true;
        float held;
        public bool IsProtected { get; private set; }
#if UNITY_EDITOR
        bool editorPreview;
        bool editorProtected;
        public void SetEditorPreview(bool enabled, bool protectedPose)
        {
            if (DesktopPhoneTestDriver.HasPhysicalHmd) return;
            editorPreview = enabled; editorProtected = protectedPose;
        }
#endif
        void Update() { AdvanceProtection(Time.deltaTime); }
        public void AdvanceProtection(float seconds)
        {
            bool valid = head != null && leftHand != null && rightHand != null &&
                (!requireTrackedDevices || HandsTracked()) && EvaluatePose(head.position, head.right, leftHand.position, rightHand.position);
#if UNITY_EDITOR
            if (editorPreview && !DesktopPhoneTestDriver.HasPhysicalHmd) valid = editorProtected;
#endif
            held = valid ? held + Mathf.Max(0f, seconds) : 0f;
            IsProtected = valid && held >= holdSeconds;
        }
        public bool EvaluatePose(Vector3 headPosition, Vector3 headRight, Vector3 left, Vector3 right)
        {
            var l = left - headPosition; var r = right - headPosition;
            if (l.sqrMagnitude > handRadius * handRadius || r.sqrMagnitude > handRadius * handRadius ||
                l.y < relativeHandHeight.x || r.y < relativeHandHeight.x || l.y > relativeHandHeight.y || r.y > relativeHandHeight.y ||
                (left - right).sqrMagnitude < 0.12f * 0.12f) return false;
            var lateral = Vector3.ProjectOnPlane(headRight, Vector3.up).normalized;
            float lx = Vector3.Dot(l, lateral), rx = Vector3.Dot(r, lateral);
            bool bothSides = lx * rx <= 0f && Mathf.Abs(lx - rx) >= 0.10f;
            bool aboveCrown = l.y >= 0.08f && r.y >= 0.08f;
            return bothSides || aboveCrown;
        }
        static bool HandsTracked()
        {
            bool left = false, right = false, headTracked = false;
            foreach (var device in InputSystem.devices)
            {
                if (device is XRHMD hmd && hmd.isTracked.ReadValue() > 0.5f) headTracked = true;
                if (!(device is UnityEngine.InputSystem.XR.XRController controller) || controller.isTracked.ReadValue() < 0.5f) continue;
                foreach (var usage in device.usages)
                {
                    if (usage == UnityEngine.InputSystem.CommonUsages.LeftHand) left = true;
                    if (usage == UnityEngine.InputSystem.CommonUsages.RightHand) right = true;
                }
            }
            return left && right && headTracked;
        }
        void OnDisable() { held = 0f; IsProtected = false; }
    }
}
