using System;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Intermittent contact rocking, joint opening, and physical release. Never drives tracking.</summary>
    public sealed class RubbleStructuralCollapse : MonoBehaviour
    {
        internal struct ContactPulse
        {
            public float onset, duration, sign, damping, cycles, slide;
        }
        [Serializable] public sealed class Element
        {
            public Transform target;
            public AftershockFallingChunk loose;
            public float phase, frequency = 29f, distance = .006f, degrees = .6f;
            public Vector3 compression;
            public float settlesAt = 4f, releaseAt = -1f;
            public Vector3 openingOffset;
            public LineRenderer[] cracks = Array.Empty<LineRenderer>();
            public ParticleSystem powder;
            [NonSerialized] public Vector3 restPosition;
            [NonSerialized] public Quaternion restRotation;
            [NonSerialized] public bool strained, fractured;
            [NonSerialized] public Rigidbody body;
            [NonSerialized] public Vector3 bearing, rockAxis, slideAxis;
            [NonSerialized] public float seed, inertia;
            [NonSerialized] internal ContactPulse[] responses;
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
            foreach(var e in elements)
            {
                if(e.target==null)continue;
                e.restPosition=e.target.localPosition;e.restRotation=e.target.localRotation;
                e.body=e.target.GetComponent<Rigidbody>();
                e.seed=Mathf.Repeat(e.phase*.6180339f,1f);
                var filter=e.target.GetComponent<MeshFilter>();
                var bounds=filter!=null&&filter.sharedMesh!=null?filter.sharedMesh.bounds:new Bounds(Vector3.zero,Vector3.one*.2f);
                var surface=e.target.GetComponent<Renderer>();
                e.inertia=Mathf.Clamp01((surface!=null?surface.bounds.size.magnitude:bounds.size.magnitude)/1.15f);
                // Pick a low point on the actual mesh. A floating centre pivot makes heavy rubble look weightless.
                var desired=bounds.center+new Vector3((Noise(e.seed,2)-.5f)*bounds.size.x,-bounds.extents.y,(Noise(e.seed,3)-.5f)*bounds.size.z);
                e.bearing=desired;
                if(filter!=null&&filter.sharedMesh!=null&&filter.sharedMesh.isReadable)
                {
                    float closest=float.MaxValue;
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        float cost=(vertex-desired).sqrMagnitude+Mathf.Max(0,vertex.y-bounds.min.y)*.15f;
                        if(cost<closest){closest=cost;e.bearing=vertex;}
                    }
                }
                e.bearing=Vector3.Scale(e.bearing,e.target.localScale);
                float direction=Noise(e.seed,4)*Mathf.PI*2;
                e.rockAxis=new Vector3(Mathf.Cos(direction),0,Mathf.Sin(direction));
                e.slideAxis=new Vector3(-e.rockAxis.z,0,e.rockAxis.x);
                e.responses=new ContactPulse[4];
                for(int pulse=0;pulse<e.responses.Length;pulse++)
                {
                    bool active=pulse==0||Noise(e.seed,10+pulse)>=.23f;
                    e.responses[pulse]=new ContactPulse
                    {
                        onset=pulse*3.9f+.18f+Noise(e.seed,20+pulse)*(pulse==0?1.05f:2.0f),
                        duration=Mathf.Lerp(.75f,2.3f,e.inertia)*Mathf.Lerp(.82f,1.3f,Noise(e.seed,30+pulse)),
                        sign=active?(Noise(e.seed,40+pulse)<.5f?-1f:1f):0,
                        damping=Mathf.Lerp(1.2f,3.6f,Noise(e.seed,50+pulse)),
                        cycles=Mathf.Lerp(.7f,1.35f,Noise(e.seed,60+pulse)),
                        slide=Mathf.Lerp(.2f,.6f,Noise(e.seed,70+pulse))
                    };
                }
                // Keep the old authored lines as archived objects, with no rendered ink substitute for a fracture.
                foreach(var line in e.cracks)if(line!=null)line.enabled=false;
            }
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
            Capture();MovingSupports=0;
            foreach(var e in elements)
            {
                if(e.target==null)continue;
                if(e.loose!=null&&e.loose.Released){CompleteFracture(e);continue;}
                float strain=e.releaseAt>0?Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-e.releaseAt+.75f)/.75f)):0;
                // A few local load responses with real pauses. Each has its own onset, duration,
                // direction and damping; changing only the phase of a continuous sine is still a buzz.
                float rock=0,slip=0;
                foreach(var pulse in e.responses)
                {
                    float u=(seconds-pulse.onset)/pulse.duration;
                    if(pulse.sign==0||u<=0||u>=1)continue;
                    float envelope=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.12f))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.56f)/.44f)));
                    float response=Mathf.Sin(u*Mathf.PI*2*pulse.cycles)*envelope*Mathf.Exp(-u*pulse.damping);
                    rock+=pulse.sign*response;
                    slip+=pulse.sign*response*pulse.slide;
                }
                float load=Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-e.settlesAt)/2.1f));
                float angle=rock*Mathf.Min(e.degrees*.32f,.5f)*(1+strain*.7f);
                var delta=Quaternion.AngleAxis(angle,e.rockAxis);
                var position=e.restPosition+e.restRotation*(e.bearing-delta*e.bearing+e.slideAxis*(slip*e.distance*.35f))+e.compression*load+e.openingOffset*strain;
                var pose=e.restRotation*delta;
                if(e.body!=null&&e.body.isKinematic){var parent=e.target.parent;e.body.MovePosition(parent!=null?parent.TransformPoint(position):position);e.body.MoveRotation(parent!=null?parent.rotation*pose:pose);}
                else{e.target.localPosition=position;e.target.localRotation=pose;}
                if(Mathf.Abs(rock)>.015f)MovingSupports++;
                if(strain>.55f&&!e.strained){e.strained=true;if(e.powder!=null)e.powder.Emit(4);}
                if(e.loose!=null&&e.releaseAt>0){e.loose.Apply(seconds-e.releaseAt);if(e.loose.Released)CompleteFracture(e);}
            }
        }
        static float Noise(float seed,int salt)
        {
            // Deterministic per-contact parameters, calculated without per-frame Random state or allocations.
            float value=Mathf.Sin(seed*127.1f+salt*311.7f)*43758.5453f;
            return value-Mathf.Floor(value);
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
