using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class RefineBuriedSurroundings
{
    public static string FinalTouch()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var root=GameObject.Find("Prototype Environment").transform.Find("Buried Surroundings");
        if(root.Find("Near Waist Compression Layer")!=null)return "Near-waist burial already present.";
        var go=new GameObject("Near Waist Compression Layer");go.transform.SetParent(root,false);go.transform.position=new Vector3(0f,0.417f,0.22f);
        go.transform.rotation=Quaternion.Euler(-6f,-8f,2f);go.transform.localScale=new Vector3(0.43f,0.11f,0.19f);go.isStatic=true;
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Art/ProceduralRubble/AngularConcrete_03.asset");
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.sharedMaterial.name.Contains("Concrete")).sharedMaterial;
        go.AddComponent<MeshCollider>().sharedMesh=mesh;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return "A thin compression layer now buries the distal thighs while leaving seated cloth visible near the waist.";
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var environment=GameObject.Find("Prototype Environment").transform;var root=environment.Find("Buried Surroundings");
        if(root.Find("Jagged Inner Fracture Layers")!=null)throw new Exception("Refinement already exists; refusing to apply twice.");
        var all=environment.GetComponentsInChildren<Transform>();
        void Place(string name,Vector3 pos,Vector3 scale)
        {
            var t=all.First(x=>x.name==name);t.position=pos;t.localScale=scale;
            var mesh=t.GetComponent<MeshFilter>();if(mesh!=null)
            {
                var box=t.GetComponent<BoxCollider>();if(box!=null)box.enabled=false;
                var collider=t.GetComponent<MeshCollider>();if(collider==null)collider=t.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh.sharedMesh;
            }
        }
        Place("Lower Body Rubble",new Vector3(-0.10f,0.30f,0.62f),new Vector3(0.70f,0.30f,0.54f));
        Place("Front Compression Slab",new Vector3(-0.09f,0.41f,0.62f),new Vector3(0.74f,0.14f,0.36f));
        Place("Left Hip Pinning Slab",new Vector3(-0.35f,0.32f,0.18f),new Vector3(0.27f,0.18f,0.38f));
        Place("Compressed Thigh Cover",new Vector3(-0.015f,0.355f,0.40f),new Vector3(0.50f,0.14f,0.28f));
        var pelvis=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Art/BuriedSurroundings/Seated Waist And Pelvis.asset");
        // Retune only the new authored pelvis, leaving all pre-existing project meshes intact.
        var v=pelvis.vertices;
        if(pelvis.bounds.max.y>0.5f)for(int i=0;i<v.Length;i++)
        {
            float t=Mathf.InverseLerp(0.255f,0.525f,v[i].y);
            float oldCenter=-0.055f-t*0.025f,newCenter=-0.035f-t*0.065f;
            float oldR=Mathf.Lerp(0.145f,0.095f,t),newR=Mathf.Lerp(0.13f,0.06f,t);
            v[i]=new Vector3(v[i].x*Mathf.Lerp(1f,0.14f/0.145f,t),Mathf.Lerp(0.255f,0.43f,t),newCenter+(v[i].z-oldCenter)*newR/oldR);
        }
        pelvis.vertices=v;pelvis.RecalculateNormals();pelvis.RecalculateBounds();EditorUtility.SetDirty(pelvis);
        var body=root.Find("Seated Buried Lower Body");body.Find("Seated Waist And Pelvis").GetComponent<MeshCollider>().sharedMesh=pelvis;
        var cloth=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/BuriedSurroundings/Dusty Charcoal Trousers.mat");cloth.SetColor("_BaseColor",new Color(0.12f,0.14f,0.145f));EditorUtility.SetDirty(cloth);
        var chunks=Enumerable.Range(1,6).Select(i=>AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i:00}.asset")).ToArray();
        var materials=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name.Contains("Concrete")).Select(r=>r.sharedMaterial).Distinct().ToArray();
        var contacts=new GameObject("Jagged Inner Fracture Layers").transform;contacts.SetParent(root,false);
        void Chunk(string name,Vector3 pos,Vector3 scale,Vector3 euler,int i)
        {
            var go=new GameObject(name);go.transform.SetParent(contacts,false);go.transform.SetPositionAndRotation(pos,Quaternion.Euler(euler));go.transform.localScale=scale;go.isStatic=true;
            go.AddComponent<MeshFilter>().sharedMesh=chunks[i%6];go.AddComponent<MeshRenderer>().sharedMaterial=materials[i%materials.Length];go.AddComponent<MeshCollider>().sharedMesh=chunks[i%6];
        }
        Chunk("Left Inner Broken Face",new Vector3(-0.47f,0.84f,0.025f),new Vector3(0.18f,0.28f,0.36f),new Vector3(24f,11f,38f),0);
        Chunk("Left Upper Fracture",new Vector3(-0.48f,1.06f,-0.21f),new Vector3(0.20f,0.24f,0.27f),new Vector3(-22f,18f,-19f),2);
        Chunk("Left Lower Fracture",new Vector3(-0.49f,0.57f,0.24f),new Vector3(0.24f,0.23f,0.31f),new Vector3(18f,-21f,12f),4);
        Chunk("Right Inner Broken Face",new Vector3(0.49f,0.98f,0.01f),new Vector3(0.19f,0.24f,0.34f),new Vector3(-19f,-14f,-28f),3);
        Chunk("Right Lower Front Fracture",new Vector3(0.55f,0.72f,0.48f),new Vector3(0.23f,0.25f,0.27f),new Vector3(27f,19f,-11f),5);
        Chunk("Right Rear Fracture",new Vector3(0.50f,0.54f,-0.33f),new Vector3(0.24f,0.27f,0.27f),new Vector3(-18f,-22f,17f),1);
        Chunk("Rear Inner Fracture Left",new Vector3(-0.25f,0.76f,-0.62f),new Vector3(0.25f,0.25f,0.16f),new Vector3(18f,-11f,29f),4);
        Chunk("Rear Inner Fracture Center",new Vector3(0.045f,0.66f,-0.59f),new Vector3(0.34f,0.22f,0.18f),new Vector3(-12f,19f,-16f),2);
        Chunk("Rear Inner Fracture Right",new Vector3(0.34f,0.91f,-0.60f),new Vector3(0.25f,0.30f,0.16f),new Vector3(21f,-14f,18f),0);
        Physics.SyncTransforms();
        foreach(var t in all.Where(t=>t.name.StartsWith("Settled Lap Fragment")||t.name.StartsWith("Settled Concrete Chip")))
        {
            if(!Physics.Raycast(new Vector3(t.position.x,0.72f,t.position.z),Vector3.down,out var hit,0.8f)||hit.normal.y<0.3f)continue;
            t.position=hit.point+hit.normal*t.localScale.y*0.22f;
        }
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Refined waist silhouette, uncovered proximal seated thighs, layered side fractures and resettled fragments.";
    }
}
