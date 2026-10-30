using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class ConfigurePhoneFocusUI
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";

    public static string Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var presenter = root.GetComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>()
                ?? throw new InvalidOperationException("PhoneFocusPresenter missing.");
            var controller = root.GetComponent<BreathInTheRubble.Prototype.PhoneInterfaceController>()
                ?? throw new InvalidOperationException("PhoneInterfaceController missing.");
            var canvas = root.GetComponentInChildren<Canvas>(true)
                ?? throw new InvalidOperationException("Phone Canvas missing.");
            var backgroundTransform = canvas.transform.Find("Screen Background")
                ?? throw new InvalidOperationException("Screen Background missing.");
            var background = backgroundTransform.GetComponent<UnityEngine.UI.Image>()
                ?? throw new InvalidOperationException("Screen Background Image missing.");
            background.raycastTarget = false;
            var dismissButton = background.GetComponent<UnityEngine.UI.Button>();
            if (dismissButton == null)
                dismissButton = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            dismissButton.targetGraphic = background;
            dismissButton.interactable = false;
            dismissButton.transition = UnityEngine.UI.Selectable.Transition.None;
            dismissButton.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

            var flashlightButton = canvas.transform.Find("Flashlight Button")?.GetComponent<UnityEngine.UI.Button>()
                ?? throw new InvalidOperationException("Flashlight Button missing.");
            var emergencyButton = canvas.transform.Find("Emergency Call Button")?.GetComponent<UnityEngine.UI.Button>()
                ?? throw new InvalidOperationException("Emergency Call Button missing.");
            flashlightButton.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            emergencyButton.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;

            var trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>()
                ?? throw new InvalidOperationException("TrackedDeviceGraphicRaycaster missing.");
            trackedRaycaster.ignoreReversedGraphics = false;
            trackedRaycaster.checkFor2DOcclusion = false;
            trackedRaycaster.checkFor3DOcclusion = false;

            var wake = root.GetComponent<BreathInTheRubble.Prototype.PhoneWakeSequence>();
            var rearFlashlight = root.GetComponentsInChildren<Light>(true).First(item => item.name == "Rear Flashlight");
            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            var flashlightLabel = flashlightButton.GetComponentInChildren<TMP_Text>(true);
            var emergencyLabel = emergencyButton.GetComponentInChildren<TMP_Text>(true);
            controller.EditorConfigure(
                wake,
                rearFlashlight,
                canvasGroup,
                flashlightButton,
                emergencyButton,
                flashlightButton.GetComponent<UnityEngine.UI.Image>(),
                emergencyButton.GetComponent<UnityEngine.UI.Image>(),
                flashlightLabel,
                emergencyLabel,
                dismissButton,
                presenter);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(presenter);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return "Configured phone UI priority: only action buttons receive XR input; the background does not intercept pokes.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
