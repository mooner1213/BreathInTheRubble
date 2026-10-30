using System;
using System.Collections;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Simulated call rules. Independent of phone UI, XR and platform telephony.</summary>
    public sealed class EmergencyCallSystem : MonoBehaviour
    {
        public enum CallState { Idle, Connecting, NoSignal, InvalidNumber, Connected }
        [SerializeField, Min(0.1f)] float failedAttemptSeconds = 2.4f;
        [SerializeField, Min(0.1f)] float weakConnectionSeconds = 4f;
        [SerializeField, Min(0.1f)] float twoBarConnectionSeconds = 3f;
        Coroutine attempt;
        public int SignalBars { get; private set; }
        public CallState State { get; private set; }
        public event Action Changed;
        public event Action CallConnected;

        public void SetSignalBars(int bars)
        {
            bars = Mathf.Clamp(bars, 0, 2);
            if (SignalBars == bars) return;
            SignalBars = bars;
            // Losing the network cancels an in-flight or established call.
            if (bars == 0 && (State == CallState.Connecting || State == CallState.Connected))
            {
                CancelAttempt(); State = CallState.NoSignal;
            }
            Changed?.Invoke();
        }

        public bool Dial(string number)
        {
            if (State == CallState.Connecting || State == CallState.Connected) return false;
            CancelAttempt();
            if (number != "119") { SetState(CallState.InvalidNumber); return false; }
            bool hadSignalAtStart = SignalBars > 0;
            SetState(CallState.Connecting);
            attempt = StartCoroutine(Connect(hadSignalAtStart));
            return true;
        }

        IEnumerator Connect(bool hadSignalAtStart)
        {
            float delay = !hadSignalAtStart ? failedAttemptSeconds : SignalBars == 1 ? weakConnectionSeconds : twoBarConnectionSeconds;
            yield return new WaitForSeconds(delay);
            attempt = null;
            // A zero-service attempt fails even if reception recovers during that attempt.
            if (!hadSignalAtStart || SignalBars == 0) { SetState(CallState.NoSignal); yield break; }
            SetState(CallState.Connected);
            CallConnected?.Invoke();
        }

        public void HangUp() { CancelAttempt(); SetState(CallState.Idle); }
        void CancelAttempt() { if (attempt != null) StopCoroutine(attempt); attempt = null; }
        void SetState(CallState state) { State = state; Changed?.Invoke(); }
        void OnDisable() { CancelAttempt(); State = CallState.Idle; }
    }
}
