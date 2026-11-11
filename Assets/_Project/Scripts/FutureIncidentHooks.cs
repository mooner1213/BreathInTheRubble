using UnityEngine;
using UnityEngine.Events;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Reserved scene references. No screams or rat movement are scheduled yet.</summary>
    public sealed class FutureIncidentHooks:MonoBehaviour
    {
        [SerializeField] Transform[] distantScreamPositions=System.Array.Empty<Transform>();
        [SerializeField] Transform[] ratRoutePoints=System.Array.Empty<Transform>();
        [SerializeField] UnityEvent onDistantScreamRequested=new UnityEvent();
        [SerializeField] UnityEvent onRatPassRequested=new UnityEvent();
        public void RequestDistantScream(){onDistantScreamRequested.Invoke();}
        public void RequestRatPass(){onRatPassRequested.Invoke();}
    }
}
