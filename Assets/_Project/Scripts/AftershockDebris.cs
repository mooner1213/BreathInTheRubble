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
        AftershockEnvironmentMotion ground;
        float damage,previousSeconds;
        public Transform[] Fragments => fragments;
        void Awake()
        {
            ground=FindFirstObjectByType<AftershockEnvironmentMotion>();
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
            damage+=(ground!=null?ground.Load:0)*Mathf.Clamp(elapsed-previousSeconds,0,.1f);previousSeconds=elapsed;
            for(int i=0;i<fragments.Length;i++)
            {
                if(fragments[i]==null||released[i]||hiddenForSafety[i])continue;
                float resistance=Mathf.Lerp(.58f,1.5f,i/(float)Mathf.Max(1,fragments.Length-1));
                if(ground!=null){bodies[i].MovePosition(ground.MapPosition(starts[i]));bodies[i].MoveRotation(ground.MapRotation(rotations[i]));}
                if(damage<resistance)continue;
                fragments[i].SetParent(transform,true);fragments[i].gameObject.SetActive(true);shapes[i].enabled=true;
                foreach(var other in protectedColliders)if(other!=null)Physics.IgnoreCollision(shapes[i],other,true);
                for(int j=0;j<shapes.Length;j++)if(j!=i&&shapes[j]!=null&&shapes[j].enabled)Physics.IgnoreCollision(shapes[i],shapes[j],true);
                bodies[i].isKinematic=false;bodies[i].useGravity=true;
                bodies[i].linearVelocity=Vector3.ClampMagnitude(ground!=null?ground.GroundVelocity:Vector3.zero,.12f);
                bodies[i].angularVelocity=Vector3.zero;released[i]=true;
            }
        }
        public void ResetEffect()
        {
            if(starts==null)return;
            damage=previousSeconds=0;
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
