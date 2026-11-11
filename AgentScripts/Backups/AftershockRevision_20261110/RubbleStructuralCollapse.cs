using System;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Individual load-bearing motion, visible fracture, and physical release at each joint.</summary>
    public sealed class RubbleStructuralCollapse : MonoBehaviour
    {
        [Serializable] public sealed class Element
        {
            public Transform target;
            public AftershockFallingChunk loose;
            public float phase, frequency = 29f, distance = .006f, degrees = .6f;
            public Vector3 compression;
            public float settlesAt = 4f, releaseAt = -1f;
            public LineRenderer[] cracks = Array.Empty<LineRenderer>();
            public ParticleSystem powder;
            [NonSerialized] public Vector3 restPosition;
            [NonSerialized] public Quaternion restRotation;
            [NonSerialized] public bool strained, fractured;
            [NonSerialized] public Rigidbody body;
        }
        [SerializeField] AftershockDirector aftershock;
        [SerializeField] Element[] elements = Array.Empty<Element>();
        bool captured;
        bool pending;
        float pendingSeconds;
        public Element[] Elements => elements;
        public int Fractures { get; private set; }
        public int MovingSupports { get; private set; }
        void Awake() { Capture(); }
        void Capture()
        {
            if(captured)return;
            foreach(var e in elements)if(e.target!=null){e.restPosition=e.target.localPosition;e.restRotation=e.target.localRotation;e.body=e.target.GetComponent<Rigidbody>();}
            captured=true;
        }
        void OnEnable(){Capture();if(aftershock!=null){aftershock.Advanced+=Apply;aftershock.EffectReset+=ResetEffect;}}
        void OnDisable(){if(aftershock!=null){aftershock.Advanced-=Apply;aftershock.EffectReset-=ResetEffect;}ResetEffect();}
        public void Apply(float seconds)
        {
            pendingSeconds=seconds;pending=true;
        }
        void FixedUpdate(){if(pending){pending=false;ApplyMotion(pendingSeconds);}}
        void ApplyMotion(float seconds)
        {
            Capture();float envelope=Mathf.Clamp01(seconds/.55f)*Mathf.Clamp01((18f-seconds)/1.3f);MovingSupports=0;
            foreach(var e in elements)
            {
                if(e.target==null)continue;
                if(e.loose!=null&&e.loose.Released){CompleteFracture(e);continue;}
                float strain=e.releaseAt>0?Mathf.Clamp01((seconds-e.releaseAt+1.1f)/1.1f):0;
                float t=seconds*e.frequency+e.phase;
                var wave=new Vector3(Mathf.Sin(t)+.31f*Mathf.Sin(t*1.63f+e.phase),Mathf.Sin(t*.81f+1.7f)*.45f,Mathf.Sin(t*1.19f+e.phase*2f)*.67f);
                var rotation=new Vector3(Mathf.Sin(t*.91f),Mathf.Sin(t*.73f+e.phase)*.55f,Mathf.Cos(t*1.31f+1.2f));
                float load=Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-e.settlesAt)/2.1f));
                var position=e.restPosition+wave*e.distance*envelope*(1+strain)+e.compression*load;
                var pose=e.restRotation*Quaternion.Euler(rotation*e.degrees*envelope*(1+strain*1.4f));
                if(e.body!=null&&e.body.isKinematic){var parent=e.target.parent;e.body.MovePosition(parent!=null?parent.TransformPoint(position):position);e.body.MoveRotation(parent!=null?parent.rotation*pose:pose);}
                else{e.target.localPosition=position;e.target.localRotation=pose;}
                if(envelope>0)MovingSupports++;
                foreach(var crack in e.cracks)if(crack!=null){crack.enabled=strain>.02f;crack.widthMultiplier=Mathf.Lerp(.0005f,.0055f,strain);}
                if(strain>.35f&&!e.strained){e.strained=true;if(e.powder!=null)e.powder.Emit(7);}
                if(e.loose!=null&&e.releaseAt>0){e.loose.Apply(seconds-e.releaseAt);if(e.loose.Released)CompleteFracture(e);}
            }
        }
        void CompleteFracture(Element e)
        {
            if(e.fractured)return;e.fractured=true;Fractures++;
            foreach(var crack in e.cracks)if(crack!=null)crack.enabled=false;
            if(e.powder!=null)e.powder.Emit(38);
        }
        public void ResetEffect()
        {
            if(!captured)return;
            pending=false;
            foreach(var e in elements)
            {
                if(e.loose!=null)e.loose.ResetEffect();
                if(e.target!=null){e.target.localPosition=e.restPosition;e.target.localRotation=e.restRotation;}
                if(e.body!=null){e.body.position=e.target.position;e.body.rotation=e.target.rotation;}
                foreach(var c in e.cracks)if(c!=null)c.enabled=false;
                if(e.powder!=null)e.powder.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                e.strained=e.fractured=false;
            }
            Fractures=MovingSupports=0;
        }
    }
}
