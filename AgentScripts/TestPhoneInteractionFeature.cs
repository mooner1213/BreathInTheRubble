using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class TestPhoneInteractionFeature
{
    public static string Run()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Play mode is required.");

        var controller = UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneInterfaceController>(FindObjectsInactive.Include)
            ?? throw new InvalidOperationException("PhoneInterfaceController missing.");
        var phone = controller.gameObject;
        var canvas = phone.transform.Find("Phone Interface Canvas")?.GetComponent<Canvas>()
            ?? throw new InvalidOperationException("Phone canvas missing.");
        var flashButton = canvas.transform.Find("Flashlight Button")?.GetComponent<UnityEngine.UI.Button>()
            ?? throw new InvalidOperationException("Flashlight button missing.");
        var emergencyButton = canvas.transform.Find("Emergency Call Button")?.GetComponent<UnityEngine.UI.Button>()
            ?? throw new InvalidOperationException("Emergency button missing.");
        var battery = canvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(item => item.name == "Battery Text")
            ?? throw new InvalidOperationException("Battery text missing.");
        var flashlight = phone.GetComponentsInChildren<Light>(true).FirstOrDefault(item => item.name == "Rear Flashlight")
            ?? throw new InvalidOperationException("Rear flashlight missing.");
        var wakeSequence = phone.GetComponent<BreathInTheRubble.Prototype.PhoneWakeSequence>()
            ?? throw new InvalidOperationException("PhoneWakeSequence missing.");
        var focusPresenter = phone.GetComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>()
            ?? throw new InvalidOperationException("PhoneFocusPresenter missing.");
        var holdGrab = phone.GetComponent<BreathInTheRubble.Prototype.PhoneToggleGrabMode>()
            ?? throw new InvalidOperationException("Phone hold grab mode missing.");
        var contactFilter = phone.GetComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>()
            ?? throw new InvalidOperationException("Phone contact-only grab filter missing.");
        var grab = phone.GetComponent<XRGrabInteractable>()
            ?? throw new InvalidOperationException("XRGrabInteractable missing.");
        var trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>()
            ?? throw new InvalidOperationException("TrackedDeviceGraphicRaycaster missing.");

        if (canvas.transform.localScale.x >= 0f)
            throw new InvalidOperationException("Phone UI horizontal orientation correction missing.");
        if (UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("Exactly one EventSystem is required.");
        if (Vector3.Dot(flashlight.transform.forward, phone.transform.forward) < 0.98f || flashlight.transform.localPosition.z <= 0f)
            throw new InvalidOperationException("Rear flashlight is not pointing out from the back of the phone.");
        if (!holdGrab.enabled)
            throw new InvalidOperationException("Hold-to-grab input mode is disabled.");
        if (!contactFilter.enabled)
            throw new InvalidOperationException("Contact-only phone grab filter is disabled.");
        if (!Enumerable.Range(0, grab.selectFilters.count).Any(index => ReferenceEquals(grab.selectFilters.GetAt(index), contactFilter)))
            throw new InvalidOperationException("Contact-only filter is not registered as a select filter.");
        if (!Enumerable.Range(0, grab.hoverFilters.count).Any(index => ReferenceEquals(grab.hoverFilters.GetAt(index), contactFilter)))
            throw new InvalidOperationException("Contact-only filter is not registered as a hover filter.");
        if (focusPresenter.enabled)
            throw new InvalidOperationException("Legacy HMD-front phone presentation must stay disabled.");
        if (!grab.trackPosition || !grab.trackRotation || grab.throwOnDetach)
            throw new InvalidOperationException("Phone grab must follow hand position/rotation without throwing on release.");
        if (!phone.GetComponentsInChildren<Collider>(true).Any(item => item.enabled))
            throw new InvalidOperationException("Phone colliders must remain enabled for direct hand contact.");
        if (UnityEngine.Object.FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length < 2)
            throw new InvalidOperationException("Both controller hands need active poke interactors for phone buttons.");

        var camera = Camera.main ?? throw new InvalidOperationException("Main Camera missing.");
        var cameraPosition = camera.transform.position;
        var cameraRotation = camera.transform.rotation;
        var restPosition = phone.transform.position;
        var restRotation = phone.transform.rotation;

        var nearFarInteractor = UnityEngine.Object.FindFirstObjectByType<NearFarInteractor>(FindObjectsInactive.Exclude)
            ?? throw new InvalidOperationException("Active NearFarInteractor missing.");
        var contactOrigin = nearFarInteractor.nearInteractionCaster?.effectiveCastOrigin
            ?? throw new InvalidOperationException("Near interaction caster origin missing.");
        var originalContactPosition = contactOrigin.position;
        var phoneCollider = phone.GetComponentsInChildren<Collider>(true).First(item => item.enabled);
        try
        {
            contactOrigin.position = phoneCollider.bounds.center + Vector3.up * 2f;
            Physics.SyncTransforms();
            if (contactFilter.Process((IXRSelectInteractor)nearFarInteractor, (IXRSelectInteractable)grab))
                throw new InvalidOperationException("Far controller position incorrectly passed the phone contact filter.");

            contactOrigin.position = phoneCollider.bounds.center;
            Physics.SyncTransforms();
            if (!contactFilter.Process((IXRSelectInteractor)nearFarInteractor, (IXRSelectInteractable)grab))
                throw new InvalidOperationException("Controller touching the phone did not pass the contact filter.");
        }
        finally
        {
            contactOrigin.position = originalContactPosition;
            Physics.SyncTransforms();
        }

        wakeSequence.WakeNow();

        controller.SetFlashlight(false);
        var flashButtonRect = flashButton.GetComponent<RectTransform>();
        var flashButtonCenter = flashButtonRect.TransformPoint(flashButtonRect.rect.center);
        ClickTrackedUI(trackedRaycaster, camera, flashButtonCenter, flashButton.gameObject);
        if (!controller.FlashlightEnabled || !flashlight.enabled)
            throw new InvalidOperationException("Tracked XR click did not enable the rear flashlight.");
        if (Vector3.Distance(phone.transform.position, restPosition) > 0.002f || Quaternion.Angle(phone.transform.rotation, restRotation) > 0.2f)
            throw new InvalidOperationException("Pressing LIGHT unexpectedly moved the phone.");
        ClickTrackedUI(trackedRaycaster, camera, flashButtonCenter, flashButton.gameObject);
        if (controller.FlashlightEnabled || flashlight.enabled)
            throw new InvalidOperationException("Tracked XR click did not toggle the rear flashlight off.");

        var emergencyButtonRect = emergencyButton.GetComponent<RectTransform>();
        var emergencyButtonCenter = emergencyButtonRect.TransformPoint(emergencyButtonRect.rect.center);
        ClickTrackedUI(trackedRaycaster, camera, emergencyButtonCenter, emergencyButton.gameObject);
        if (!controller.EmergencyCallRequested)
            throw new InvalidOperationException("Emergency button did not raise its request state.");
        var dialer=phone.GetComponent<BreathInTheRubble.Prototype.PhoneDialerView>();
        if (dialer == null || !dialer.IsOpen || dialer.Number != "119" || dialer.CallSystem.State != BreathInTheRubble.Prototype.EmergencyCallSystem.CallState.Idle)
            throw new InvalidOperationException("SOS must open the prefilled 119 dialer without placing a call.");
        dialer.Close();

        if (Vector3.Distance(camera.transform.position, cameraPosition) > 0.0001f || Quaternion.Angle(camera.transform.rotation, cameraRotation) > 0.01f)
            throw new InvalidOperationException("Phone interaction changed the player camera pose.");

        return $"PASS: far hover/select filter rejected a controller 2m away and accepted contact at the phone collider; hold-to-grab tracking enabled; two poke hands active; tracked UI LIGHT toggled the rear flashlight; camera and phone pose unchanged; battery={battery.text}.";
    }

    static void ClickTrackedUI(
        TrackedDeviceGraphicRaycaster raycaster,
        Camera camera,
        Vector3 worldPoint,
        GameObject expectedTarget)
    {
        var direction = (worldPoint - camera.transform.position).normalized;
        var eventData = new TrackedDeviceEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            layerMask = ~0,
            rayPoints = new List<Vector3>
            {
                camera.transform.position,
                worldPoint + direction * 0.25f,
            },
        };
        var results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);
        if (results.Count == 0)
            throw new InvalidOperationException($"Tracked UI ray missed {expectedTarget.name}.");
        if (results[0].gameObject != expectedTarget)
            throw new InvalidOperationException($"Tracked UI ray expected {expectedTarget.name} but hit {results[0].gameObject.name} first.");

        eventData.pointerCurrentRaycast = results[0];
        ExecuteEvents.Execute(expectedTarget, eventData, ExecuteEvents.pointerClickHandler);
    }
}
