using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class ApplyPhoneFocusPresenter
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";

    public static string Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var grab = root.GetComponent<XRGrabInteractable>()
                ?? throw new InvalidOperationException("XRGrabInteractable was not found.");
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.trackScale = false;
            grab.throwOnDetach = false;

            var holdGrab = root.GetComponent<BreathInTheRubble.Prototype.PhoneToggleGrabMode>();
            if (holdGrab != null)
                holdGrab.enabled = true;
            var contactFilter = root.GetComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>();
            if (contactFilter == null)
                contactFilter = root.AddComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>();
            contactFilter.enabled = true;

            var presenter = root.GetComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>();
            if (presenter == null)
                presenter = root.AddComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>();
            presenter.enabled = false;

            EditorUtility.SetDirty(grab);
            EditorUtility.SetDirty(contactFilter);
            EditorUtility.SetDirty(presenter);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return "Phone now follows the selecting hand while grab is held; legacy HMD-front presentation is disabled.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
