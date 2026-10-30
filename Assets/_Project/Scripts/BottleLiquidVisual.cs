using System.Collections.Generic;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>A closed bottle's visual liquid volume. No XR or gameplay dependencies.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class BottleLiquidVisual : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float fillFraction = 0.45f;
        [SerializeField] Mesh interiorMesh;
        readonly List<Vector3> vertices = new(1024);
        readonly List<int> triangles = new(2048);
        readonly List<Vector3> rim = new(256);
        readonly Vector3[] polygon = new Vector3[4];
        Mesh liquidMesh;
        Vector3[] source;
        int[] indices;
        Vector3 lastNormal;
        float totalVolume;
        public float FillFraction => fillFraction;

        void Awake()
        {
            if (interiorMesh == null) { enabled = false; return; }
            source = interiorMesh.vertices;
            indices = interiorMesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
                totalVolume += Vector3.Dot(source[indices[i]], Vector3.Cross(source[indices[i + 1]], source[indices[i + 2]])) / 6f;
            totalVolume = Mathf.Abs(totalVolume);
            liquidMesh = new Mesh { name = "Bottle liquid (constant volume)" };
            liquidMesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = liquidMesh;
            UpdateLiquid(true);
        }

        void LateUpdate() => UpdateLiquid(false);

        void UpdateLiquid(bool force)
        {
            var normal = transform.InverseTransformDirection(Vector3.up).normalized;
            if (!force && Vector3.Dot(normal, lastNormal) > 0.99998f) return;
            lastNormal = normal;
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            foreach (var p in source) { var h = Vector3.Dot(normal, p); low = Mathf.Min(low, h); high = Mathf.Max(high, h); }
            for (int step = 0; step < 13; step++)
            {
                float h = (low + high) * 0.5f;
                if (Clip(normal, h, false) < totalVolume * fillFraction) low = h; else high = h;
            }
            Clip(normal, (low + high) * 0.5f, true);
            liquidMesh.Clear();
            liquidMesh.SetVertices(vertices);
            liquidMesh.SetTriangles(triangles, 0);
            liquidMesh.RecalculateNormals();
            liquidMesh.RecalculateBounds();
        }

        float Clip(Vector3 normal, float height, bool write)
        {
            vertices.Clear(); triangles.Clear(); rim.Clear();
            float volume = 0f;
            for (int i = 0; i < indices.Length; i += 3)
            {
                int count = 0;
                var previous = source[indices[i + 2]];
                float previousD = Vector3.Dot(normal, previous) - height;
                for (int j = 0; j < 3; j++)
                {
                    var current = source[indices[i + j]];
                    float currentD = Vector3.Dot(normal, current) - height;
                    if ((previousD <= 0f) != (currentD <= 0f))
                    {
                        var intersection = Vector3.LerpUnclamped(previous, current, previousD / (previousD - currentD));
                        polygon[count++] = intersection;
                        bool duplicate = false;
                        foreach (var p in rim) if ((p - intersection).sqrMagnitude < 1e-11f) { duplicate = true; break; }
                        if (!duplicate) rim.Add(intersection);
                    }
                    if (currentD <= 0f) polygon[count++] = current;
                    previous = current; previousD = currentD;
                }
                for (int j = 1; j + 1 < count; j++)
                    volume += Triangle(polygon[0], polygon[j], polygon[j + 1], write);
            }
            if (rim.Count >= 3)
            {
                var center = Vector3.zero;
                foreach (var p in rim) center += p;
                center /= rim.Count;
                var axisX = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                var axisY = Vector3.Cross(normal, axisX);
                // Insertion sort avoids allocating a comparison closure while the bottle moves.
                for (int i = 1; i < rim.Count; i++)
                {
                    var p = rim[i];
                    float angle = Angle(p - center, axisX, axisY);
                    int j = i - 1;
                    while (j >= 0 && Angle(rim[j] - center, axisX, axisY) > angle) { rim[j + 1] = rim[j]; j--; }
                    rim[j + 1] = p;
                }
                for (int i = 0; i < rim.Count; i++) volume += Triangle(center, rim[i], rim[(i + 1) % rim.Count], write);
            }
            return Mathf.Abs(volume);
        }

        static float Angle(Vector3 p, Vector3 x, Vector3 y) => Mathf.Atan2(Vector3.Dot(p, y), Vector3.Dot(p, x));
        float Triangle(Vector3 a, Vector3 b, Vector3 c, bool write)
        {
            if (write) { int i = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2); }
            return Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
        }

        void OnDestroy() { if (liquidMesh != null) Destroy(liquidMesh); }
    }
}
