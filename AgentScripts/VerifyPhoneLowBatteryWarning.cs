using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using TMPro;
using UnityEngine;

public static class VerifyPhoneLowBatteryWarning
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static object Run()
    {
        Check(Application.isPlaying, "Play Mode required.");
        var driver = UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();
        Check(driver != null && driver.DesktopEnabled, "Desktop test path is unavailable.");
        var phone = driver.Phone;
        var battery = phone.GetComponent<PhoneBattery>();
        var view = phone.GetComponent<PhoneBatteryView>();
        var warning = phone.GetComponent<PhoneLowBatteryWarning>();
        Check(warning != null, "Warning component is missing from the scene phone.");
        var canvas = phone.transform.Find("Phone Interface Canvas");
        var batteryRoot = canvas.Find("Battery 47 Percent");
        var text = batteryRoot.GetComponentInChildren<TMP_Text>(true);
        var graphics = batteryRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        var banner = (RectTransform)canvas.Find("Low Battery Warning");
        var close = banner.GetComponentInChildren<UnityEngine.UI.Button>(true);

        driver.PreviewBattery(47f);
        Color normalText = text.color;
        Color[] normalGraphics = graphics.Select(g => g.color).ToArray();
        driver.PreviewBattery(25f);
        Check(!battery.IsLow && !warning.IsWarningVisible, "25% incorrectly warns.");
        driver.PreviewBattery(24.01f);
        Check(battery.DisplayedPercent == 25 && !battery.IsLow && !warning.IsWarningVisible, "Fractional 25% display warns early.");
        battery.Drain(2f, true, EmergencyCallSystem.CallState.Idle);
        view.RefreshNow(); warning.CheckNow();
        Check(battery.DisplayedPercent == 24 && warning.IsWarningVisible, "Natural drain crossing 24% did not show popup.");
        Check(text.color.r > 0.9f && text.color.g < 0.3f && graphics.All(g => g.color.r > 0.9f && g.color.g < 0.3f), "Battery text/outline/fill are not all red.");
        Check(driver.IsHolding && phone.GetComponent<PhoneWakeSequence>().IsAwake && banner.gameObject.activeInHierarchy, "Battery test does not bring up an awake phone.");
        var label = banner.GetComponentInChildren<TMP_Text>(true);
        label.ForceMeshUpdate(true);
        Check(label.text.Contains("24% remaining") && !label.isTextOverflowing, "Warning text is missing, stale or clipped.");
        var corners = new Vector3[4]; banner.GetWorldCorners(corners);
        Check(corners.All(c => ((RectTransform)canvas).rect.Contains(canvas.InverseTransformPoint(c))), "Popup is outside the phone screen.");
        close.onClick.Invoke(); warning.CheckNow();
        Check(!warning.IsWarningVisible && battery.IsLow && text.color.g < 0.3f, "Dismiss failed or cleared red state.");
        battery.PreviewRemainingPercent(23f); warning.CheckNow();
        Check(!warning.IsWarningVisible, "Dismissed popup reopened while remaining low.");
        driver.PreviewBattery(24f);
        Check(warning.IsWarningVisible, "Repeated 24% test does not reopen popup.");
        driver.PreviewBattery(47f);
        Check(!warning.IsWarningVisible && text.color == normalText, "Recovery did not restore normal text.");
        for (int i = 0; i < graphics.Length; i++) Check(graphics[i].color == normalGraphics[i], "Recovery did not restore icon colors.");
        driver.PreviewBattery(0f);
        Check(battery.IsLow && warning.IsWarningVisible && text.text == "0%", "Empty battery warning failed.");
        driver.PreviewBattery(47f); driver.PreviewBattery(24f);
        phone.GetComponent<PhoneDialerView>().Open();
        Check(warning.IsWarningVisible && close.IsInteractable(), "Dialer hides/disables warning.");
        close.onClick.Invoke();
        Check(!warning.IsWarningVisible && phone.GetComponent<PhoneDialerView>().IsOpen, "Dismiss changed emergency call screen.");
        phone.GetComponent<PhoneDialerView>().Close();
        driver.PreviewBattery(24f);
        return new { result = "PASS", frame = Time.frameCount, checks = "25/24 boundary, natural drain, red text and all icon graphics, persistent popup, dismiss, recovery, repeat preview, empty battery, phone bounds, readable text, dialer coexistence" };
    }
}
