using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class InspectAftershockTiming
{
    public static object Run()
    {
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        var data=new SerializedObject(dust);
        var systems=new[]{"dustVeil","fracturePlumes","awakeningDust","aftershockDust"}.Select(n=>
        {
            var p=data.FindProperty(n);var refs=p.isArray?Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue as ParticleSystem).ToArray():new[]{p.objectReferenceValue as ParticleSystem};
            return new{name=n,systems=refs.Where(s=>s!=null).Select(s=>new{s.name,pos=s.transform.position.ToString("F2"),lifetime=s.main.startLifetime.ToString(),s.main.maxParticles,rate=s.emission.rateOverTime.ToString(),size=s.main.startSize.ToString(),gravity=s.main.gravityModifier.ToString(),s.main.loop,s.main.playOnAwake,simulation=s.main.simulationSpace.ToString(),shape=s.shape.shapeType.ToString(),shapeScale=s.shape.scale.ToString("F2"),material=s.GetComponent<Renderer>().sharedMaterial.name}).ToArray()};
        }).ToArray();
        return new{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,systems,front=manager.Elements.Where(e=>e.target.name.Contains("Front face")||e.target.name.Contains("Center crushed")||e.target.name.Contains("ceiling lip")||e.target.name.Contains("fracture veil")||e.target.name.Contains("failing support")).Select(e=>new{e.target.name,parent=e.target.parent.name,pos=e.target.position.ToString("F3"),e.releaseAt,body=e.target.GetComponent<Rigidbody>()!=null}).ToArray(),activeRain=UnityEngine.Object.FindObjectsByType<AftershockRubbleRain>(FindObjectsSortMode.None).Select(r=>new{r.name,r.enabled}).ToArray(),releaseRange=manager.Elements.Where(e=>e.releaseAt>0).Select(e=>e.releaseAt).ToArray()};
    }
}
