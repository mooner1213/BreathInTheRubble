using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Rescue requires an already-sent request and a held-pipe impact during an audible search cue.</summary>
    public sealed class RescueSignalController : MonoBehaviour
    {
        [SerializeField] GameSequenceController sequence;
        [SerializeField] PipeRescueSignalRelay pipeSignals;
        [SerializeField] AudioSource searchSource;
        [SerializeField] AudioClip searchClip;
        [SerializeField, Min(1f)] float quietIntervalSeconds = 18f;
        float nextSearchAt, cueElapsed;
        bool cueActive;
        public bool IsListeningWindow => cueActive && searchSource != null && searchSource.isPlaying && searchSource.volume > 0f;
        public bool SignalAccepted { get; private set; }
        public int SearchCount { get; private set; }
        public float CueDuration => searchClip != null ? searchClip.length : 0f;
        void OnEnable()
        {
            if (sequence != null) { sequence.Advanced += Advance; sequence.SequenceReset += ResetSearch; }
            if (pipeSignals != null) pipeSignals.SignalMade += OnPipeSignal;
            ResetSearch();
        }
        void OnDisable()
        {
            if (sequence != null) { sequence.Advanced -= Advance; sequence.SequenceReset -= ResetSearch; }
            if (pipeSignals != null) pipeSignals.SignalMade -= OnPipeSignal;
            StopCue();
        }
        public void Advance(float seconds)
        {
            if (sequence == null || SignalAccepted) return;
            if (sequence.Stage != GameStage.Isolation) { StopCue(); return; }
            if (cueActive)
            {
                cueElapsed += Mathf.Max(0f, seconds);
                if (cueElapsed >= CueDuration || !searchSource.isPlaying)
                {
                    StopCue(); nextSearchAt = sequence.ElapsedSeconds + quietIntervalSeconds;
                }
                return;
            }
            if (sequence.ElapsedSeconds >= nextSearchAt) StartCue();
        }
        void StartCue()
        {
            if (searchSource == null || searchClip == null) return;
            cueElapsed = 0f; cueActive = true; SearchCount++;
            searchSource.clip = searchClip; searchSource.Play();
        }
        void OnPipeSignal()
        {
            if (sequence == null || SignalAccepted || !sequence.HasRequestedRescue || !IsListeningWindow) return;
            if (!sequence.ConfirmRescueSignal()) return;
            SignalAccepted = true; StopCue();
        }
        void StopCue() { cueActive = false; if (searchSource != null) searchSource.Stop(); }
        public void ResetSearch()
        {
            StopCue(); SignalAccepted = false; SearchCount = 0; cueElapsed = 0f;
            nextSearchAt = sequence != null ? sequence.FirstRescueSearchSeconds : 240f;
        }
#if UNITY_EDITOR
        [ContextMenu("Preview Search Radio (PC Play Mode)")]
        public void PreviewSearchCue()
        {
            if (Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd && sequence != null && sequence.Stage == GameStage.Isolation && !SignalAccepted) StartCue();
        }
#endif
    }
}
