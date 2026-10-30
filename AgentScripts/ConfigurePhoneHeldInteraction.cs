using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class ConfigurePhoneHeldInteraction
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";

    public static string Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var grab = root.GetComponent<XRGrabInteractable>()
                ?? throw new InvalidOperationException("XRGrabInteractable missing from phone prefab.");
            var holdMode = root.GetComponent<BreathInTheRubble.Prototype.PhoneToggleGrabMode>()
                ?? throw new InvalidOperationException("Phone hold-mode component missing.");
            var contactFilter = root.GetComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>();
            if (contactFilter == null)
                contactFilter = root.AddComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>();
            var focusPresenter = root.GetComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>()
                ?? throw new InvalidOperationException("PhoneFocusPresenter missing.");
            var canvas = root.transform.Find("Phone Interface Canvas")
                ?? throw new InvalidOperationException("Phone Interface Canvas missing.");
            var flashlightButton = canvas.Find("Flashlight Button")?.GetComponent<UnityEngine.UI.Button>()
                ?? throw new InvalidOperationException("Flashlight Button missing.");
            var screenBackground = canvas.Find("Screen Background")?.GetComponent<UnityEngine.UI.Image>()
                ?? throw new InvalidOperationException("Screen Background missing.");
            var trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>()
                ?? throw new InvalidOperationException("TrackedDeviceGraphicRaycaster missing.");
            var rearFlashlight = root.GetComponentsInChildren<Light>(true)
                .FirstOrDefault(item => item.name == "Rear Flashlight")
                ?? throw new InvalidOperationException("Rear Flashlight missing.");

            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.trackScale = false;
            grab.throwOnDetach = false;
            holdMode.enabled = true;
            contactFilter.enabled = true;
            focusPresenter.enabled = false;

            flashlightButton.interactable = true;
            screenBackground.raycastTarget = false;
            var dismissButton = screenBackground.GetComponent<UnityEngine.UI.Button>();
            if (dismissButton != null)
                dismissButton.interactable = false;
            var flashlightGraphic = flashlightButton.GetComponent<UnityEngine.UI.Graphic>();
            if (flashlightGraphic != null)
                flashlightGraphic.raycastTarget = true;
            trackedRaycaster.ignoreReversedGraphics = false;
            trackedRaycaster.checkFor2DOcclusion = false;
            trackedRaycaster.checkFor3DOcclusion = false;
            rearFlashlight.enabled = false;

            EditorUtility.SetDirty(grab);
            EditorUtility.SetDirty(holdMode);
            EditorUtility.SetDirty(contactFilter);
            EditorUtility.SetDirty(focusPresenter);
            EditorUtility.SetDirty(flashlightButton);
            EditorUtility.SetDirty(screenBackground);
            if (dismissButton != null)
                EditorUtility.SetDirty(dismissButton);
            EditorUtility.SetDirty(trackedRaycaster);
            EditorUtility.SetDirty(rearFlashlight);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();

            return "Configured contact-only phone interaction: far hover/select blocked, hold-to-grab hand tracking enabled, opposite-hand XR UI flashlight button enabled, and rear light output configured.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
