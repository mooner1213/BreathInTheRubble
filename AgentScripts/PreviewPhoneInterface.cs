using System;
using System.Linq;
using UnityEngine;

public static class PreviewPhoneInterface
{
    public static string PlaceInFrontOfCamera()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Play mode is required.");

        var phone = UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneInterfaceController>(FindObjectsInactive.Include)
            ?? throw new InvalidOperationException("PhoneInterfaceController was not found.");
        var presenter = phone.GetComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>()
            ?? throw new InvalidOperationException("PhoneFocusPresenter was not found.");
        presenter.FocusNow(true);
        phone.GetComponent<BreathInTheRubble.Prototype.PhoneWakeSequence>()?.WakeNow();
        phone.SetFlashlight(false);
        return $"Phone focused through gameplay presenter: pose={phone.transform.position:F3}; rotation={phone.transform.eulerAngles:F1}";
    }

    public static string ToggleFlashlight()
    {
        var phone = UnityEngine.Object.FindObjectsByType<BreathInTheRubble.Prototype.PhoneInterfaceController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault()
            ?? throw new InvalidOperationException("PhoneInterfaceController was not found.");
        phone.ToggleFlashlight();
        return $"FlashlightEnabled={phone.FlashlightEnabled}";
    }

    public static string UseBalancedFlashlight()
    {
        var light = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == "Rear Flashlight")
            ?? throw new InvalidOperationException("Rear Flashlight was not found.");
        light.intensity = 8f;
        light.range = 4.5f;
        light.spotAngle = 64f;
        light.innerSpotAngle = 34f;
        return $"Rear flashlight balanced: intensity={light.intensity}, range={light.range}, spot={light.spotAngle}.";
    }
}
