using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Shared ground excitation. Tracking and freely moving or held objects never inherit its transform.</summary>
    [DefaultExecutionOrder(-80)]
    public sealed class AftershockEnvironmentMotion : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Transform environmentRoot;
        [SerializeField, Range(0f, 1.5f)] float excitationScale = 1f;
        [SerializeField, Range(0f, .035f)] float roomShakeDistance = .020f;
        [SerializeField, Range(0f, 1f)] float roomShakeDegrees = .12f;
        sealed class Support { public Transform target; public Rigidbody body; public Vector3 position; public Quaternion rotation; }
        readonly List<Support> supports = new List<Support>();
        Vector3 pivot;
        bool pending, captured;
        public float ExcitationScale { get => excitationScale; set => excitationScale = Mathf.Clamp(value, 0, 1.5f); }
        public Vector3 GroundOffset { get; private set; }
        public Quaternion GroundRotation { get; private set; } = Quaternion.identity;
        public Vector3 GroundVelocity { get; private set; }
        public Vector3 GroundAcceleration { get; private set; }
        public float Load => Mathf.Pow(Mathf.Max(0, GroundAcceleration.magnitude / 1.4f - .16f), 1.25f);
        public int DrivenSupports => supports.Count;
        public float AppliedMaximumDrop => 0;
        public float MaximumConfiguredDrop => 0;
        public Vector3 DesktopShakeOffset => Vector3.zero;
        public Vector3 DesktopShakeEuler => Vector3.zero;
        public void Prepare() { Capture(); }
        void Awake() { Capture(); }
        void Capture()
        {
            if(captured)return;
            pivot=head!=null?head.position:Vector3.up;
            if(environmentRoot==null)return;
            var manager=FindFirstObjectByType<RubbleStructuralCollapse>();
            var managed=new HashSet<Transform>();
            if(manager!=null)foreach(var e in manager.Elements)if(e.target!=null)managed.Add(e.target);
            var debris=FindFirstObjectByType<AftershockDebris>();
            if(debris!=null)foreach(var fragment in debris.Fragments)if(fragment!=null)managed.Add(fragment);
            var owners=new HashSet<Transform>();
            foreach(var renderer in environmentRoot.GetComponentsInChildren<MeshRenderer>())
                if(renderer.enabled)owners.Add(Owner(renderer.transform));
            foreach(var collider in environmentRoot.GetComponentsInChildren<Collider>())
                if(collider.enabled&&!collider.isTrigger)owners.Add(Owner(collider.transform));
            foreach(var owner in owners)
            {
                if(owner==null||owner.GetComponentInParent<XRGrabInteractable>()!=null)continue;
                bool excluded=false;
                for(var p=owner;p!=null&&p!=environmentRoot;p=p.parent)
                    if(managed.Contains(p)||p.name=="Tracked rubble contact"){excluded=true;break;}
                if(excluded)continue;
                var body=owner.GetComponent<Rigidbody>();
                if(body!=null&&!body.isKinematic)continue;
                supports.Add(new Support{target=owner,body=body,position=owner.position,rotation=owner.rotation});
            }
            supports.Sort((a,b)=>Depth(a.target).CompareTo(Depth(b.target)));
            captured=true;
        }
        static int Depth(Transform t){int n=0;while(t.parent!=null){n++;t=t.parent;}return n;}
        static Transform Owner(Transform t){var body=t.GetComponentInParent<Rigidbody>();return body!=null?body.transform:t;}
        Vector3 Offset(float t)
        {
            float a=AftershockEnvelope.Motion(t)*roomShakeDistance*excitationScale, tau=t*Mathf.PI*2;
            return new Vector3(.58f*Mathf.Sin(tau*.92f)+.28f*Mathf.Sin(tau*1.67f+.7f)+.14f*Mathf.Sin(tau*2.31f+1.9f),
                .16f*Mathf.Sin(tau*1.24f+.4f), .55f*Mathf.Sin(tau*.78f+.9f)+.2f*Mathf.Sin(tau*1.43f))*a;
        }
        public void Apply(float elapsed,float progress,float energy)
        {
            Capture();GroundOffset=Offset(elapsed);
            var before=Offset(elapsed-.01f);var after=Offset(elapsed+.01f);
            GroundVelocity=(after-before)/.02f;
            GroundAcceleration=(after-2*GroundOffset+before)/.0001f;
            float tau=elapsed*Mathf.PI*2,a=AftershockEnvelope.Motion(elapsed)*roomShakeDegrees*excitationScale;
            GroundRotation=Quaternion.Euler(Mathf.Sin(tau*.83f)*a*.5f,Mathf.Sin(tau*.67f+.4f)*a*.25f,Mathf.Sin(tau*.92f)*a);
            pending=true;
        }
        public Vector3 MapPosition(Vector3 rest)=>pivot+GroundRotation*(rest-pivot)+GroundOffset;
        public Quaternion MapRotation(Quaternion rest)=>GroundRotation*rest;
        void FixedUpdate()
        {
            if(!pending)return;pending=false;
            foreach(var s in supports)
            {
                if(s.target==null||!s.target.gameObject.activeInHierarchy)continue;
                if(s.body!=null){if(!s.body.isKinematic)continue;s.body.MovePosition(MapPosition(s.position));s.body.MoveRotation(MapRotation(s.rotation));}
                else s.target.SetPositionAndRotation(MapPosition(s.position),MapRotation(s.rotation));
            }
        }
        public void Restore()
        {
            pending=false;GroundOffset=GroundVelocity=GroundAcceleration=Vector3.zero;GroundRotation=Quaternion.identity;
            foreach(var s in supports)if(s.target!=null)
            {
                s.target.SetPositionAndRotation(s.position,s.rotation);
                if(s.body!=null&&s.body.isKinematic){s.body.position=s.position;s.body.rotation=s.rotation;}
            }
        }
        void OnDisable(){Restore();}
    }
}
