using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class SimulateTwoHandPhoneInteraction
{
    public static string Run()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Play mode is required.");

        var controller = UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneInterfaceController>(FindObjectsInactive.Include)
            ?? throw new InvalidOperationException("PhoneInterfaceController missing.");
        var phone = controller.gameObject;
        var grab = phone.GetComponent<XRGrabInteractable>()
            ?? throw new InvalidOperationException("Phone XRGrabInteractable missing.");
        var contactFilter = phone.GetComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>()
            ?? throw new InvalidOperationException("Phone contact-only grab filter missing.");
        var wakeSequence = phone.GetComponent<BreathInTheRubble.Prototype.PhoneWakeSequence>()
            ?? throw new InvalidOperationException("PhoneWakeSequence missing.");
        var interactionManager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>(FindObjectsInactive.Exclude)
            ?? throw new InvalidOperationException("XRInteractionManager missing.");

        var canvas = phone.transform.Find("Phone Interface Canvas")?.GetComponent<Canvas>()
            ?? throw new InvalidOperationException("Phone Interface Canvas missing.");
        var flashButton = canvas.transform.Find("Flashlight Button")?.GetComponent<UnityEngine.UI.Button>()
            ?? throw new InvalidOperationException("Flashlight Button missing.");
        var flashLabel = flashButton.GetComponentInChildren<TMP_Text>(true)
            ?? throw new InvalidOperationException("Flashlight label missing.");
        var flashlight = phone.GetComponentsInChildren<Light>(true).FirstOrDefault(item => item.name == "Rear Flashlight")
            ?? throw new InvalidOperationException("Rear Flashlight missing.");
        var raycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>()
            ?? throw new InvalidOperationException("TrackedDeviceGraphicRaycaster missing.");
        var eventSystem = EventSystem.current
            ?? throw new InvalidOperationException("EventSystem missing.");

        var nearFarInteractors = UnityEngine.Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var rightInteractor = nearFarInteractors.FirstOrDefault(item => HierarchyPath(item.transform).Contains("/Right Controller/"))
            ?? throw new InvalidOperationException("Active right Near-Far Interactor missing.");
        var leftPoke = UnityEngine.Object.FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .FirstOrDefault(item => HierarchyPath(item.transform).Contains("/Left Controller/"))
            ?? throw new InvalidOperationException("Active left Poke Interactor missing.");
        var leftPokePoint = leftPoke.transform.Find("Poke Point") ?? leftPoke.transform;
        var rightContactOrigin = rightInteractor.nearInteractionCaster?.effectiveCastOrigin
            ?? throw new InvalidOperationException("Right near interaction origin missing.");
        var phoneCollider = phone.GetComponentsInChildren<Collider>(true).FirstOrDefault(item => item.enabled)
            ?? throw new InvalidOperationException("Enabled phone collider missing.");

        var originalRightPosition = rightContactOrigin.position;
        var originalRightRotation = rightContactOrigin.rotation;
        var originalLeftPosition = leftPokePoint.position;
        var originalLeftRotation = leftPokePoint.rotation;
        var selected = false;

        wakeSequence.WakeNow();
        controller.SetFlashlight(false);

        try
        {
            rightContactOrigin.position = phoneCollider.bounds.center + Vector3.up * 2f;
            Physics.SyncTransforms();
            var farFilterPassed = contactFilter.Process((IXRSelectInteractor)rightInteractor, (IXRSelectInteractable)grab);
            var farSelectionPossible = interactionManager.IsSelectPossible((IXRSelectInteractor)rightInteractor, (IXRSelectInteractable)grab);
            if (farFilterPassed || farSelectionPossible)
                throw new InvalidOperationException("A right hand two metres away could still select the phone.");

            rightContactOrigin.position = phoneCollider.bounds.center;
            rightContactOrigin.rotation = phone.transform.rotation;
            Physics.SyncTransforms();
            var contactFilterPassed = contactFilter.Process((IXRSelectInteractor)rightInteractor, (IXRSelectInteractable)grab);
            var contactSelectionPossible = interactionManager.IsSelectPossible((IXRSelectInteractor)rightInteractor, (IXRSelectInteractable)grab);
            if (!contactFilterPassed || !contactSelectionPossible)
                throw new InvalidOperationException($"Right hand contact was not selectable. filter={contactFilterPassed}, manager={contactSelectionPossible}.");

            interactionManager.SelectEnter((IXRSelectInteractor)rightInteractor, (IXRSelectInteractable)grab);
            selected = grab.isSelected && rightInteractor.interactablesSelected.Contains((IXRSelectInteractable)grab);
            if (!selected)
                throw new InvalidOperationException("XR Interaction Manager did not enter right-hand phone selection.");

            var buttonRect = flashButton.GetComponent<RectTransform>();
            var buttonCenter = buttonRect.TransformPoint(buttonRect.rect.center);
            var camera = Camera.main ?? throw new InvalidOperationException("Main Camera missing.");
            var pressDirection = (buttonCenter - camera.transform.position).normalized;
            leftPokePoint.position = buttonCenter - pressDirection * 0.025f;
            leftPokePoint.rotation = Quaternion.LookRotation(pressDirection, canvas.transform.up);
            Physics.SyncTransforms();

            var eventData = CreateTrackedEvent(eventSystem, leftPokePoint.position, buttonCenter, pressDirection);
            var results = new List<RaycastResult>();
            raycaster.Raycast(eventData, results);
            if (results.Count == 0)
                throw new InvalidOperationException("Left-hand tracked UI ray missed the LIGHT button.");
            if (results[0].gameObject != flashButton.gameObject)
                throw new InvalidOperationException($"Left-hand tracked UI ray hit {results[0].gameObject.name} instead of LIGHT.");

            eventData.pointerCurrentRaycast = results[0];
            eventData.pointerPressRaycast = results[0];
            eventData.pointerPress = flashButton.gameObject;
            eventData.rawPointerPress = flashButton.gameObject;
            eventData.eligibleForClick = true;

            ExecuteEvents.Execute(flashButton.gameObject, eventData, ExecuteEvents.pointerDownHandler);
            var selectedDuringDown = grab.isSelected && rightInteractor.interactablesSelected.Contains((IXRSelectInteractable)grab);
            if (!selectedDuringDown)
                throw new InvalidOperationException("Right-hand selection was lost during left LIGHT pointer-down.");

            ExecuteEvents.Execute(flashButton.gameObject, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(flashButton.gameObject, eventData, ExecuteEvents.pointerClickHandler);

            var selectedAfterClick = grab.isSelected && rightInteractor.interactablesSelected.Contains((IXRSelectInteractable)grab);
            if (!selectedAfterClick)
                throw new InvalidOperationException("Right-hand selection was lost after the left LIGHT click.");
            if (!controller.FlashlightEnabled || !flashlight.enabled)
                throw new InvalidOperationException("LIGHT click completed but the rear flashlight did not turn on.");

            return string.Join(" | ", new[]
            {
                "PASS",
                $"far-right: filter={farFilterPassed}, selectable={farSelectionPossible}",
                $"right-contact: filter={contactFilterPassed}, selectable={contactSelectionPossible}",
                $"right-grab-entered={selected}",
                $"left-source={HierarchyPath(leftPoke.transform)}",
                $"left-pointer-down: right-grab-held={selectedDuringDown}",
                $"left-pointer-up+click: right-grab-held={selectedAfterClick}",
                $"rear-light={flashlight.enabled}",
                $"controller-light={controller.FlashlightEnabled}",
                $"label={flashLabel.text}",
            });
        }
        finally
        {
            if (grab.isSelected && rightInteractor.interactablesSelected.Contains((IXRSelectInteractable)grab))
                interactionManager.SelectExit((IXRSelectInteractor)rightInteractor, (IXRSelectInteractable)grab);

            rightContactOrigin.SetPositionAndRotation(originalRightPosition, originalRightRotation);
            leftPokePoint.SetPositionAndRotation(originalLeftPosition, originalLeftRotation);
            Physics.SyncTransforms();
        }
    }

    static TrackedDeviceEventData CreateTrackedEvent(
        EventSystem eventSystem,
        Vector3 source,
        Vector3 target,
        Vector3 direction)
    {
        return new TrackedDeviceEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left,
            layerMask = ~0,
            rayPoints = new List<Vector3>
            {
                source,
                target + direction * 0.05f,
            },
        };
    }

    static string HierarchyPath(Transform transform)
    {
        var names = new Stack<string>();
        for (var current = transform; current != null; current = current.parent)
            names.Push(current.name);

        return "/" + string.Join("/", names);
    }
}
