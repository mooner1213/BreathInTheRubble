using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PipeImpactSurface : MonoBehaviour
    {
        public enum SurfaceKind { Concrete, Metal }
        [SerializeField] SurfaceKind kind;
        public SurfaceKind Kind => kind;
    }
}
