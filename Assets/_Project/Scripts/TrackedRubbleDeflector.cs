using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>A kinematic contact surface for rubble; tracking remains the sole owner of the view pose.</summary>
    [RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
    public sealed class TrackedRubbleDeflector : MonoBehaviour
    {
        [SerializeField] Transform trackedHead;
        [SerializeField] Collider[] ignored = System.Array.Empty<Collider>();
        Rigidbody body;
        void Awake()
        {
            body=GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
            var shape=GetComponent<SphereCollider>();foreach(var other in ignored)if(other!=null&&other!=shape)Physics.IgnoreCollision(shape,other,true);
            if(trackedHead!=null)body.position=trackedHead.position;
        }
        void FixedUpdate(){if(trackedHead!=null)body.MovePosition(trackedHead.position);}
    }
}
