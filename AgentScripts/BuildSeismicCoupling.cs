using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildSeismicCoupling
{
    const string Folder="Assets/_Project/Art/SeismicContacts";
    static void Set(UnityEngine.Object o,string field,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
    static void Number(UnityEngine.Object o,string field,float value){var s=new SerializedObject(o);s.FindProperty(field).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var root=GameObject.Find("Prototype Environment");var ground=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        Set(ground,"environmentRoot",root.transform);Number(ground,"roomShakeDistance",.020f);Number(ground,"roomShakeDegrees",.12f);Number(ground,"excitationScale",1);
        Set(manager,"ground",ground);
        foreach(var e in manager.Elements)if(e.loose!=null&&e.releaseAt>0)
            e.failureResistance=Mathf.Lerp(.58f,1.5f,Mathf.InverseLerp(5.15f,9.45f,e.releaseAt));
        EditorUtility.SetDirty(manager);
        int foundations=0;
        foreach(var c in root.GetComponentsInChildren<Collider>())
        {
            if(!c.enabled||c.isTrigger||c.attachedRigidbody!=null||c.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>()!=null)continue;
            if(c.GetComponentsInParent<Transform>().Any(t=>t.name=="Tracked rubble contact"))continue;
            var b=c.gameObject.AddComponent<Rigidbody>();b.isKinematic=true;b.useGravity=false;b.interpolation=RigidbodyInterpolation.Interpolate;b.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;foundations++;
        }
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art","SeismicContacts");
        var all=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
        var pieces=manager.Elements.Where(e=>e.powder!=null).ToArray();var fitted=new List<object>();int unresolved=0;
        for(int i=0;i<pieces.Length;i++)
        {
            var e=pieces[i];var collider=e.target.GetComponent<MeshCollider>();if(collider==null)continue;
            string path=Folder+"/Bearing_"+i.ToString("D2")+".asset";
            if(AssetDatabase.LoadAssetAtPath<Mesh>(path)!=null)throw new Exception("Refusing to replace existing collision asset: "+path);
            var source=e.target.GetComponent<MeshFilter>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(source);mesh.name=source.name+" interior contact hull";
            var original=source.vertices;var center=source.bounds.center;float scale=1,depth=0;string obstacle="";
            for(int step=0;step<23;step++)
            {
                scale=1-step*.04f;mesh.vertices=original.Select(v=>center+(v-center)*scale).ToArray();mesh.RecalculateBounds();collider.sharedMesh=null;collider.sharedMesh=mesh;
                Physics.SyncTransforms();depth=0;
                foreach(var other in all)
                {
                    if(other==collider||other.attachedRigidbody==collider.attachedRigidbody||!collider.bounds.Intersects(other.bounds))continue;
                    if(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,other,other.transform.position,other.transform.rotation,out var normal,out float d)&&d>depth){depth=d;obstacle=other.name;}
                }
                if(depth<=.0015f)break;
            }
            if(depth>.0015f)unresolved++;
            AssetDatabase.CreateAsset(mesh,path);collider.contactOffset=.001f;EditorUtility.SetDirty(collider);
            var body=e.target.GetComponent<Rigidbody>();body.maxDepenetrationVelocity=.18f;body.maxAngularVelocity=4;EditorUtility.SetDirty(body);
            fitted.Add(new{e.target.name,scale,depth,obstacle});
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
        var result=new{foundations,fitted,unresolved,failures=manager.Elements.Count(e=>e.failureResistance>0)};
        Directory.CreateDirectory("AgentScripts/ReviewImages/SeismicCoupling");File.WriteAllText("AgentScripts/ReviewImages/SeismicCoupling/Build.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
