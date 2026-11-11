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
        [SerializeField, Min(1f)] float durationSeconds=AftershockEnvelope.MotionEnd;
        [SerializeField, Min(1)] int maximumHeadImpacts = 4;
        [SerializeField] AftershockFractureSection[] fractureSections = Array.Empty<AftershockFractureSection>();
        [SerializeField] UnityEvent onStarted=new UnityEvent();
        [SerializeField] UnityEvent onFinished=new UnityEvent();
        [SerializeField] UnityEvent onUnprotectedImpact=new UnityEvent();
        int nextImpact;
        float lastPhysicalImpact=-10f;
        public bool IsRunning{get;private set;}
        public float ElapsedSeconds{get;private set;}
        public int GuardedImpacts{get;private set;}
        public int UnprotectedImpacts{get;private set;}
        public float DurationSeconds => durationSeconds;
        public int ImpactCount => maximumHeadImpacts;
        public float MotionStrength => AftershockEnvelope.Motion(ElapsedSeconds)*(environmentMotion!=null?environmentMotion.ExcitationScale:0f);
        public event Action<bool> ImpactResolved;
        public event Action Finished;
        public event Action<float> Advanced;
        public event Action EffectReset;
        public void Begin()
        {
            ResetEffect();IsRunning=true;
            if(environmentMotion!=null)environmentMotion.Prepare();
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
            float energy=MotionStrength;
            if(audioCue!=null)audioCue.SetStrength(energy);
            if(environmentMotion!=null)environmentMotion.Apply(ElapsedSeconds,progress,energy);
            if(debris!=null)debris.Apply(ElapsedSeconds);
            // Structural failures and contact sounds come from real load/contact, never scheduled explosions.
            Advanced?.Invoke(ElapsedSeconds);
            if(ElapsedSeconds<durationSeconds)return;
            IsRunning=false;
            if(environmentMotion!=null)environmentMotion.Apply(durationSeconds,1f,0f);
            if(audioCue!=null)audioCue.Stop();
            if(extraDust!=null)extraDust.Stop(false,ParticleSystemStopBehavior.StopEmitting);
            if(dustAtmosphere!=null)dustAtmosphere.End();
            onFinished.Invoke();Finished?.Invoke();
        }
        public void ResolvePhysicalImpact(Vector3 position,float speed)
        {
            if(!IsRunning||speed<.5f||ElapsedSeconds-lastPhysicalImpact<.45f)return;
            lastPhysicalImpact=ElapsedSeconds;
            if(audioCue!=null)audioCue.Impact();
            // Remote rubble landing must not produce a head-hit overlay.
            var camera=Camera.main;
            if(camera==null||Vector3.Distance(camera.transform.position,position)>.38f||nextImpact>=ImpactCount)return;
            nextImpact++;bool guarded=protection!=null&&protection.IsProtected;
            if(guarded)GuardedImpacts++;
            else{UnprotectedImpacts++;if(hitFeedback!=null)hitFeedback.PlayHit();onUnprotectedImpact.Invoke();}
            ImpactResolved?.Invoke(guarded);
        }
        public void ResetEffect()
        {
            IsRunning=false;ElapsedSeconds=0f;nextImpact=0;lastPhysicalImpact=-10f;GuardedImpacts=0;UnprotectedImpacts=0;
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
