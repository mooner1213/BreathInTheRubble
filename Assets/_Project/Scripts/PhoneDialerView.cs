using TMPro;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    public sealed class PhoneDialerView : MonoBehaviour
    {
        [SerializeField] EmergencyCallSystem callSystem;
        [SerializeField] GameObject dialerPanel;
        [SerializeField] GameObject[] homeControls;
        [SerializeField] TMP_Text numberText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text serviceText;
        [SerializeField] TMP_Text callButtonText;
        [SerializeField] UnityEngine.UI.Button callButton;
        [SerializeField] UnityEngine.UI.Button backButton;
        [SerializeField] UnityEngine.UI.Button deleteButton;
        [SerializeField] UnityEngine.UI.Button[] digitButtons;
        [SerializeField] UnityEngine.UI.Image[] signalBars;
        string number = "119";
        public bool IsOpen => dialerPanel != null && dialerPanel.activeSelf;
        public string Number => number;
        public EmergencyCallSystem CallSystem => callSystem;

        void Awake()
        {
            if (callSystem == null) callSystem = GetComponent<EmergencyCallSystem>();
            callButton.onClick.AddListener(ConfirmCall);
            backButton.onClick.AddListener(Close);
            deleteButton.onClick.AddListener(DeleteDigit);
            foreach (var button in digitButtons)
            {
                var digit = button.name.Substring("Dial Key ".Length);
                button.onClick.AddListener(() => AppendDigit(digit));
            }
            dialerPanel.SetActive(false);
        }
        void OnEnable() { if (callSystem != null) callSystem.Changed += Refresh; Refresh(); }
        void OnDisable() { if (callSystem != null) callSystem.Changed -= Refresh; }
        void OnDestroy()
        {
            if (callButton != null) callButton.onClick.RemoveListener(ConfirmCall);
            if (backButton != null) backButton.onClick.RemoveListener(Close);
            if (deleteButton != null) deleteButton.onClick.RemoveListener(DeleteDigit);
        }
        public void Open()
        {
            if (IsOpen) return;
            number = "119";
            dialerPanel.SetActive(true);
            foreach (var control in homeControls) control.SetActive(false);
            Refresh();
        }
        public void Close()
        {
            callSystem.HangUp(); dialerPanel.SetActive(false);
            foreach (var control in homeControls) control.SetActive(true);
        }
        public void ConfirmCall()
        {
            if (!IsOpen) return;
            if (callSystem.State == EmergencyCallSystem.CallState.Connected) callSystem.HangUp();
            else callSystem.Dial(number);
        }
        public void AppendDigit(string digit)
        {
            if (Busy || number.Length >= 12) return;
            number += digit; callSystem.HangUp(); Refresh();
        }
        public void DeleteDigit()
        {
            if (Busy || number.Length == 0) return;
            number = number.Substring(0, number.Length - 1); callSystem.HangUp(); Refresh();
        }
        bool Busy => callSystem != null && (callSystem.State == EmergencyCallSystem.CallState.Connecting || callSystem.State == EmergencyCallSystem.CallState.Connected);
        void Refresh()
        {
            if (callSystem == null || numberText == null) return;
            for (int i = 0; i < signalBars.Length; i++) signalBars[i].enabled = i < callSystem.SignalBars;
            serviceText.text = callSystem.SignalBars == 0 ? "NO SERVICE" : "WEAK SIGNAL";
            numberText.text = number;
            var state = callSystem.State;
            statusText.text = state == EmergencyCallSystem.CallState.Connecting ? "CONNECTING..." :
                state == EmergencyCallSystem.CallState.NoSignal ? "NO SIGNAL\nUNABLE TO CONNECT" :
                state == EmergencyCallSystem.CallState.InvalidNumber ? "EMERGENCY NUMBER: 119" :
                state == EmergencyCallSystem.CallState.Connected ? "119 CONNECTED\nWEAK CONNECTION" :
                callSystem.SignalBars == 0 ? "NO MOBILE NETWORK" : "EMERGENCY CALL AVAILABLE";
            statusText.color = state == EmergencyCallSystem.CallState.NoSignal || state == EmergencyCallSystem.CallState.InvalidNumber
                ? new Color(1f, 0.42f, 0.32f) : state == EmergencyCallSystem.CallState.Connected
                ? new Color(0.45f, 0.93f, 0.65f) : new Color(0.7f, 0.82f, 0.86f);
            callButtonText.text = state == EmergencyCallSystem.CallState.Connecting ? "CONNECTING" : state == EmergencyCallSystem.CallState.Connected ? "END CALL" : state == EmergencyCallSystem.CallState.NoSignal ? "RETRY CALL" : "CALL 119";
            callButton.interactable = state != EmergencyCallSystem.CallState.Connecting && number.Length > 0;
            deleteButton.interactable = !Busy && number.Length > 0;
            foreach (var button in digitButtons) button.interactable = !Busy;
            backButton.GetComponentInChildren<TMP_Text>().text = Busy ? "CANCEL / BACK" : "BACK";
        }
    }
}
