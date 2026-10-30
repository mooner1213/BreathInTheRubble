using System;
using System.Collections.Generic;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class BuildConfinedSpace
{
    const string Destination = "Assets/_Project/Scenes/ConfinedPrototype.unity";
    const string Art = "Assets/_Project/Art/ConfinedSpace";
    static Transform environment;
    static Mesh[] chunks;
    static Material[] concrete;
    static Material rust;

    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/Prototype.unity" || scene.isDirty)
            throw new InvalidOperationException("The original Prototype must be active and saved.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
            throw new InvalidOperationException("The confined variant already exists; refusing to replace it.");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/_Project/Art", "ConfinedSpace");
        // Preserve the original scene, prefab and materials as a comparison / rollback point.
        if (!EditorSceneManager.SaveScene(scene, Destination, true)) throw new InvalidOperationException("Cannot copy scene.");
        scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        environment = GameObject.Find("Prototype Environment").transform;
        chunks = Enumerable.Range(1, 6).Select(i => AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i:00}.asset")).ToArray();
        var concreteShader = Shader.Find("BreathInTheRubble/Weathered Concrete") ?? throw new InvalidOperationException("Concrete shader not imported.");
        var copies = new Dictionary<Material, Material>();
        foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>(true))
        {
            var source = renderer.sharedMaterial;
            if (source == null || !source.name.StartsWith("Concrete", StringComparison.Ordinal)) continue;
            if (!copies.TryGetValue(source, out var material))
            {
                material = new Material(concreteShader) { name = source.name + " Weathered", enableInstancing = true };
                var color = source.GetColor("_BaseColor");
                material.SetColor("_BaseColor", color * 1.30f);
                material.SetFloat("_GrainScale", 240f);
                AssetDatabase.CreateAsset(material, Art + "/" + material.name + ".mat");
                copies.Add(source, material);
            }
            renderer.sharedMaterial = material;
        }
        concrete = copies.Values.Take(4).ToArray();
        rust = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/RebarRust.mat");

        ConfigureEnclosure();
        BuildBuriedLegs();
        BuildContactDetails();
        ConfigureDust();
        BuildWaterBottle();
        // A static initial seated offset; no camera animation, movement provider or tracking override.
        var origin = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        var originData = new SerializedObject(origin);
        originData.FindProperty("m_CameraYOffset").floatValue = 0.90f;
        originData.ApplyModifiedProperties();
        origin.CameraFloorOffsetObject.transform.localPosition = new Vector3(0f, 0.90f, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save confined scene.");
        return "scene=" + scene.path + "|seatedOffset=0.90m|persistentDust=90motes+28veil|waterVolume=45%|originalAssetsPreserved=true";
    }

    static Transform Find(string name) => environment.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    static void Place(string name, Vector3 position, Vector3? scale = null, Vector3? rotation = null)
    {
        var t = Find(name); t.position = position;
        if (scale.HasValue) t.localScale = scale.Value;
        if (rotation.HasValue) t.rotation = Quaternion.Euler(rotation.Value);
    }

    static void ConfigureEnclosure()
    {
        Place("Left Wall", new Vector3(-0.66f, 0.69f, 0.03f));
        Place("Right Wall", new Vector3(0.69f, 0.70f, 0.02f));
        Place("Low Ceiling Rear", new Vector3(-0.04f, 1.31f, -0.28f));
        Place("Low Ceiling Front", new Vector3(0.09f, 1.23f, 0.63f));
        Place("Overhead Broken Slab Left", new Vector3(-0.46f, 1.26f, 0.32f));
        Place("Overhead Broken Slab Right", new Vector3(0.48f, 1.29f, 0.18f));
        Place("Broken Ceiling Lip", new Vector3(0.08f, 1.17f, 0.86f));
        Place("Ceiling Crevice", new Vector3(-0.17f, 1.34f, 0.46f));
        Place("Leaning Wall Plate", new Vector3(-0.55f, 0.70f, 0.45f), new Vector3(0.19f, 0.91f, 0.72f));
        Place("Left Shoulder Chunk", new Vector3(-0.59f, 0.82f, 0.30f), new Vector3(0.32f, 0.42f, 0.57f));
        Place("Crushed Cross Beam", new Vector3(0.62f, 0.93f, 0.40f), new Vector3(0.18f, 0.20f, 1.00f));
        Place("Front Rubble Seal", new Vector3(0f, -0.04f, -0.08f));
        foreach (var t in Find("Exposed Rebar").GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("Ceiling Rebar", StringComparison.Ordinal)) t.position += new Vector3(0f, -0.11f, 0f);
        Place("Left Wall Crack", new Vector3(-0.538f, 0.70f, -0.29f));
        Place("Right Wall Crack", new Vector3(0.567f, 0.94f, -0.18f));
        // Keep the phone's low-light discovery beat. Dust reacts to phone / flashlight illumination.
        RenderSettings.fogDensity = 0.075f;
    }

    static Transform Root(string name)
    {
        var root = new GameObject(name); root.transform.SetParent(environment, false);
        return root.transform;
    }
    static GameObject Chunk(Transform parent, string name, Vector3 position, Vector3 scale, Vector3 euler, int variant, bool collider = true)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler)); go.transform.localScale = scale; go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = chunks[variant % chunks.Length];
        go.AddComponent<MeshRenderer>().sharedMaterial = concrete[variant % concrete.Length];
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = chunks[variant % chunks.Length];
        return go;
    }

    static void BuildBuriedLegs()
    {
        Place("Lower Body Rubble", new Vector3(-0.10f, 0.32f, 0.47f), new Vector3(0.73f, 0.48f, 0.76f));
        Place("Front Compression Slab", new Vector3(-0.12f, 0.48f, 0.60f), new Vector3(0.83f, 0.17f, 0.45f), new Vector3(-6f, 8f, -7f));
        var root = Root("Buried Lower Body Rubble");
        Chunk(root, "Left Hip Pinning Slab", new Vector3(-0.34f, 0.38f, 0.12f), new Vector3(0.40f, 0.23f, 0.48f), new Vector3(12f, 24f, -19f), 1);
        Chunk(root, "Right Thigh Rubble", new Vector3(0.12f, 0.36f, 0.50f), new Vector3(0.30f, 0.22f, 0.34f), new Vector3(-12f, -18f, 8f), 3);
        Chunk(root, "Pinned Knee Slab", new Vector3(-0.12f, 0.38f, 0.64f), new Vector3(0.59f, 0.22f, 0.41f), new Vector3(11f, 14f, -8f), 4);
        var random = new System.Random(46121);
        for (int i = 0; i < 22; i++)
        {
            float x = -0.40f + (float)random.NextDouble() * 0.73f;
            float z = 0.25f + (float)random.NextDouble() * 0.42f;
            float size = 0.025f + (float)random.NextDouble() * 0.065f;
            Chunk(root, $"Settled Lap Fragment {i + 1:00}", new Vector3(x, 0.50f + (float)random.NextDouble() * 0.045f, z),
                new Vector3(size * 1.2f, size * 0.4f, size), new Vector3(0f, (float)random.NextDouble() * 180f, 8f), i, false);
        }
    }

    static void BuildContactDetails()
    {
        var root = Root("Settled Collapse Details");
        var random = new System.Random(7712);
        // Small fragments lie on the actual upward-facing rubble surfaces, rather than floating.
        Physics.SyncTransforms();
        for (int i = 0; i < 65; i++)
        {
            float x = -0.59f + (float)random.NextDouble() * 1.18f;
            float z = -0.59f + (float)random.NextDouble() * 1.30f;
            if (Mathf.Abs(x) < 0.27f && z < 0.22f) continue;
            if (x > 0.21f && z > -0.10f && z < 0.40f) continue; // phone and hand access
            if (x < -0.16f && z < -0.30f) continue; // bottle pocket
            if (!Physics.Raycast(new Vector3(x, 0.70f, z), Vector3.down, out var hit, 0.9f)) continue;
            if (!hit.transform.IsChildOf(environment) || hit.normal.y < 0.45f) continue;
            float size = 0.015f + (float)random.NextDouble() * 0.042f;
            var fragment = Chunk(root, $"Settled Concrete Chip {i:00}", hit.point + hit.normal * size * 0.15f,
                new Vector3(size, size * 0.32f, size * 0.8f), Vector3.zero, i, false);
            fragment.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, i * 137.5f, 0f);
        }
        // Embedded steel ends link the overhead collapse visually with the compressed lap slab.
        Rod(root, "Embedded Reinforcement Left", new Vector3(-0.33f, 0.53f, 0.65f), new Vector3(-0.21f, 0.53f, 0.43f), 0.009f, rust);
        Rod(root, "Bent Reinforcement End", new Vector3(-0.21f, 0.53f, 0.43f), new Vector3(-0.18f, 0.56f, 0.39f), 0.009f, rust);
    }

    static void ConfigureDust()
    {
        var effect = UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();
        foreach (var system in effect.GetComponentsInChildren<ParticleSystem>())
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; var emission = system.emission; var shape = system.shape;
            if (system.name == "Initial Dust Cloud")
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(24f, 40f);
                main.maxParticles = 300;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)240) });
                system.transform.localPosition = new Vector3(0f, 0.88f, 0.49f);
                continue;
            }
            main.loop = true; main.duration = 30f; main.prewarm = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.001f, 0.004f);
            bool motes = system.name == "Suspended Dust Motes";
            main.startLifetime = motes ? new ParticleSystem.MinMaxCurve(24f, 38f) : new ParticleSystem.MinMaxCurve(18f, 28f);
            main.maxParticles = motes ? 90 : 28;
            main.startSize = motes ? new ParticleSystem.MinMaxCurve(0.003f, 0.010f) : new ParticleSystem.MinMaxCurve(0.13f, 0.29f);
            emission.rateOverTime = motes ? 2.1f : 0.65f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(motes ? 60 : 16)) });
            system.transform.localPosition = new Vector3(0f, 0.90f, 0.39f);
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = motes ? new Vector3(1.03f, 0.45f, 1.00f) : new Vector3(0.90f, 0.40f, 0.64f);
            var noise = system.noise; noise.enabled = true; noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = motes ? 0.008f : 0.012f; noise.frequency = 0.18f; noise.scrollSpeed = 0.012f;
            var color = new Gradient();
            color.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.85f, 0.80f, 0.72f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(motes ? 0.62f : 0.30f, 0.15f),
                    new GradientAlphaKey(motes ? 0.48f : 0.20f, 0.75f), new GradientAlphaKey(0f, 1f) });
            var lifetime = system.colorOverLifetime; lifetime.enabled = true; lifetime.color = color;
        }
    }

    static Material Lit(string name, Color color, float smoothness, bool transparent = false)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
        if (transparent)
        {
            material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f); material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        AssetDatabase.CreateAsset(material, Art + "/" + name + ".mat"); return material;
    }

    static void BuildWaterBottle()
    {
        var shell = Lit("Dusty PET Bottle", new Color(0.55f, 0.66f, 0.67f, 0.14f), 0.74f, true);
        var water = Lit("Clear Water", new Color(0.22f, 0.43f, 0.46f, 0.62f), 0.88f, true);
        water.SetFloat("_Cull", (float)CullMode.Back); water.renderQueue = 2998;
        var cap = Lit("Bottle Cap Muted Blue", new Color(0.045f, 0.14f, 0.19f), 0.24f);
        var label = Lit("Weathered Bottle Label", new Color(0.57f, 0.58f, 0.52f), 0.04f);
        var seam = Lit("Bottle Label Ink", new Color(0.08f, 0.16f, 0.17f), 0.08f);
        var profile = new List<Vector2> { new(0.004f,0.025f), new(0.012f,0.033f) };
        for (int i = 0; i < 12; i++) { float y = 0.023f + i * 0.014f; profile.Add(new Vector2(y,0.034f)); profile.Add(new Vector2(y + 0.003f,0.0355f)); profile.Add(new Vector2(y + 0.006f,0.034f)); }
        profile.Add(new Vector2(0.193f,0.032f)); profile.Add(new Vector2(0.218f,0.014f)); profile.Add(new Vector2(0.244f,0.014f));
        var bodyMesh = Revolve("Ribbed PET Bottle", profile, 32);
        var interiorMesh = Revolve("Bottle Interior Volume", new[] { new Vector2(0.009f,0.025f),new Vector2(0.016f,0.032f),new Vector2(0.188f,0.032f),new Vector2(0.211f,0.012f),new Vector2(0.239f,0.012f) }, 24);
        var pocket = Root("Rear Bottle Pocket");
        var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube); ledge.name = "Bottle Resting Slab";
        ledge.transform.SetParent(pocket, false); ledge.transform.position = new Vector3(-0.29f,0.255f,-0.44f);
        ledge.transform.localScale = new Vector3(0.25f,0.09f,0.22f); ledge.transform.rotation = Quaternion.Euler(0f,14f,0f);
        ledge.GetComponent<MeshRenderer>().sharedMaterial = concrete[0]; ledge.isStatic = true;
        Chunk(pocket,"Rear Pocket Broken Edge",new Vector3(-0.43f,0.27f,-0.46f),new Vector3(0.15f,0.18f,0.26f),new Vector3(10f,22f,8f),3);

        var bottle = new GameObject("Water Bottle 45 Percent (Grabbable)");
        bottle.transform.position = new Vector3(-0.28f,0.302f,-0.43f); bottle.transform.rotation = Quaternion.Euler(0f,-18f,0f);
        var body = new GameObject("Ribbed Transparent Bottle"); body.transform.SetParent(bottle.transform,false);
        body.AddComponent<MeshFilter>().sharedMesh = bodyMesh; var renderer = body.AddComponent<MeshRenderer>(); renderer.sharedMaterial = shell; renderer.shadowCastingMode = ShadowCastingMode.Off;
        var liquid = new GameObject("Water Volume 45 Percent"); liquid.transform.SetParent(bottle.transform,false);
        liquid.AddComponent<MeshFilter>().sharedMesh = interiorMesh; liquid.AddComponent<MeshRenderer>().sharedMaterial = water;
        var visual = liquid.AddComponent<BottleLiquidVisual>();
        var serialized = new SerializedObject(visual); serialized.FindProperty("fillFraction").floatValue = 0.45f;
        serialized.FindProperty("interiorMesh").objectReferenceValue = interiorMesh; serialized.ApplyModifiedProperties();
        Rod(bottle.transform,"Screw Cap",new Vector3(0f,0.240f,0f),new Vector3(0f,0.260f,0f),0.034f,cap,false);
        var bandMesh = Revolve("Narrow Bottle Label",new[]{new Vector2(0.125f,0.035f),new Vector2(0.160f,0.035f)},32,false);
        var band = new GameObject("Worn Paper Label"); band.transform.SetParent(bottle.transform,false); band.AddComponent<MeshFilter>().sharedMesh = bandMesh; band.AddComponent<MeshRenderer>().sharedMaterial = label;
        var inkMesh = Revolve("Label Print Band",new[]{new Vector2(0.133f,0.0352f),new Vector2(0.138f,0.0352f)},32,false);
        var ink = new GameObject("Faded Label Stripe"); ink.transform.SetParent(bottle.transform,false); ink.AddComponent<MeshFilter>().sharedMesh = inkMesh; ink.AddComponent<MeshRenderer>().sharedMaterial = seam;
        var collider = bottle.AddComponent<CapsuleCollider>(); collider.center = new Vector3(0f,0.132f,0f); collider.height = 0.260f; collider.radius = 0.0355f;
        var rigidbody = bottle.AddComponent<Rigidbody>(); rigidbody.mass = 0.25f; rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        var grab = bottle.AddComponent<XRGrabInteractable>(); grab.useDynamicAttach = true; grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = true;
        PrefabUtility.SaveAsPrefabAsset(bottle,"Assets/_Project/Prefabs/WaterBottle45.prefab");
    }

    static Mesh Revolve(string name, IReadOnlyList<Vector2> profile, int sides, bool closed = true)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
        for (int ring = 0; ring < profile.Count; ring++)
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle)*profile[ring].y,profile[ring].x,Mathf.Sin(angle)*profile[ring].y)); uv.Add(new Vector2((float)i/sides,profile[ring].x*4f));
                if (ring == 0 || i == sides) continue;
                int a=(ring-1)*(sides+1)+i,b=ring*(sides+1)+i;
                triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);
            }
        if (closed)
        {
            int bottom=vertices.Count; vertices.Add(new Vector3(0f,profile[0].x,0f)); uv.Add(Vector2.zero);
            int top=vertices.Count; vertices.Add(new Vector3(0f,profile[profile.Count-1].x,0f)); uv.Add(Vector2.one);
            for (int i=0;i<sides;i++) { triangles.Add(bottom);triangles.Add(i);triangles.Add(i+1); int a=(profile.Count-1)*(sides+1)+i;triangles.Add(top);triangles.Add(a+1);triangles.Add(a); }
        }
        var mesh=new Mesh {name=name}; mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Art+"/"+name+".asset"); return mesh;
    }

    static void Rod(Transform parent,string name,Vector3 start,Vector3 end,float diameter,Material material,bool world=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
        if(world) { go.transform.position=(start+end)*0.5f;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,(end-start).normalized); }
        else { go.transform.localPosition=(start+end)*0.5f;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(end-start).normalized); }
        go.transform.localScale=new Vector3(diameter,(end-start).magnitude*0.5f,diameter);go.GetComponent<MeshRenderer>().sharedMaterial=material;
        // Decorative rods and the cap use the enclosing static / bottle collider.
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
}
