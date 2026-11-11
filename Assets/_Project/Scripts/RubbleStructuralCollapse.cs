using System;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Common ground motion loads independent damped bearings; accumulated load causes support loss.</summary>
    [DefaultExecutionOrder(-60)]
    public sealed class RubbleStructuralCollapse : MonoBehaviour
    {
        [Serializable] public sealed class Element
        {
            public Transform target;
            public AftershockFallingChunk loose;
            public float phase, frequency = 29f, distance = .006f, degrees = .6f;
            public Vector3 compression;
            public float settlesAt = 4f, releaseAt = -1f;
            public Vector3 openingOffset;
            [Min(0)] public float failureResistance;
            public LineRenderer[] cracks = Array.Empty<LineRenderer>();
            public ParticleSystem powder;
            [NonSerialized] public Vector3 restPosition;
            [NonSerialized] public Quaternion restRotation;
            [NonSerialized] public bool strained, fractured;
            [NonSerialized] public Rigidbody body;
            [NonSerialized] public Vector3 bearing, rockAxis, slideAxis;
            [NonSerialized] public float seed, inertia, damage, response, responseSpeed;
            [NonSerialized] public Matrix4x4 parentMatrix;
            [NonSerialized] public Quaternion parentRotation;
            [NonSerialized] public Vector3 previousWorld;
            [NonSerialized] public Quaternion previousRotation;
        }
        [SerializeField] AftershockDirector aftershock;
        [SerializeField] AftershockEnvironmentMotion ground;
        [SerializeField] Element[] elements = Array.Empty<Element>();
        bool captured, pending;
        float pendingSeconds, previousSeconds;
        public Element[] Elements => elements;
        public int Fractures { get; private set; }
        public int MovingSupports { get; private set; }
        void Awake() { Capture(); }
        void Capture()
        {
            if(captured)return;
            if(ground==null)ground=FindFirstObjectByType<AftershockEnvironmentMotion>();
            foreach(var e in elements)
            {
                if(e.target==null)continue;
                e.restPosition=e.target.localPosition;e.restRotation=e.target.localRotation;
                e.parentMatrix=e.target.parent!=null?e.target.parent.localToWorldMatrix:Matrix4x4.identity;
                e.parentRotation=e.target.parent!=null?e.target.parent.rotation:Quaternion.identity;
                e.previousWorld=e.target.position;e.previousRotation=e.target.rotation;
                e.body=e.target.GetComponent<Rigidbody>();
                e.seed=Mathf.Repeat(e.phase*.6180339f,1f);
                var filter=e.target.GetComponent<MeshFilter>();
                var bounds=filter!=null&&filter.sharedMesh!=null?filter.sharedMesh.bounds:new Bounds(Vector3.zero,Vector3.one*.2f);
                var surface=e.target.GetComponent<Renderer>();
                e.inertia=Mathf.Clamp01((surface!=null?surface.bounds.size.magnitude:bounds.size.magnitude)/1.15f);
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
                foreach(var line in e.cracks)if(line!=null)line.enabled=false;
            }
            captured=true;
        }
        void OnEnable(){Capture();if(aftershock!=null){aftershock.Advanced+=Apply;aftershock.EffectReset+=ResetEffect;}}
        void OnDisable(){if(aftershock!=null){aftershock.Advanced-=Apply;aftershock.EffectReset-=ResetEffect;}ResetEffect();}
        public void Apply(float seconds){pendingSeconds=seconds;pending=true;}
        void FixedUpdate(){if(pending){pending=false;ApplyMotion(pendingSeconds);}}
        void ApplyMotion(float seconds)
        {
            Capture();MovingSupports=0;
            float dt=Mathf.Clamp(seconds-previousSeconds,0,.1f);previousSeconds=seconds;
            foreach(var e in elements)
            {
                if(e.target==null)continue;
                if(e.loose!=null&&e.loose.Released){CompleteFracture(e);continue;}
                float load=ground!=null?ground.Load:0;
                e.damage+=load*dt;
                float strain=e.failureResistance>0?Mathf.Clamp01(e.damage/e.failureResistance):0;
                // Different mass, bearing directions and resonances respond to the SAME ground acceleration.
                float hz=Mathf.Lerp(1.9f,.72f,e.inertia)*Mathf.Lerp(.85f,1.18f,Noise(e.seed,8));
                float omega=hz*Mathf.PI*2;
                var acceleration=ground!=null?Quaternion.Inverse(e.parentRotation)*ground.GroundAcceleration:Vector3.zero;
                float force=Vector3.Dot(acceleration,e.slideAxis)*omega*omega*.45f;
                e.responseSpeed+=(force-omega*omega*e.response-2*Mathf.Lerp(.24f,.55f,Noise(e.seed,9))*omega*e.responseSpeed)*dt;
                e.response=Mathf.Clamp(e.response+e.responseSpeed*dt,-1.5f,1.5f);
                if(seconds>=AftershockEnvelope.MotionEnd){e.response=e.responseSpeed=0;}
                float angle=e.response*Mathf.Min(e.degrees,1.2f)*(1+strain*.25f);
                var delta=Quaternion.AngleAxis(angle,e.rockAxis);
                float creep=Mathf.SmoothStep(0,1,Mathf.Clamp01((strain-.4f)/.6f));
                var local=e.restPosition+e.restRotation*(e.bearing-delta*e.bearing+e.slideAxis*(e.response*e.distance*.45f))
                    +e.compression*Mathf.Clamp01(e.damage*.4f)+Vector3.ClampMagnitude(e.openingOffset,.035f)*creep;
                var world=e.parentMatrix.MultiplyPoint3x4(local);
                var rotation=e.parentRotation*e.restRotation*delta;
                if(ground!=null){world=ground.MapPosition(world);rotation=ground.MapRotation(rotation);}
                var velocity=dt>.0001f?(world-e.previousWorld)/dt:Vector3.zero;
                var turn=rotation*Quaternion.Inverse(e.previousRotation);turn.ToAngleAxis(out float degrees,out var axis);
                if(degrees>180)degrees-=360;
                var spin=dt>.0001f&&axis.sqrMagnitude>.1f?axis*(degrees*Mathf.Deg2Rad/dt):Vector3.zero;
                if(e.body!=null&&e.body.isKinematic){e.body.MovePosition(world);e.body.MoveRotation(rotation);}
                else e.target.SetPositionAndRotation(world,rotation);
                e.previousWorld=world;e.previousRotation=rotation;
                if(Mathf.Abs(e.response)>.015f)MovingSupports++;
                if(strain>.55f&&!e.strained){e.strained=true;if(e.powder!=null)e.powder.Emit(3);}
                if(e.loose!=null&&e.failureResistance>0&&strain>=1)
                {
                    // Transfer the bearing's measured motion, with no authored outward launch or spin.
                    e.loose.ReleaseFromSupport(velocity,spin);CompleteFracture(e);
                }
            }
        }
        static float Noise(float seed,int salt){float value=Mathf.Sin(seed*127.1f+salt*311.7f)*43758.5453f;return value-Mathf.Floor(value);}
        void CompleteFracture(Element e)
        {
            if(e.fractured)return;e.fractured=true;Fractures++;
            foreach(var crack in e.cracks)if(crack!=null)crack.enabled=false;
            if(e.powder!=null)e.powder.Emit(5);
        }
        public void ResetEffect()
        {
            if(!captured)return;
            pending=false;previousSeconds=0;
            foreach(var e in elements)
            {
                if(e.loose!=null)e.loose.ResetEffect();
                if(e.target!=null){e.target.localPosition=e.restPosition;e.target.localRotation=e.restRotation;e.previousWorld=e.target.position;e.previousRotation=e.target.rotation;}
                if(e.body!=null&&e.target!=null){e.body.position=e.target.position;e.body.rotation=e.target.rotation;}
                foreach(var c in e.cracks)if(c!=null)c.enabled=false;
                if(e.powder!=null)e.powder.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                e.strained=e.fractured=false;e.damage=e.response=e.responseSpeed=0;
            }
            Fractures=MovingSupports=0;
        }
    }
}
