using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class InspectBurialGaps
{
    public static string PathOf(Transform t)=>t.parent!=null?PathOf(t.parent)+"/"+t.name:t.name;
    public static object Run()
    {
        var root=GameObject.Find("Prototype Environment");var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var camera=Camera.main;
        var elements=manager.Elements.ToDictionary(e=>e.target,e=>e);var fragments=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>().Fragments;
        var rows=new List<object>();
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled))
        {
            var body=r.GetComponentInParent<Rigidbody>();var p=camera.WorldToViewportPoint(r.bounds.center);RubbleStructuralCollapse.Element entry=null;
            for(var t=r.transform;t!=null;t=t.parent)if(elements.TryGetValue(t,out entry))break;
            rows.Add(new{path=PathOf(r.transform),mesh=r.GetComponent<MeshFilter>()?.sharedMesh?.name,position=r.bounds.center.ToString("F3"),size=r.bounds.size.ToString("F3"),body=body!=null?PathOf(body.transform):null,kinematic=body!=null?(bool?)body.isKinematic:null,gravity=body!=null?(bool?)body.useGravity:null,mapped=entry!=null,loose=entry?.loose!=null,resistance=entry?.failureResistance,release=entry?.loose?.Released,visible=p.z>0&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1,screen=p.ToString("F2"),fragment=fragments.Contains(r.transform)});
        }
        var result=new{playing=Application.isPlaying,head=camera.transform.position.ToString("F3"),look=camera.transform.eulerAngles.ToString("F2"),pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name,rows};
        Directory.CreateDirectory("AgentScripts/ReviewImages/BurialGaps");File.WriteAllText("AgentScripts/ReviewImages/BurialGaps/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new{result.playing,result.head,result.look,result.pipeline,renderers=rows.Count};
    }
}
