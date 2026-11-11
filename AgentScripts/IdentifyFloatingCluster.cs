using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BreathInTheRubble.Prototype;
public static class IdentifyFloatingCluster
{
    public static object Run()
    {
        var main=Camera.main;var host=new GameObject("Temporary mesh identification"){hideFlags=HideFlags.HideAndDontSave};var camera=host.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.aspect=4f/3;camera.transform.position=main.transform.position;
        var rows=new List<object>();var filters=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null&&f.sharedMesh.isReadable&&f.GetComponent<MeshRenderer>()?.enabled==true&&f.GetComponent<AftershockFallingChunk>()==null).ToArray();
        try
        {
            foreach(float yaw in new[]{90f,270f})foreach(var point in new[]{new Vector2(.25f,.8f),new Vector2(.35f,.8f),new Vector2(.45f,.8f),new Vector2(.65f,.8f),new Vector2(.75f,.8f),new Vector2(.85f,.8f)})
            {
                camera.transform.rotation=Quaternion.Euler(-10,yaw,0);var ray=camera.ViewportPointToRay(point);float nearest=10;MeshFilter winner=null;
                foreach(var filter in filters)
                {
                    if(!filter.GetComponent<Renderer>().bounds.IntersectRay(ray))continue;var matrix=filter.transform.worldToLocalMatrix;var origin=matrix.MultiplyPoint3x4(ray.origin);var direction=matrix.MultiplyVector(ray.direction);var mesh=filter.sharedMesh;var v=mesh.vertices;var tris=mesh.triangles;
                    for(int i=0;i<tris.Length;i+=3){var a=v[tris[i]];var e1=v[tris[i+1]]-a;var e2=v[tris[i+2]]-a;var h=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-8f)continue;float inv=1/det;var s=origin-a;float u=Vector3.Dot(s,h)*inv;if(u<0||u>1)continue;var q=Vector3.Cross(s,e1);float w=Vector3.Dot(direction,q)*inv;if(w<0||u+w>1)continue;float t=Vector3.Dot(e2,q)*inv;if(t>0&&t<nearest){nearest=t;winner=filter;}}
                }
                rows.Add(new{yaw,point=point.ToString(),nearest,owner=winner?.name,parent=winner?.transform.parent?.name,grandparent=winner?.transform.parent?.parent?.name,mesh=winner?.sharedMesh.name});
            }
            File.WriteAllText("AgentScripts/ReviewImages/BurialGaps/ClusterIdentification.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
        }
        finally{UnityEngine.Object.DestroyImmediate(host);}
    }
}
