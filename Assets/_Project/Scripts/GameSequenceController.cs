using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace BreathInTheRubble.Prototype
{
    public enum GameStage{Awakening,Aftershock,Isolation,Rescue}
    public sealed class GameSequenceController:MonoBehaviour
    {
        [SerializeField] PhoneReceptionProgression reception;
        [SerializeField] EmergencyCallSystem calls;
        [SerializeField] AftershockDirector aftershock;
        [SerializeField,Min(0f)] float aftershockAtSeconds=75f;
        [FormerlySerializedAs("rescueEarliestSeconds")]
        [SerializeField,Min(0f)] float firstRescueSearchSeconds=240f;
        [SerializeField] UnityEvent<GameStage> onStageChanged=new UnityEvent<GameStage>();
        bool aftershockStarted,rescueSignalReceived,rescuePreview;
        public float ElapsedSeconds{get;private set;}
        public GameStage Stage{get;private set;}
        public event Action<GameStage> StageChanged;
        public event Action<float> Advanced;
        public event Action SequenceReset;
        public bool HasRequestedRescue => calls != null && calls.RescueRequestSent;
        public float FirstRescueSearchSeconds => firstRescueSearchSeconds;
        void OnEnable(){if(reception!=null)reception.SetExternalClock(true);}
        void Update(){Advance(Time.deltaTime);}
        public void Advance(float seconds)
        {
            float delta=Mathf.Max(0f,seconds);float before=ElapsedSeconds;ElapsedSeconds+=delta;
            if(reception!=null)reception.Advance(delta);
            float shockDelta=delta;
            if(!aftershockStarted && ElapsedSeconds>=aftershockAtSeconds)
            {
                BeginAftershockNow();shockDelta=Mathf.Max(0f,ElapsedSeconds-Mathf.Max(before,aftershockAtSeconds));
            }
            if(aftershock!=null && aftershock.IsRunning)aftershock.Advance(shockDelta);
            if(rescuePreview || rescueSignalReceived)SetStage(GameStage.Rescue);
            else if(aftershock!=null && aftershock.IsRunning)SetStage(GameStage.Aftershock);
            else if(aftershockStarted)SetStage(GameStage.Isolation);
            Advanced?.Invoke(delta);
        }
        public bool ConfirmRescueSignal()
        {
            if (!HasRequestedRescue || Stage != GameStage.Isolation) return false;
            rescueSignalReceived = true; SetStage(GameStage.Rescue); return true;
        }
        public void BeginAftershockNow(){SequenceReset?.Invoke();aftershockStarted=true;rescuePreview=rescueSignalReceived=false;if(aftershock!=null)aftershock.Begin();SetStage(GameStage.Aftershock);}
        public void RestoreAftershock(){SequenceReset?.Invoke();rescuePreview=rescueSignalReceived=false;if(aftershock!=null)aftershock.ResetEffect();SetStage(aftershockStarted?GameStage.Isolation:GameStage.Awakening);}
        public void PreviewNextStage()
        {
            if(Stage==GameStage.Awakening)BeginAftershockNow();
            else if(Stage==GameStage.Aftershock)RestoreAftershock();
            else{rescuePreview=true;SetStage(GameStage.Rescue);}
        }
        void SetStage(GameStage stage){if(Stage==stage)return;Stage=stage;StageChanged?.Invoke(stage);onStageChanged.Invoke(stage);}
        void OnDisable(){if(reception!=null)reception.SetExternalClock(false);}
    }
}
