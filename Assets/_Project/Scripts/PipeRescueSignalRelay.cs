using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Converts physical, held-pipe impacts into an input event without deciding rescue rules.</summary>
    [RequireComponent(typeof(PipeImpactAudio), typeof(XRGrabInteractable))]
    public sealed class PipeRescueSignalRelay : MonoBehaviour
    {
        PipeImpactAudio impacts;
        XRGrabInteractable grab;
        public event Action SignalMade;
        void OnEnable()
        {
            impacts = GetComponent<PipeImpactAudio>(); grab = GetComponent<XRGrabInteractable>();
            impacts.ImpactPlayed += OnImpact;
        }
        void OnDisable() { if (impacts != null) impacts.ImpactPlayed -= OnImpact; }
        void OnImpact()
        {
            bool held = grab.isSelected;
#if UNITY_EDITOR
            var desktop = GetComponent<DesktopPipeTestDriver>();
            held |= desktop != null && desktop.IsHolding && !DesktopPhoneTestDriver.HasPhysicalHmd;
#endif
            if (held) SignalMade?.Invoke();
        }
    }
}
