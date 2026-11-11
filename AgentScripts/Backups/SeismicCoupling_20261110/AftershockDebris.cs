using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    public sealed class AftershockDebris : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform rightHand;
        [SerializeField] Transform[] fragments=System.Array.Empty<Transform>();
        [SerializeField] float[] releaseTimes=System.Array.Empty<float>();
        [SerializeField] Collider[] protectedColliders=System.Array.Empty<Collider>();
        Vector3[] starts;
        Vector3[] previousPositions;
        Quaternion[] rotations;
        bool[] hiddenForSafety;
        bool[] released;
        Rigidbody[] bodies;
        Collider[] shapes;
        Transform[] parents;
        void Awake()
        {
            int count=fragments.Length;
            starts=new Vector3[count];previousPositions=new Vector3[count];rotations=new Quaternion[count];hiddenForSafety=new bool[count];released=new bool[count];bodies=new Rigidbody[count];shapes=new Collider[count];parents=new Transform[count];
            for(int i=0;i<count;i++)
            {
                var f=fragments[i];if(f==null)continue;starts[i]=f.position;rotations[i]=f.rotation;parents[i]=f.parent;
                var shape=f.GetComponent<MeshCollider>();if(shape==null)shape=f.gameObject.AddComponent<MeshCollider>();
                shape.sharedMesh=f.GetComponent<MeshFilter>().sharedMesh;shape.convex=true;shape.contactOffset=.001f;shapes[i]=shape;
                var body=f.GetComponent<Rigidbody>();if(body==null)body=f.gameObject.AddComponent<Rigidbody>();
                body.mass=.025f;body.linearDamping=.18f;body.angularDamping=.75f;body.interpolation=RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.maxDepenetrationVelocity=.45f;body.maxAngularVelocity=12f;bodies[i]=body;
            }
            ResetEffect();
        }
        public void Apply(float elapsed)
        {
            if(starts==null)return;
            for(int i=0;i<fragments.Length;i++)
            {
                if(fragments[i]==null||released[i]||hiddenForSafety[i])continue;
                float release = i < releaseTimes.Length ? releaseTimes[i] : 1.3f+i*0.42f;
                if(elapsed<release)continue;
                fragments[i].SetParent(transform,true);fragments[i].gameObject.SetActive(true);shapes[i].enabled=true;
                foreach(var other in protectedColliders)if(other!=null)Physics.IgnoreCollision(shapes[i],other,true);
                for(int j=0;j<shapes.Length;j++)if(j!=i&&shapes[j]!=null&&shapes[j].enabled)Physics.IgnoreCollision(shapes[i],shapes[j],true);
                bodies[i].isKinematic=false;bodies[i].useGravity=true;
                bodies[i].linearVelocity=new Vector3(Mathf.Sin(i*2.3f)*.055f,-.08f,Mathf.Cos(i*1.7f)*.045f);
                bodies[i].angularVelocity=new Vector3(1.2f+i%3,.8f,1.6f)*(i%2==0?1:-1);released[i]=true;
            }
        }
        public void ResetEffect()
        {
            if(starts==null)return;
            bool restoreHierarchy=Application.isPlaying&&gameObject.activeInHierarchy;
            for(int i=0;i<fragments.Length;i++)
            {
                if(fragments[i]==null||bodies[i]==null)continue;
                if(!bodies[i].isKinematic){bodies[i].linearVelocity=Vector3.zero;bodies[i].angularVelocity=Vector3.zero;}
                bodies[i].isKinematic=true;bodies[i].useGravity=false;shapes[i].enabled=false;
                // Play mode/scene teardown cannot reparent children of a deactivating Game Flow.
                if(!restoreHierarchy)continue;
                if(parents[i]!=null)fragments[i].SetParent(parents[i],true);
                fragments[i].SetPositionAndRotation(starts[i],rotations[i]);bodies[i].position=starts[i];bodies[i].rotation=rotations[i];
                // Intact chips are already attached to a seam before release, rather than spawning in air.
                previousPositions[i]=starts[i];released[i]=hiddenForSafety[i]=false;fragments[i].gameObject.SetActive(true);
            }
        }
        void LateUpdate()
        {
            if(bodies==null)return;
            for(int i=0;i<fragments.Length;i++)
            {
                if(!released[i]||hiddenForSafety[i]||fragments[i]==null)continue;
                var position=fragments[i].position;
                if(position.y<-.6f||NearPath(head,previousPositions[i],position,.29f)||NearPath(leftHand,previousPositions[i],position,.12f)||NearPath(rightHand,previousPositions[i],position,.12f))
                {hiddenForSafety[i]=true;bodies[i].isKinematic=true;bodies[i].useGravity=false;shapes[i].enabled=false;fragments[i].gameObject.SetActive(false);}
                previousPositions[i]=position;
            }
        }
        static bool NearPath(Transform tracked, Vector3 start, Vector3 end, float radius)
        {
            if (tracked == null) return false;
            Vector3 path = end - start;
            float t = path.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector3.Dot(tracked.position-start,path)/path.sqrMagnitude) : 0f;
            return (tracked.position - (start + path*t)).sqrMagnitude < radius*radius;
        }
        void OnDisable(){ResetEffect();}
    }
}
