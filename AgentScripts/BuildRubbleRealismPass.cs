using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildRubbleRealismPass
{
    const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
    const string MeshFolder = "Assets/_Project/Art/ProceduralRubble";

    static readonly Vector3[] DetailPositions =
    {
        new(-0.72f, 0.10f, 0.52f), new(-0.63f, 0.08f, 0.17f),
        new(-0.56f, 0.11f, -0.46f), new(-0.34f, 0.07f, -0.68f),
        new(0.34f, 0.08f, -0.69f), new(0.60f, 0.10f, -0.49f),
        new(0.71f, 0.08f, 0.14f), new(0.70f, 0.11f, 0.51f),
        new(-0.50f, 0.39f, 0.68f), new(0.57f, 0.42f, 0.66f),
        new(-0.65f, 1.22f, -0.35f), new(0.63f, 1.21f, -0.43f)
    };

    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Expected {ScenePath}, but active scene is {scene.path}.");
        if (scene.isDirty)
            throw new InvalidOperationException("Prototype scene has unsaved changes; refusing to overwrite them.");

        EnsureFolder("Assets/_Project/Art", "ProceduralRubble");
        var meshes = Enumerable.Range(0, 6).Select(LoadOrCreateMesh).ToArray();

        var environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Prototype Environment")
            ?? throw new InvalidOperationException("Prototype Environment root was not found.");
        var filters = environment.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.sharedMesh != null && filter.sharedMesh.name == "Cube")
            .Where(filter => IsRubbleTarget(GetPath(filter.transform)))
            .Where(filter => filter.name != "Phone Ledge")
            .OrderBy(filter => GetPath(filter.transform))
            .ToArray();

        for (var index = 0; index < filters.Length; index++)
        {
            Undo.RecordObject(filters[index], "Replace blockout rubble mesh");
            filters[index].sharedMesh = meshes[index % meshes.Length];
            EditorUtility.SetDirty(filters[index]);
        }

        var detailCount = CreateDetailFragments(environment.transform, meshes);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scene.path))
            throw new InvalidOperationException($"Could not save {scene.path}.");
        AssetDatabase.SaveAssets();

        return $"replaced={filters.Length}|detailFragments={detailCount}|meshVariants={meshes.Length}";
    }

    static Mesh LoadOrCreateMesh(int index)
    {
        var path = $"{MeshFolder}/AngularConcrete_{index + 1:00}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;

        var mesh = CreateAngularChunk(3107 + index * 97, 6 + index % 3);
        mesh.name = $"AngularConcrete_{index + 1:00}";
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static Mesh CreateAngularChunk(int seed, int sideCount)
    {
        var previousState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);
        try
        {
            var top = new Vector3[sideCount];
            var bottom = new Vector3[sideCount];
            var topOffset = new Vector2(UnityEngine.Random.Range(-0.10f, 0.10f), UnityEngine.Random.Range(-0.10f, 0.10f));
            var bottomOffset = new Vector2(UnityEngine.Random.Range(-0.06f, 0.06f), UnityEngine.Random.Range(-0.06f, 0.06f));

            for (var index = 0; index < sideCount; index++)
            {
                var angle = index * Mathf.PI * 2f / sideCount + UnityEngine.Random.Range(-0.16f, 0.16f);
                var topRadius = UnityEngine.Random.Range(0.40f, 0.62f);
                var bottomRadius = UnityEngine.Random.Range(0.42f, 0.64f);
                var xDirection = Mathf.Cos(angle);
                var zDirection = Mathf.Sin(angle);
                top[index] = new Vector3(
                    xDirection * topRadius + topOffset.x,
                    0.5f + UnityEngine.Random.Range(-0.10f, 0.09f),
                    zDirection * topRadius + topOffset.y);
                bottom[index] = new Vector3(
                    xDirection * bottomRadius + bottomOffset.x,
                    -0.5f + UnityEngine.Random.Range(-0.08f, 0.10f),
                    zDirection * bottomRadius + bottomOffset.y);
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var topCenter = top.Aggregate(Vector3.zero, (sum, value) => sum + value) / sideCount;
            var bottomCenter = bottom.Aggregate(Vector3.zero, (sum, value) => sum + value) / sideCount;

            for (var index = 0; index < sideCount; index++)
            {
                var next = (index + 1) % sideCount;
                AddTriangle(vertices, triangles, uvs, topCenter, top[next], top[index]);
                AddTriangle(vertices, triangles, uvs, bottomCenter, bottom[index], bottom[next]);
                AddQuad(vertices, triangles, uvs, bottom[index], top[index], top[next], bottom[next]);
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
        finally
        {
            UnityEngine.Random.state = previousState;
        }
    }

    static int CreateDetailFragments(Transform environment, IReadOnlyList<Mesh> meshes)
    {
        var realismPass = environment.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == "Earthquake Realism Pass")
            ?? environment;
        var existing = realismPass.Find("Procedural Rubble Detail");
        if (existing != null)
            return existing.childCount;

        var materialPaths = new[]
        {
            "Assets/_Project/Materials/ConcreteFractured.mat",
            "Assets/_Project/Materials/ConcreteExposedCore.mat",
            "Assets/_Project/Materials/ConcreteFreshEdge.mat",
            "Assets/_Project/Materials/ConcreteDust.mat"
        };
        var materials = materialPaths.Select(AssetDatabase.LoadAssetAtPath<Material>).Where(item => item != null).ToArray();
        if (materials.Length == 0)
            throw new InvalidOperationException("Concrete materials were not found.");

        var detailRoot = new GameObject("Procedural Rubble Detail");
        Undo.RegisterCreatedObjectUndo(detailRoot, "Create rubble detail root");
        detailRoot.transform.SetParent(realismPass, false);

        for (var index = 0; index < DetailPositions.Length; index++)
        {
            var fragment = new GameObject($"Angular Fragment {index + 1:00}");
            Undo.RegisterCreatedObjectUndo(fragment, "Create rubble detail fragment");
            fragment.transform.SetParent(detailRoot.transform, false);
            fragment.transform.localPosition = DetailPositions[index];
            fragment.transform.localRotation = Quaternion.Euler(
                17f + index * 31f % 140f,
                23f + index * 47f % 180f,
                11f + index * 19f % 120f);
            var size = 0.055f + index % 4 * 0.021f;
            fragment.transform.localScale = new Vector3(size * 1.3f, size * 0.75f, size);
            fragment.isStatic = true;

            var filter = fragment.AddComponent<MeshFilter>();
            filter.sharedMesh = meshes[(index + 2) % meshes.Count];
            var renderer = fragment.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials[index % materials.Length];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return DetailPositions.Length;
    }

    static bool IsRubbleTarget(string path)
    {
        return path.Contains("/Rubble/") ||
               path.Contains("/Dense Rubble Piles/") ||
               path.Contains("/Collapsed Slabs/");
    }

    static string GetPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    static void EnsureFolder(string parent, string child)
    {
        var fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }

    static void AddTriangle(
        ICollection<Vector3> vertices,
        ICollection<int> triangles,
        ICollection<Vector2> uvs,
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        var start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        uvs.Add(new Vector2(a.x + 0.5f, a.z + 0.5f));
        uvs.Add(new Vector2(b.x + 0.5f, b.z + 0.5f));
        uvs.Add(new Vector2(c.x + 0.5f, c.z + 0.5f));
    }

    static void AddQuad(
        ICollection<Vector3> vertices,
        ICollection<int> triangles,
        ICollection<Vector2> uvs,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        var start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(1f, 0f));
    }
}
