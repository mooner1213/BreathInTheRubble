using System;
using TMPro;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PhoneInterfaceController : MonoBehaviour
    {
        [Header("Phone")]
        [SerializeField] PhoneWakeSequence wakeSequence;
        [SerializeField] Light rearFlashlight;

        [Header("Interface")]
        [SerializeField] CanvasGroup interfaceCanvasGroup;
        [SerializeField, Range(0.05f, 1f)] float screenBrightness = 0.55f;
        [SerializeField] UnityEngine.UI.Button flashlightButton;
        [SerializeField] UnityEngine.UI.Button emergencyCallButton;
        [SerializeField] UnityEngine.UI.Image flashlightButtonBackground;
        [SerializeField] UnityEngine.UI.Image emergencyButtonBackground;
        [SerializeField] TMP_Text flashlightLabel;
        [SerializeField] TMP_Text emergencyLabel;
        [SerializeField] UnityEngine.UI.Button dismissAreaButton;
        [SerializeField] PhoneFocusPresenter focusPresenter;
        [SerializeField] PhoneDialerView emergencyDialer;

        [Header("Colors")]
        [SerializeField] Color flashlightOffColor = new(0.08f, 0.18f, 0.23f, 0.96f);
        [SerializeField] Color flashlightOnColor = new(0.15f, 0.75f, 0.92f, 1f);
        [SerializeField] Color emergencyIdleColor = new(0.55f, 0.055f, 0.045f, 0.98f);
        [SerializeField] Color emergencyActiveColor = new(0.95f, 0.14f, 0.08f, 1f);

        bool flashlightEnabled;
        bool emergencyCallRequested;

        public bool FlashlightEnabled => flashlightEnabled;
        public bool EmergencyCallRequested => emergencyDialer != null ? emergencyDialer.IsOpen : emergencyCallRequested;

        public event Action EmergencyCallPressed;

        void Awake()
        {
            if (wakeSequence == null)
                wakeSequence = GetComponent<PhoneWakeSequence>();
            if (focusPresenter == null)
                focusPresenter = GetComponent<PhoneFocusPresenter>();
            if (dismissAreaButton == null)
                dismissAreaButton = transform.Find("Phone Interface Canvas/Screen Background")?.GetComponent<UnityEngine.UI.Button>();

            flashlightButton?.onClick.AddListener(ToggleFlashlight);
            emergencyCallButton?.onClick.AddListener(RequestEmergencyCall);
            dismissAreaButton?.onClick.AddListener(DismissPhone);
            SetFlashlight(false);
            SetInterfaceVisible(wakeSequence == null || wakeSequence.IsAwake);
            RefreshInterface();
        }

        void OnEnable()
        {
            if (wakeSequence != null)
                wakeSequence.Awakened += ShowInterface;
        }

        void OnDisable()
        {
            if (wakeSequence != null)
                wakeSequence.Awakened -= ShowInterface;
        }

        void OnDestroy()
        {
            flashlightButton?.onClick.RemoveListener(ToggleFlashlight);
            emergencyCallButton?.onClick.RemoveListener(RequestEmergencyCall);
            dismissAreaButton?.onClick.RemoveListener(DismissPhone);
        }

        public void ToggleFlashlight()
        {
            SetFlashlight(!flashlightEnabled);
        }

        public void SetFlashlight(bool enabled)
        {
            flashlightEnabled = enabled;
            if (wakeSequence != null)
                wakeSequence.SetFlashlight(enabled);
            else if (rearFlashlight != null)
                rearFlashlight.enabled = enabled;

            RefreshInterface();
        }

        public void RequestEmergencyCall()
        {
            if (emergencyDialer != null)
            {
                emergencyDialer.Open();
                return;
            }
            if (emergencyCallRequested)
                return;

            emergencyCallRequested = true;
            EmergencyCallPressed?.Invoke();
            RefreshInterface();
        }

        public void DismissPhone()
        {
            focusPresenter?.ReturnToRest(false);
        }

        void ShowInterface()
        {
            SetInterfaceVisible(true);
        }

        void SetInterfaceVisible(bool visible)
        {
            if (interfaceCanvasGroup == null)
                return;

            interfaceCanvasGroup.alpha = visible ? screenBrightness : 0f;
            interfaceCanvasGroup.interactable = visible;
            interfaceCanvasGroup.blocksRaycasts = visible;
        }

        void RefreshInterface()
        {
            if (flashlightButtonBackground != null)
                flashlightButtonBackground.color = flashlightEnabled ? flashlightOnColor : flashlightOffColor;
            if (flashlightLabel != null)
                flashlightLabel.text = flashlightEnabled ? "LIGHT ON" : "LIGHT";

            if (emergencyButtonBackground != null)
                emergencyButtonBackground.color = emergencyCallRequested ? emergencyActiveColor : emergencyIdleColor;
            if (emergencyLabel != null)
                emergencyLabel.text = emergencyCallRequested ? "CALLING" : "SOS";
            if (emergencyCallButton != null)
                emergencyCallButton.interactable = !emergencyCallRequested;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            PhoneWakeSequence sequence,
            Light flashlight,
            CanvasGroup canvasGroup,
            UnityEngine.UI.Button flashlightControl,
            UnityEngine.UI.Button emergencyControl,
            UnityEngine.UI.Image flashlightBackground,
            UnityEngine.UI.Image emergencyBackground,
            TMP_Text flashlightText,
            TMP_Text emergencyText,
            UnityEngine.UI.Button dismissControl,
            PhoneFocusPresenter presenter)
        {
            wakeSequence = sequence;
            rearFlashlight = flashlight;
            interfaceCanvasGroup = canvasGroup;
            flashlightButton = flashlightControl;
            emergencyCallButton = emergencyControl;
            flashlightButtonBackground = flashlightBackground;
            emergencyButtonBackground = emergencyBackground;
            flashlightLabel = flashlightText;
            emergencyLabel = emergencyText;
            dismissAreaButton = dismissControl;
            focusPresenter = presenter;
        }
#endif
    }
}
