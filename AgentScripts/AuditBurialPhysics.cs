using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class AuditBurialPhysics
{
    public static object Run()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var all=manager.Elements.Where(e=>e.loose==null).SelectMany(e=>e.target.GetComponents<MeshCollider>()).Where(c=>c.enabled&&c.convex).ToArray();
        var rows=all.Select(c=>new{c.name,parent=c.transform.parent?.name,mesh=c.sharedMesh.name,size=c.bounds.size.ToString("F2")}).ToArray();
        File.WriteAllText("AgentScripts/ReviewImages/BurialGaps/ConvexSupports.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return new{convexSupports=all.Length,examples=rows.Take(8)};
    }
}
