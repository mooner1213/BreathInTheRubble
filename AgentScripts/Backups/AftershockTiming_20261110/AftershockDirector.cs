using System;
using UnityEngine;
using UnityEngine.Events;

namespace BreathInTheRubble.Prototype
{
    public sealed class AftershockDirector : MonoBehaviour
    {
        [SerializeField] HeadProtectionDetector protection;
        [SerializeField] HeadImpactFeedback hitFeedback;
        [SerializeField] AftershockEnvironmentMotion environmentMotion;
        [SerializeField] AftershockDebris debris;
        [SerializeField] AftershockAudio audioCue;
        [SerializeField] ParticleSystem extraDust;
        [SerializeField] AftershockDustEffect dustAtmosphere;
        [SerializeField, Min(0)] int openingDustCount = 180;
        [SerializeField, Min(0)] int impactDustCount = 120;
        [SerializeField, Min(1f)] float durationSeconds=18f;
        [SerializeField] float[] impactTimes={3.2f,4.8f,6.6f};
        [SerializeField, Min(1)] int primaryImpactCount = 3;
        [SerializeField, Min(0)] int lateImpactDustCount = 36;
        [SerializeField] AftershockFractureSection[] fractureSections = Array.Empty<AftershockFractureSection>();
        [SerializeField] UnityEvent onStarted=new UnityEvent();
        [SerializeField] UnityEvent onFinished=new UnityEvent();
        [SerializeField] UnityEvent onUnprotectedImpact=new UnityEvent();
        int nextImpact;
        public bool IsRunning{get;private set;}
        public float ElapsedSeconds{get;private set;}
        public int GuardedImpacts{get;private set;}
        public int UnprotectedImpacts{get;private set;}
        public float DurationSeconds => durationSeconds;
        public int ImpactCount => impactTimes.Length;
        public event Action<bool> ImpactResolved;
        public event Action Finished;
        public event Action<float> Advanced;
        public event Action EffectReset;
        public void Begin()
        {
            ResetEffect();IsRunning=true;
            if(environmentMotion!=null)environmentMotion.Prepare(impactTimes, primaryImpactCount);
            if(audioCue!=null)audioCue.Begin(durationSeconds);
            if(extraDust!=null){extraDust.Play();extraDust.Emit(openingDustCount);}
            if(dustAtmosphere!=null)dustAtmosphere.Begin();
            onStarted.Invoke();
        }
        public void Advance(float seconds)
        {
            if(!IsRunning)return;
            ElapsedSeconds+=Mathf.Max(0f,seconds);
            float progress=Mathf.Clamp01(ElapsedSeconds/durationSeconds);
            float onset=Mathf.Clamp01(ElapsedSeconds/0.8f);
            float tail=1f-Mathf.Clamp01((ElapsedSeconds-Mathf.Max(0f,durationSeconds-1.8f))/1.8f);
            float swell=Mathf.Sin(ElapsedSeconds*1.8f);
            float energy=onset*tail*(0.78f+0.22f*swell*swell);
            if(environmentMotion!=null)environmentMotion.Apply(ElapsedSeconds,progress,energy);
            if(debris!=null)debris.Apply(ElapsedSeconds);
            foreach(var section in fractureSections)if(section!=null)section.Apply(ElapsedSeconds);
            while(nextImpact<impactTimes.Length && ElapsedSeconds>=impactTimes[nextImpact])
            {
                nextImpact++;bool guarded=protection!=null && protection.IsProtected;
                if(guarded)GuardedImpacts++;
                else{UnprotectedImpacts++;if(hitFeedback!=null)hitFeedback.PlayHit();onUnprotectedImpact.Invoke();}
                bool primary = nextImpact <= primaryImpactCount;
                if(extraDust!=null)extraDust.Emit(primary ? impactDustCount : lateImpactDustCount);
                if(primary && dustAtmosphere!=null)dustAtmosphere.Fracture();
                if(audioCue!=null)audioCue.Impact();
                ImpactResolved?.Invoke(guarded);
            }
            Advanced?.Invoke(ElapsedSeconds);
            if(ElapsedSeconds<durationSeconds)return;
            IsRunning=false;
            if(environmentMotion!=null)environmentMotion.Apply(durationSeconds,1f,0f);
            if(audioCue!=null)audioCue.Stop();
            if(extraDust!=null)extraDust.Stop(false,ParticleSystemStopBehavior.StopEmitting);
            if(dustAtmosphere!=null)dustAtmosphere.End();
            onFinished.Invoke();Finished?.Invoke();
        }
        public void ResetEffect()
        {
            IsRunning=false;ElapsedSeconds=0f;nextImpact=0;GuardedImpacts=0;UnprotectedImpacts=0;
            if(environmentMotion!=null)environmentMotion.Restore();if(debris!=null)debris.ResetEffect();
            foreach(var section in fractureSections)if(section!=null)section.ResetEffect();
            if(hitFeedback!=null)hitFeedback.Clear();if(audioCue!=null)audioCue.Stop();
            if(extraDust!=null)extraDust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(dustAtmosphere!=null)dustAtmosphere.Clear();
            EffectReset?.Invoke();
        }
        void OnDisable(){ResetEffect();}
    }
}
