using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class InspectSeismicCoupling
{
    public static object Run()
    {
        Physics.SyncTransforms();var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var root=GameObject.Find("Prototype Environment");
        var pieces=manager.Elements.Where(e=>e.powder!=null).Select(e=>e.target.GetComponent<Collider>()).ToArray();var all=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();var overlaps=new List<object>();
        foreach(var a in pieces)foreach(var b in all)
        {
            if(a==b||!a.bounds.Intersects(b.bounds))continue;
            if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out var normal,out float depth)&&depth>.002f)
                overlaps.Add(new{a=a.name,b=b.name,depth,otherBody=b.attachedRigidbody?.name,normal=normal.ToString("F2")});
        }
        var mapped=new HashSet<Transform>(manager.Elements.Select(e=>e.target));
        var missed=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!mapped.Contains(r.transform)).Select(r=>new{r.name,p=r.transform.position.ToString("F2"),parent=r.transform.parent?.name,body=r.GetComponentInParent<Rigidbody>()?.name,collider=r.GetComponentInParent<Collider>()?.name}).ToArray();
        var groups=root.transform.Cast<Transform>().Select(t=>new{t.name,renderers=t.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled),bodies=t.GetComponentsInChildren<Rigidbody>().Length}).ToArray();
        var result=new{mapped=mapped.Count,missed,groups,overlapCount=overlaps.Count,overlaps=overlaps.Take(70).ToArray(),props=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).Select(g=>new{g.name,g.GetComponent<Rigidbody>().isKinematic,g.GetComponent<Rigidbody>().constraints}).ToArray()};
        Directory.CreateDirectory("AgentScripts/ReviewImages/SeismicCoupling");File.WriteAllText("AgentScripts/ReviewImages/SeismicCoupling/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new{result.mapped,missed=missed.Take(18),result.overlapCount,overlaps=overlaps.Take(18),result.props,result.groups};
    }
}
