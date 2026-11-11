using TMPro;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PhoneBatteryView : MonoBehaviour
    {
        [SerializeField] PhoneBattery battery;
        [SerializeField] TMP_Text percentText;
        [SerializeField] RectTransform fillRect;
        [SerializeField] UnityEngine.UI.Image[] batteryGraphics = System.Array.Empty<UnityEngine.UI.Image>();
        [SerializeField] Color lowBatteryColor = new Color(1f, 0.20f, 0.18f, 1f);
        [SerializeField, Min(0f)] float fullFillWidth = 90f;
        Color normalTextColor;
        Color[] normalGraphicColors;
        bool? lastLow;
        float refreshElapsed;
        float lastPercent = -1f;
        int lastNumber = -1;

        void Start() { RefreshNow(); }
        void LateUpdate()
        {
            refreshElapsed += Time.deltaTime;
            if (battery != null && battery.DisplayedPercent == lastNumber && refreshElapsed < 1f) return;
            refreshElapsed = 0f;
            RefreshNow();
        }
        public void RefreshNow()
        {
            if (battery == null) return;
            float remaining = battery.RemainingPercent;
            int number = battery.DisplayedPercent;
            if (percentText != null && number != lastNumber) percentText.text = number + "%";
            if (fillRect != null && remaining != lastPercent)
                fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullFillWidth * remaining / 100f);
            if (normalGraphicColors == null)
            {
                normalTextColor = percentText != null ? percentText.color : Color.white;
                normalGraphicColors = new Color[batteryGraphics.Length];
                for (int i = 0; i < batteryGraphics.Length; i++)
                    if (batteryGraphics[i] != null) normalGraphicColors[i] = batteryGraphics[i].color;
            }
            if (lastLow != battery.IsLow)
            {
                if (percentText != null) percentText.color = battery.IsLow ? lowBatteryColor : normalTextColor;
                for (int i = 0; i < batteryGraphics.Length; i++)
                    if (batteryGraphics[i] != null) batteryGraphics[i].color = battery.IsLow ? lowBatteryColor : normalGraphicColors[i];
                lastLow = battery.IsLow;
            }
            lastPercent = remaining;
            lastNumber = number;
        }
    }
}
