using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BreathInTheRubble.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class PhoneToggleGrabMode : MonoBehaviour
    {
        readonly Dictionary<XRBaseInputInteractor, XRBaseInputInteractor.InputTriggerType> originalModes = new();
        readonly HashSet<XRBaseInputInteractor> hoveringInteractors = new();
        readonly HashSet<XRBaseInputInteractor> selectingInteractors = new();

        XRGrabInteractable grabInteractable;

        void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();

            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
                grabInteractable.hoverExited.RemoveListener(OnHoverExited);
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                grabInteractable.selectExited.RemoveListener(OnSelectExited);
            }

            RestoreAllInteractors();
        }

        void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (args.interactorObject is not XRBaseInputInteractor interactor)
                return;

            hoveringInteractors.Add(interactor);
            UseHoldMode(interactor);
        }

        void OnHoverExited(HoverExitEventArgs args)
        {
            if (args.interactorObject is not XRBaseInputInteractor interactor)
                return;

            hoveringInteractors.Remove(interactor);
            RestoreIfUnused(interactor);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is not XRBaseInputInteractor interactor)
                return;

            selectingInteractors.Add(interactor);
            UseHoldMode(interactor);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactorObject is not XRBaseInputInteractor interactor)
                return;

            selectingInteractors.Remove(interactor);
            RestoreIfUnused(interactor);
        }

        void UseHoldMode(XRBaseInputInteractor interactor)
        {
            if (!originalModes.ContainsKey(interactor))
                originalModes.Add(interactor, interactor.selectActionTrigger);

            // The phone is a physical held object: selection must end as soon as
            // the player releases the grab input instead of latching on a press.
            interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.State;
        }

        void RestoreIfUnused(XRBaseInputInteractor interactor)
        {
            if (hoveringInteractors.Contains(interactor) || selectingInteractors.Contains(interactor))
                return;

            if (!originalModes.Remove(interactor, out var originalMode) || interactor == null)
                return;

            interactor.selectActionTrigger = originalMode;
        }

        void RestoreAllInteractors()
        {
            foreach (var pair in originalModes)
            {
                if (pair.Key != null)
                    pair.Key.selectActionTrigger = pair.Value;
            }

            originalModes.Clear();
            hoveringInteractors.Clear();
            selectingInteractors.Clear();
        }
    }
}
