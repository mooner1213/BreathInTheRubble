using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BreathInTheRubble.Prototype;

public static class FitSeismicBearings
{
    public static object Compact()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var root=GameObject.Find("Prototype Environment");
        var pieces=manager.Elements.Where(e=>e.powder!=null).ToArray();var all=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
        int before=0,after=0;
        foreach(var e in pieces)
        {
            var list=e.target.GetComponents<BoxCollider>().ToList();before+=list.Count;
            for(int axis=0;axis<3;axis++)
            {
                bool changed=true;
                while(changed)
                {
                    changed=false;
                    for(int i=0;i<list.Count&&!changed;i++)for(int j=i+1;j<list.Count&&!changed;j++)
                    {
                        var a=list[i];var b=list[j];bool aligned=true;
                        for(int k=0;k<3;k++)if(k!=axis&&(Mathf.Abs(a.center[k]-b.center[k])>.00001f||Mathf.Abs(a.size[k]-b.size[k])>.00001f))aligned=false;
                        if(!aligned)continue;
                        float gap=Mathf.Abs(a.center[axis]-b.center[axis])-(a.size[axis]+b.size[axis])*.5f;
                        if(gap<0||gap>Mathf.Min(a.size[axis],b.size[axis])*.18f)continue;
                        var oldCenter=a.center;var oldSize=a.size;var min=Vector3.Min(a.center-a.size*.5f,b.center-b.size*.5f);var max=Vector3.Max(a.center+a.size*.5f,b.center+b.size*.5f);
                        a.center=(min+max)*.5f;a.size=max-min;Physics.SyncTransforms();bool blocked=false;
                        foreach(var other in all)if(other!=null&&other.attachedRigidbody!=a.attachedRigidbody&&a.bounds.Intersects(other.bounds))
                            if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,other,other.transform.position,other.transform.rotation,out var n,out float depth)&&depth>.0005f){blocked=true;break;}
                        if(blocked){a.center=oldCenter;a.size=oldSize;continue;}
                        UnityEngine.Object.DestroyImmediate(b);list.RemoveAt(j);changed=true;
                    }
                }
            }
            after+=list.Count;
        }
        EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
        var result=new{before,after};File.WriteAllText("AgentScripts/ReviewImages/SeismicCoupling/Compact.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
    }
    static bool Inside(Vector3 p,Vector3[] vertices,int[] tris)
    {
        var d=new Vector3(1,.37139f,.21717f).normalized;int hits=0;
        for(int i=0;i<tris.Length;i+=3)
        {
            var a=vertices[tris[i]];var edge1=vertices[tris[i+1]]-a;var edge2=vertices[tris[i+2]]-a;
            var h=Vector3.Cross(d,edge2);float det=Vector3.Dot(edge1,h);if(Mathf.Abs(det)<1e-8f)continue;
            float inv=1/det;var s=p-a;float u=Vector3.Dot(s,h)*inv;if(u<0||u>1)continue;
            var q=Vector3.Cross(s,edge1);float v=Vector3.Dot(d,q)*inv;if(v<0||u+v>1)continue;
            if(Vector3.Dot(edge2,q)*inv>1e-6f)hits++;
        }
        return hits%2==1;
    }
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var root=GameObject.Find("Prototype Environment");
        var pieces=manager.Elements.Where(e=>e.powder!=null).ToArray();
        foreach(var e in pieces){var mc=e.target.GetComponent<MeshCollider>();mc.sharedMesh=e.target.GetComponent<MeshFilter>().sharedMesh;mc.enabled=false;EditorUtility.SetDirty(mc);}
        Physics.SyncTransforms();var all=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToList();var report=new List<object>();
        foreach(var e in pieces)
        {
            if(e.target.GetComponents<BoxCollider>().Length>0)throw new Exception("Already fitted: "+e.target.name);
            var mesh=e.target.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var tris=mesh.triangles;var bounds=mesh.bounds;
            var scale=e.target.lossyScale;var size=Vector3.Scale(bounds.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
            int nx=Mathf.Clamp(Mathf.CeilToInt(size.x/.055f),3,9),ny=Mathf.Clamp(Mathf.CeilToInt(size.y/.055f),3,9),nz=Mathf.Clamp(Mathf.CeilToInt(size.z/.055f),3,9);
            var cell=new Vector3(bounds.size.x/nx,bounds.size.y/ny,bounds.size.z/nz);var boxes=new List<BoxCollider>();
            var probe=e.target.gameObject.AddComponent<BoxCollider>();probe.size=cell*.88f;probe.contactOffset=.001f;
            for(int x=0;x<nx;x++)for(int y=0;y<ny;y++)for(int z=0;z<nz;z++)
            {
                var center=bounds.min+Vector3.Scale(cell,new Vector3(x+.5f,y+.5f,z+.5f));
                if(!Inside(center,vertices,tris))continue;
                bool interior=true;
                foreach(var axis in new[]{Vector3.right,Vector3.up,Vector3.forward})
                    if(!Inside(center+Vector3.Scale(cell,axis)*.39f,vertices,tris)||!Inside(center-Vector3.Scale(cell,axis)*.39f,vertices,tris)){interior=false;break;}
                if(!interior)continue;
                probe.center=center;Physics.SyncTransforms();bool blocked=false;
                foreach(var other in all)
                {
                    if(other.attachedRigidbody==probe.attachedRigidbody||!probe.bounds.Intersects(other.bounds))continue;
                    if(Physics.ComputePenetration(probe,probe.transform.position,probe.transform.rotation,other,other.transform.position,other.transform.rotation,out var n,out float depth)&&depth>.0005f){blocked=true;break;}
                }
                if(blocked)continue;
                var b=e.target.gameObject.AddComponent<BoxCollider>();b.center=center;b.size=probe.size;b.contactOffset=.001f;boxes.Add(b);
            }
            UnityEngine.Object.DestroyImmediate(probe);
            if(boxes.Count==0){e.failureResistance=0;e.target.GetComponent<MeshCollider>().enabled=true;all.Add(e.target.GetComponent<MeshCollider>());}
            else all.AddRange(boxes);
            report.Add(new{e.target.name,boxes=boxes.Count,willFall=boxes.Count>0,visualSize=size.ToString("F3")});
            EditorUtility.SetDirty(e.target.gameObject);
        }
        EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
        File.WriteAllText("AgentScripts/ReviewImages/SeismicCoupling/Contacts.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
