using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Battery accounting in percentage points; independent of input and visuals.</summary>
    [DisallowMultipleComponent]
    public sealed class PhoneBattery : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] float startingPercent = 47f;
        [SerializeField, Min(0f)] float flashlightPercentPerMinute = 0.36f;
        [SerializeField, Min(0f)] float connectingPercentPerMinute = 0.60f;
        [SerializeField, Min(0f)] float connectedPercentPerMinute = 0.24f;
        double remainingPercent;
        public float RemainingPercent => (float)remainingPercent;
        // Ceiling preserves the starting number until a full percentage point is used.
        public int DisplayedPercent => Mathf.CeilToInt(RemainingPercent);

        void Awake() { remainingPercent = Mathf.Clamp(startingPercent, 0f, 100f); }

        public void Drain(float seconds, bool flashlightOn, EmergencyCallSystem.CallState callState)
        {
            if (seconds <= 0f || remainingPercent <= 0d) return;
            double rate = flashlightOn ? Mathf.Max(0f, flashlightPercentPerMinute) : 0d;
            if (callState == EmergencyCallSystem.CallState.Connecting) rate += Mathf.Max(0f, connectingPercentPerMinute);
            else if (callState == EmergencyCallSystem.CallState.Connected) rate += Mathf.Max(0f, connectedPercentPerMinute);
            // Double precision avoids frame-rate dependent rounding during tiny per-frame drains.
            remainingPercent = System.Math.Max(0d, remainingPercent - rate * seconds / 60d);
        }
    }
}
