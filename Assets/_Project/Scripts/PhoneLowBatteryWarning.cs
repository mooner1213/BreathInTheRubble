using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PhoneLowBatteryWarning : MonoBehaviour
    {
        [SerializeField] PhoneBattery battery;
        [SerializeField] GameObject warningBanner;
        [SerializeField] UnityEngine.UI.Button dismissButton;
        [SerializeField] TMPro.TMP_Text messageText;
        bool wasLow;
        int lastNumber = -1;
        public bool IsWarningVisible => warningBanner != null && warningBanner.activeSelf;

        void Awake()
        {
            if (battery == null) battery = GetComponent<PhoneBattery>();
            if (dismissButton != null) dismissButton.onClick.AddListener(Dismiss);
        }
        void Start() { CheckNow(); }
        void Update() { CheckNow(); }

        public void CheckNow()
        {
            if (battery == null || warningBanner == null) return;
            bool low = battery.IsLow;
            if (low && !wasLow)
            {
                warningBanner.transform.SetAsLastSibling();
                warningBanner.SetActive(true);
            }
            if (!low) warningBanner.SetActive(false);
            if (messageText != null && battery.DisplayedPercent != lastNumber)
            {
                lastNumber = battery.DisplayedPercent;
                messageText.text = "LOW BATTERY\n" + lastNumber + "% remaining\nUse the flashlight sparingly.";
            }
            wasLow = low;
        }

        public void Dismiss() { if (warningBanner != null) warningBanner.SetActive(false); }
#if UNITY_EDITOR
        public void PreviewWarning() { wasLow = false; CheckNow(); }
#endif
        void OnDestroy() { if (dismissButton != null) dismissButton.onClick.RemoveListener(Dismiss); }
    }
}
