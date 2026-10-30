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
        [SerializeField, Min(0f)] float fullFillWidth = 90f;
        float refreshElapsed;
        float lastPercent = -1f;
        int lastNumber = -1;

        void Start() { RefreshNow(); }
        void LateUpdate()
        {
            refreshElapsed += Time.deltaTime;
            if (refreshElapsed < 1f) return;
            refreshElapsed %= 1f;
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
            lastPercent = remaining;
            lastNumber = number;
        }
    }
}
