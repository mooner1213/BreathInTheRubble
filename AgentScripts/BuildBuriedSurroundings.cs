using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildBuriedSurroundings
{
    const string Art="Assets/_Project/Art/BuriedSurroundings";
    static Transform root;
    static Mesh[] chunks;
    static Material[] concrete;
    static Material cloth, seam, rust;

    public static string Run()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype must be open in Edit mode.");
        var environment=GameObject.Find("Prototype Environment").transform;
        if(environment.Find("Buried Surroundings")!=null)throw new Exception("Surroundings already exist; refusing to duplicate them.");
        var backup="Assets/_Project/Scenes/Backups/BeforeBuriedSurroundings_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot preserve current scene.");
        if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/_Project/Art","BuriedSurroundings");
        chunks=Enumerable.Range(1,6).Select(i=>AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i:00}.asset")).ToArray();
        concrete=new[]{"ConcreteFractured Weathered","ConcreteExposedCore Weathered","ConcreteFreshEdge Weathered","ConcreteDust Weathered"}
            .Select(n=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ConfinedSpace/"+n+".mat")).Where(m=>m!=null).ToArray();
        rust=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/RebarRust.mat");
        cloth=NewMaterial("Dusty Charcoal Trousers",new Color(0.075f,0.09f,0.10f),0.04f);
        seam=NewMaterial("Trousers Stitched Seams",new Color(0.13f,0.14f,0.135f),0.02f);
        root=new GameObject("Buried Surroundings").transform;root.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Build buried surroundings");

        BuildSide(-1);BuildSide(1);BuildRear();BuildSeatedBody();BuildLap();SettleFragments();
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return $"surroundings={root.GetComponentsInChildren<MeshRenderer>().Length} renderers|seated lower-body visual and lap burial added|tracking unchanged|backup={backup}";
    }

    static Material NewMaterial(string name,Color color,float smoothness)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smoothness);AssetDatabase.CreateAsset(material,Art+"/"+name+".mat");return material;
    }
    static Transform Group(string name){var go=new GameObject(name);go.transform.SetParent(root,false);return go.transform;}
    static GameObject Chunk(Transform parent,string name,Vector3 position,Vector3 scale,Vector3 rotation,int variant,bool collider=true)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(position,Quaternion.Euler(rotation));go.transform.localScale=scale;go.isStatic=true;
        go.AddComponent<MeshFilter>().sharedMesh=chunks[variant%6];var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=concrete[variant%concrete.Length];renderer.shadowCastingMode=ShadowCastingMode.On;
        if(collider)go.AddComponent<MeshCollider>().sharedMesh=chunks[variant%6];return go;
    }
    static void BuildSide(int sign)
    {
        var group=Group(sign<0?"Left Collapsed Wall Layers":"Right Collapsed Wall Layers");
        Chunk(group,"Tilted Rear Wall Fragment",new Vector3(sign*0.61f,0.83f,-0.43f),new Vector3(0.17f,0.77f,0.56f),new Vector3(13f,sign*8f,sign*-10f),sign<0?1:3);
        Chunk(group,"Overlapping Mid Wall Plate",new Vector3(sign*0.60f,0.99f,0.02f),new Vector3(0.16f,0.58f,0.65f),new Vector3(-16f,sign*-9f,sign*9f),sign<0?4:0);
        Chunk(group,"Broken Front Side Panel",new Vector3(sign*0.66f,0.80f,0.55f),new Vector3(0.24f,0.66f,0.51f),new Vector3(8f,sign*17f,sign*-15f),sign<0?2:5);
        Chunk(group,"Rear Hip Rubble Bank",new Vector3(sign*0.52f,0.25f,sign<0?-0.10f:-0.21f),sign<0?new Vector3(0.22f,0.30f,0.28f):new Vector3(0.29f,0.35f,0.43f),new Vector3(12f,sign*26f,-11f),2);
        Chunk(group,"Lower Side Slab",new Vector3(sign*0.57f,0.37f,0.55f),new Vector3(0.34f,0.36f,0.56f),new Vector3(-9f,sign*18f,14f),4);
        Chunk(group,"Upper Rear Wedged Chunk",new Vector3(sign*0.52f,1.18f,-0.49f),new Vector3(0.31f,0.22f,0.35f),new Vector3(12f,sign*-19f,-8f),3);
        // Broken steel ends protrude from the plates, outside the central reach volume.
        Rod(group,"Exposed Side Rebar",new Vector3(sign*0.45f,0.97f,-0.46f),new Vector3(sign*0.43f,1.15f,-0.19f),0.009f,rust);
        Rod(group,"Bent Side Rebar Tip",new Vector3(sign*0.43f,1.15f,-0.19f),new Vector3(sign*0.48f,1.19f,-0.08f),0.009f,rust);
    }
    static void BuildRear()
    {
        var group=Group("Rear Collapsed Wall Layers");
        Chunk(group,"Rear Diagonal Reinforced Slab",new Vector3(0.13f,0.94f,-0.70f),new Vector3(0.80f,0.56f,0.18f),new Vector3(8f,-7f,17f),1);
        Chunk(group,"Rear Left Fractured Panel",new Vector3(-0.43f,0.84f,-0.72f),new Vector3(0.49f,0.59f,0.14f),new Vector3(-6f,9f,-19f),4);
        Chunk(group,"Rear Right Stacked Slab",new Vector3(0.42f,0.49f,-0.67f),new Vector3(0.46f,0.48f,0.25f),new Vector3(13f,-14f,11f),2);
        Chunk(group,"Rear Low Rubble Bank",new Vector3(0.14f,0.26f,-0.70f),new Vector3(0.40f,0.30f,0.22f),new Vector3(-8f,11f,-6f),3);
        Chunk(group,"Rear Ceiling Broken Corner",new Vector3(0.02f,1.20f,-0.62f),new Vector3(0.66f,0.20f,0.35f),new Vector3(7f,-18f,9f),5);
        // Preserve the bottle pocket at (-0.28, 0.30, -0.43).
        Rod(group,"Rear Slab Reinforcement",new Vector3(-0.20f,0.79f,-0.54f),new Vector3(0.11f,0.87f,-0.53f),0.011f,rust);
        Rod(group,"Rear Broken Steel End",new Vector3(0.11f,0.87f,-0.53f),new Vector3(0.20f,0.94f,-0.56f),0.011f,rust);
    }
    static void BuildSeatedBody()
    {
        var body=Group("Seated Buried Lower Body");
        // Authored seated silhouette; no head/hand tracking is driven or animated here.
        CreateBodyMesh(body,"Seated Waist And Pelvis",CreatePelvis(),cloth);
        CreateBodyMesh(body,"Left Seated Thigh",CreateThigh(-1),cloth);
        CreateBodyMesh(body,"Right Seated Thigh",CreateThigh(1),cloth);
        Rod(body,"Left Visible Trouser Seam",new Vector3(-0.18f,0.390f,0.035f),new Vector3(-0.20f,0.405f,0.185f),0.003f,seam);
        Rod(body,"Right Visible Trouser Seam",new Vector3(0.18f,0.390f,0.035f),new Vector3(0.20f,0.405f,0.185f),0.003f,seam);
    }
    static void CreateBodyMesh(Transform parent,string name,Mesh mesh,Material material)
    {
        mesh.name=name;AssetDatabase.CreateAsset(mesh,Art+"/"+name+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static Mesh CreatePelvis()
    {
        return RingMesh(9,32,(ring,angle)=>
        {
            float t=ring/8f;float xRadius=Mathf.Lerp(0.18f,0.14f,t),zRadius=Mathf.Lerp(0.13f,0.06f,t);
            float fold=1f+0.018f*Mathf.Sin(angle*7f+t*19f);
            return new Vector3(Mathf.Cos(angle)*xRadius*fold,Mathf.Lerp(0.255f,0.43f,t),-0.035f-t*0.065f+Mathf.Sin(angle)*zRadius*fold);
        });
    }
    static Mesh CreateThigh(int sign)
    {
        return RingMesh(14,28,(ring,angle)=>
        {
            float t=ring/13f;float xRadius=Mathf.Lerp(0.104f,0.076f,t),yRadius=Mathf.Lerp(0.089f,0.078f,t);
            float fold=1f+0.025f*Mathf.Sin(t*36f+angle*3f)+0.012f*Mathf.Sin(t*67f-angle*2f);
            return new Vector3(sign*Mathf.Lerp(0.105f,0.145f,t)+Mathf.Cos(angle)*xRadius*fold,
                0.335f+0.015f*Mathf.Sin(t*Mathf.PI)+Mathf.Sin(angle)*yRadius*fold,Mathf.Lerp(-0.015f,0.48f,t));
        },true);
    }
    static Mesh RingMesh(int rings,int sides,Func<int,float,Vector3> point,bool alongZ=false)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        for(int ring=0;ring<rings;ring++)for(int side=0;side<=sides;side++)
        {
            vertices.Add(point(ring,side*Mathf.PI*2f/sides));uv.Add(new Vector2(side/(float)sides,ring/(float)(rings-1)));
            if(ring==0||side==sides)continue;int a=(ring-1)*(sides+1)+side,b=ring*(sides+1)+side;
            if(alongZ){triangles.Add(a);triangles.Add(a+1);triangles.Add(b);triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);}
            else{triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);}
        }
        // Cap the waistband/hips and knee ends so downward and oblique views stay solid.
        for(int end=0;end<2;end++)
        {
            int ring=end==0?0:rings-1;var center=Vector3.zero;for(int i=0;i<sides;i++)center+=vertices[ring*(sides+1)+i];center/=sides;
            int c=vertices.Count;vertices.Add(center);uv.Add(new Vector2(0.5f,0.5f));
            for(int i=0;i<sides;i++){int a=ring*(sides+1)+i;bool reverse=alongZ?end==0:end==1;triangles.Add(c);triangles.Add(reverse?a+1:a);triangles.Add(reverse?a:a+1);}
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void BuildLap()
    {
        var group=Group("Close Waist And Knee Burial");
        Chunk(group,"Left Hip Embedded Concrete",new Vector3(-0.285f,0.34f,-0.035f),new Vector3(0.25f,0.23f,0.35f),new Vector3(8f,19f,-12f),0);
        Chunk(group,"Right Hip Embedded Concrete",new Vector3(0.285f,0.33f,-0.07f),new Vector3(0.22f,0.23f,0.31f),new Vector3(-9f,-17f,10f),3);
        Chunk(group,"Compressed Thigh Cover",new Vector3(-0.015f,0.405f,0.34f),new Vector3(0.56f,0.17f,0.33f),new Vector3(-5f,9f,-5f),2);
        Chunk(group,"Fully Buried Knees",new Vector3(0.005f,0.31f,0.56f),new Vector3(0.65f,0.37f,0.46f),new Vector3(6f,-11f,4f),4);
        Chunk(group,"Left Waist Rubble Contact",new Vector3(-0.225f,0.40f,0.11f),new Vector3(0.15f,0.12f,0.21f),new Vector3(-9f,22f,-14f),5);
        Chunk(group,"Right Waist Rubble Contact",new Vector3(0.225f,0.38f,0.10f),new Vector3(0.13f,0.14f,0.19f),new Vector3(9f,-14f,11f),1);
        // Bring small rubble up to the waist, closing the visible bare-floor gap below the camera.
        Chunk(group,"Near Left Hip Packing",new Vector3(-0.21f,0.22f,-0.18f),new Vector3(0.21f,0.29f,0.25f),new Vector3(10f,11f,9f),2);
        Chunk(group,"Near Right Hip Packing",new Vector3(0.21f,0.22f,-0.18f),new Vector3(0.21f,0.29f,0.25f),new Vector3(-10f,-11f,-9f),3);
    }
    static void SettleFragments()
    {
        var group=Group("Local Rubble Contacts");var random=new System.Random(51204);Physics.SyncTransforms();
        for(int i=0;i<52;i++)
        {
            float angle=(float)random.NextDouble()*Mathf.PI*2f;float radius=0.32f+(float)random.NextDouble()*0.28f;
            float x=Mathf.Sin(angle)*radius,z=Mathf.Cos(angle)*radius;
            if(x>0.25f&&z>-0.08f&&z<0.34f)continue;
            if(x< -0.16f&&x> -0.4f&&z< -0.30f)continue;
            if(!Physics.Raycast(new Vector3(x,0.72f,z),Vector3.down,out var hit,0.75f)||hit.normal.y<0.5f||hit.transform.root.name!="Prototype Environment")continue;
            float size=0.025f+(float)random.NextDouble()*0.05f;
            var chunk=Chunk(group,$"Settled Contact Fragment {i:00}",hit.point+hit.normal*size*0.15f,new Vector3(size,size*0.4f,size*0.8f),Vector3.zero,i,false);
            chunk.transform.rotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0f,i*137.5f,0f);
        }
    }
    static void Rod(Transform parent,string name,Vector3 start,Vector3 end,float diameter,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);go.transform.position=(start+end)*0.5f;
        go.transform.rotation=Quaternion.FromToRotation(Vector3.up,(end-start).normalized);go.transform.localScale=new Vector3(diameter,(end-start).magnitude*0.5f,diameter);go.GetComponent<MeshRenderer>().sharedMaterial=material;go.isStatic=true;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
}
