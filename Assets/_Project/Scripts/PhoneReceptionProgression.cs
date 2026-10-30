using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Temporary story timing until stage events drive SetSignalBars directly.</summary>
    public sealed class PhoneReceptionProgression : MonoBehaviour
    {
        [SerializeField] EmergencyCallSystem callSystem;
        [SerializeField] bool automaticRecovery = true;
        [SerializeField, Min(0f)] float firstBarSeconds = 90f;
        [SerializeField, Min(0f)] float secondBarSeconds = 120f;
        [Header("Unstable first reception")]
        [SerializeField] Vector2 briefOneBarSeconds = new Vector2(1.2f, 2.6f);
        [SerializeField] Vector2 lostSignalSeconds = new Vector2(1.5f, 3.2f);
        [SerializeField] Vector2 callOpportunitySeconds = new Vector2(5.5f, 7.5f);
        [SerializeField, Min(0f)] float settleBeforeSecondBarSeconds = 5f;
        readonly System.Random receptionRandom = new System.Random();
        float elapsed;
        bool previewOverride;
        bool weakPhaseStarted;
        bool hasWeakSignal;
        float nextReceptionChange;
        int oneBarWindows;
        public float ElapsedSeconds => elapsed;
        public bool HasPreviewOverride => previewOverride;

        void Awake() { if (callSystem == null) callSystem = GetComponent<EmergencyCallSystem>(); }
        void Update() { Advance(Time.deltaTime); }
        public void Advance(float seconds)
        {
            elapsed += Mathf.Max(0f, seconds);
            if (!automaticRecovery || previewOverride || callSystem == null) return;
            float secondBarTime = Mathf.Max(firstBarSeconds, secondBarSeconds);
            if (elapsed >= secondBarTime) { callSystem.SetSignalBars(2); return; }
            if (elapsed < firstBarSeconds) { callSystem.SetSignalBars(0); return; }
            // Settle briefly at one bar so the final recovery is visibly 1 -> 2.
            float settleTime = Mathf.Max(firstBarSeconds, secondBarTime - settleBeforeSecondBarSeconds);
            if (elapsed >= settleTime) { callSystem.SetSignalBars(1); return; }
            if (!weakPhaseStarted)
            {
                weakPhaseStarted = true;
                hasWeakSignal = true;
                oneBarWindows = 1;
                nextReceptionChange = firstBarSeconds + ChooseDuration(briefOneBarSeconds);
            }
            while (elapsed >= nextReceptionChange)
            {
                hasWeakSignal = !hasWeakSignal;
                if (hasWeakSignal) oneBarWindows++;
                // Every third opening is long enough to retry and complete the four-second call.
                var duration = !hasWeakSignal ? lostSignalSeconds : oneBarWindows % 3 == 0 ? callOpportunitySeconds : briefOneBarSeconds;
                nextReceptionChange += ChooseDuration(duration);
            }
            callSystem.SetSignalBars(hasWeakSignal ? 1 : 0);
        }
        float ChooseDuration(Vector2 range)
        {
            float minimum = Mathf.Max(0.25f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            return Mathf.Lerp(minimum, maximum, (float)receptionRandom.NextDouble());
        }
        public void PreviewSignal(int bars) { previewOverride = true; callSystem.SetSignalBars(bars); }
        public void ResumeAutomaticRecovery() { previewOverride = false; Advance(0f); }
    }
}
