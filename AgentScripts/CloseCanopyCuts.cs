using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class CloseCanopyCuts
{
    struct PlaneFace {public Vector3 n;public float d;public PlaneFace(Vector3 nn,float dd){n=nn.normalized;d=dd/nn.magnitude;}public float Distance(Vector3 p)=>Vector3.Dot(n,p)+d;}
    static List<PlaneFace[]> Volumes()
    {
        float[,] s={{-.62f,.92f,.35f,.34f},{.62f,.94f,.35f,.34f},{-.90f,.10f,.30f,.35f},{.90f,.10f,.30f,.35f},{.25f,-.82f,.35f,.29f},{0,.88f,.29f,.30f}};var result=new List<PlaneFace[]>();
        for(int j=0;j<6;j++){var ps=new List<PlaneFace>{new PlaneFace(Vector3.up,-1.12f),new PlaneFace(Vector3.down,4.3f)};for(int i=0;i<8;i++){float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;var p=new Vector2(s[j,0]+Mathf.Cos(a)*s[j,2],s[j,1]+Mathf.Sin(a)*s[j,3]);var q=new Vector2(s[j,0]+Mathf.Cos(b)*s[j,2],s[j,1]+Mathf.Sin(b)*s[j,3]);var n=new Vector3(-(q.y-p.y),0,q.x-p.x);ps.Add(new PlaneFace(n,-n.x*p.x-n.z*p.y));}result.Add(ps.ToArray());}return result;
    }
    static string Key(Vector3 p)=>Mathf.RoundToInt(p.x*30000)+","+Mathf.RoundToInt(p.y*30000)+","+Mathf.RoundToInt(p.z*30000);
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c)=>Cross(b-a,p-a)>=-.000001f&&Cross(c-b,p-b)>=-.000001f&&Cross(a-c,p-c)>=-.000001f;
    static int[] Triangulate(List<Vector2> ps)
    {
        var order=Enumerable.Range(0,ps.Count).ToList();float area=0;for(int i=0;i<ps.Count;i++)area+=Cross(ps[i],ps[(i+1)%ps.Count]);if(area<0)order.Reverse();var tris=new List<int>();int guard=ps.Count*ps.Count;
        while(order.Count>2&&guard-->0){bool found=false;for(int j=0;j<order.Count;j++){int a=order[(j+order.Count-1)%order.Count],b=order[j],c=order[(j+1)%order.Count];if(Cross(ps[b]-ps[a],ps[c]-ps[b])<.0000001f)continue;if(order.Any(k=>k!=a&&k!=b&&k!=c&&Inside(ps[k],ps[a],ps[b],ps[c])))continue;tris.AddRange(new[]{a,b,c});order.RemoveAt(j);found=true;break;}if(!found)break;}return tris.ToArray();
    }
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");const string art="Assets/_Project/Art/ClosedCanopyCuts";if(AssetDatabase.FindAssets("t:Mesh",new[]{art}).Length>0)throw new Exception("Already applied");if(!AssetDatabase.IsValidFolder(art))AssetDatabase.CreateFolder("Assets/_Project/Art","ClosedCanopyCuts");var volumes=Volumes();var planes=volumes.SelectMany(v=>v).ToArray();int total=0,capTotal=0;var audit=new List<object>();
        foreach(var renderer in GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.GetComponent<MeshFilter>()?.sharedMesh?.name.StartsWith("CutCanopy_")==true).ToArray())
        {
            var filter=renderer.GetComponent<MeshFilter>();var original=filter.sharedMesh;var verts=original.vertices;var world=verts.Select(filter.transform.TransformPoint).ToArray();var unique=new List<Vector3>();var ids=new Dictionary<string,int>();var welded=new int[world.Length];for(int i=0;i<world.Length;i++){var key=Key(world[i]);if(!ids.TryGetValue(key,out int idx)){idx=unique.Count;ids[key]=idx;unique.Add(world[i]);}welded[i]=idx;}
            var edges=new Dictionary<(int,int),int>();for(int sub=0;sub<original.subMeshCount;sub++){var ts=original.GetTriangles(sub);for(int t=0;t<ts.Length;t+=3)for(int k=0;k<3;k++){int a=welded[ts[t+k]],b=welded[ts[t+(k+1)%3]];if(a==b)continue;var edge=a<b?(a,b):(b,a);edges[edge]=edges.TryGetValue(edge,out int count)?count+1:1;}}
            var boundaries=edges.Where(e=>e.Value==1).Select(e=>e.Key).ToArray();var vs=verts.ToList();var ns=original.normals.ToList();var uv=original.uv.ToList();while(ns.Count<vs.Count)ns.Add(Vector3.up);while(uv.Count<vs.Count)uv.Add(Vector2.zero);var caps=new List<int>();int loops=0;
            foreach(var volume in volumes)foreach(var face in volume)
            {
                var es=boundaries.Where(e=>Mathf.Abs(face.Distance(unique[e.Item1]))<.00015f&&Mathf.Abs(face.Distance(unique[e.Item2]))<.00015f&&volume.All(p=>p.Distance((unique[e.Item1]+unique[e.Item2])*.5f)>-.0002f)).ToList();if(es.Count==0)continue;
                var adjacency=new Dictionary<int,HashSet<int>>();Action<int,int> add=(a,b)=>{if(!adjacency.ContainsKey(a))adjacency[a]=new HashSet<int>();if(!adjacency.ContainsKey(b))adjacency[b]=new HashSet<int>();adjacency[a].Add(b);adjacency[b].Add(a);};foreach(var e in es)add(e.Item1,e.Item2);
                foreach(var border in planes.Where(p=>Vector3.Cross(face.n,p.n).sqrMagnitude>.001f))
                {
                    var ends=adjacency.Where(p=>p.Value.Count==1&&Mathf.Abs(border.Distance(unique[p.Key]))<.0002f).Select(p=>p.Key).OrderBy(i=>Vector3.Dot(unique[i],Vector3.Cross(face.n,border.n))).ToArray();for(int i=0;i+1<ends.Length;i+=2)add(ends[i],ends[i+1]);
                }
                var used=new HashSet<(int,int)>();var right=Vector3.Cross(face.n,Mathf.Abs(face.n.y)>.9f?Vector3.forward:Vector3.up).normalized;var up=Vector3.Cross(face.n,right).normalized;
                foreach(var start in adjacency.Keys.ToArray())foreach(var first in adjacency[start].ToArray())
                {
                    var firstEdge=start<first?(start,first):(first,start);if(used.Contains(firstEdge))continue;var loop=new List<int>{start};int prev=start,cur=first;used.Add(firstEdge);int guard=adjacency.Count+2;
                    while(cur!=start&&guard-->0){loop.Add(cur);var next=adjacency[cur].Where(v=>v!=prev&&!used.Contains(v<cur?(v,cur):(cur,v))).ToArray();if(next.Length==0)break;int n=next[0];used.Add(n<cur?(n,cur):(cur,n));prev=cur;cur=n;}
                    if(cur!=start||loop.Count<3)continue;var points=loop.Select(i=>new Vector2(Vector3.Dot(unique[i],right),Vector3.Dot(unique[i],up))).ToList();var triangles=Triangulate(points);if(triangles.Length==0)continue;int offset=vs.Count;foreach(var i in loop){vs.Add(filter.transform.InverseTransformPoint(unique[i]));ns.Add(filter.transform.localToWorldMatrix.transpose.MultiplyVector(face.n).normalized);uv.Add(new Vector2(Vector3.Dot(unique[i],right)*1.5f,Vector3.Dot(unique[i],up)*1.5f));}caps.AddRange(triangles.Select(i=>offset+i));loops++;
                }
            }
            if(caps.Count==0)continue;var mesh=UnityEngine.Object.Instantiate(original);mesh.indexFormat=IndexFormat.UInt32;mesh.name="Solid cut walls - "+renderer.name;mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,uv);int subIndex=mesh.subMeshCount;mesh.subMeshCount++;mesh.SetTriangles(caps,subIndex);mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,art+"/ClosedCut_"+(total++)+".asset");filter.sharedMesh=mesh;var ms=renderer.sharedMaterials.ToList();ms.Add(ms[0]);renderer.sharedMaterials=ms.ToArray();foreach(var c in renderer.GetComponents<MeshCollider>())if(c.enabled)c.sharedMesh=mesh;capTotal+=loops;audit.Add(new{renderer.name,loops,triangles=caps.Count/3});
        }
        File.WriteAllText("Temp/BuriedCellReview/ClosedCuts.json",Newtonsoft.Json.JsonConvert.SerializeObject(audit,Newtonsoft.Json.Formatting.Indented));EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{meshes=total,closedCutFaces=capTotal};
    }
}
