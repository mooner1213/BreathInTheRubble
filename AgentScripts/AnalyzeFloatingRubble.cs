using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BreathInTheRubble.Prototype;
public static class AnalyzeFloatingRubble
{
    static string PathOf(Transform t)=>t.parent!=null?PathOf(t.parent)+"/"+t.name:t.name;
    public static List<int[]> Components(Mesh mesh)
    {
        var v=mesh.vertices;var parent=Enumerable.Range(0,v.Length).ToArray();var weld=new Dictionary<Vector3Int,int>();
        int Find(int a){while(parent[a]!=a){parent[a]=parent[parent[a]];a=parent[a];}return a;}
        void Join(int a,int b){a=Find(a);b=Find(b);if(a!=b)parent[a]=b;}
        for(int i=0;i<v.Length;i++){var key=Vector3Int.RoundToInt(v[i]*100000);if(weld.TryGetValue(key,out int j))Join(i,j);else weld[key]=i;}
        var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){Join(t[i],t[i+1]);Join(t[i],t[i+2]);}
        return Enumerable.Range(0,v.Length).GroupBy(Find).Select(g=>g.ToArray()).Where(g=>g.Length>=3).ToList();
    }
    public static object Run()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var head=Camera.main.transform.position;var rows=new List<object>();
        foreach(var e in manager.Elements.Where(e=>e.loose==null))
        {
            var r=e.target.GetComponent<MeshRenderer>();var filter=e.target.GetComponent<MeshFilter>();if(r==null||!r.enabled||filter?.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
            var bounds=r.bounds;if(bounds.max.y<head.y+.15f||Vector3.ProjectOnPlane(bounds.ClosestPoint(head)-head,Vector3.up).magnitude>1.2f)continue;
            var groups=Components(filter.sharedMesh);if(groups.Count<2)continue;var vertices=filter.sharedMesh.vertices;var islands=groups.Select(g=>{var b=new Bounds(e.target.TransformPoint(vertices[g[0]]),Vector3.zero);foreach(int j in g)b.Encapsulate(e.target.TransformPoint(vertices[j]));return new{vertices=g.Length,x=b.center.x,y=b.center.y,z=b.center.z,size=b.size.magnitude};}).Where(b=>b.size>.05f&&b.size<.65f&&b.y>head.y+.1f&&Mathf.Sqrt(b.x*b.x+b.z*b.z)<1.25f).ToArray();
            if(islands.Length>0)rows.Add(new{path=PathOf(e.target),mesh=filter.sharedMesh.name,groups=groups.Count,islands});
        }
        File.WriteAllText("AgentScripts/ReviewImages/BurialGaps/Islands.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
    }
}
