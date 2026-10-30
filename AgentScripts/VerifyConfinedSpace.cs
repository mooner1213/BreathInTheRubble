using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class VerifyConfinedSpace
{
    public static object Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play mode required.");
        var objects = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();
        int origins=objects.Count(c=>c.GetType().Name=="XROrigin"), inputs=objects.Count(c=>c.GetType().Name=="InputActionManager");
        if(origins!=1 || inputs!=1) throw new Exception("XR setup duplicated.");
        var camera=Camera.main;
        var environment=GameObject.Find("Prototype Environment").transform;
        if(Physics.OverlapSphere(camera.transform.position,0.12f).Any(c=>c.transform.IsChildOf(environment))) throw new Exception("Head intersects rubble.");
        var liquid=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>();
        var bottle=liquid.transform.parent;
        var body=bottle.GetComponent<Rigidbody>();
        if(liquid.FillFraction!=0.45f || bottle.position.z>=0f || bottle.position.y<0.24f) throw new Exception("Bottle placement / fill failed.");
        var grip=bottle.GetComponent<XRGrabInteractable>();
        var interactor=UnityEngine.Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None).First(i=>i.name.Contains("Near-Far"));
        var manager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
        manager.SelectEnter((IXRSelectInteractor)interactor,(IXRSelectInteractable)grip);
        if(!grip.isSelected) throw new Exception("Bottle selection failed.");
        manager.SelectExit((IXRSelectInteractor)interactor,(IXRSelectInteractable)grip);
        var initialRotation=bottle.rotation;
        bool kinematic=body.isKinematic; body.isKinematic=true;
        var update=typeof(BottleLiquidVisual).GetMethod("UpdateLiquid",BindingFlags.NonPublic|BindingFlags.Instance);
        var interior=(Mesh)new SerializedObject(liquid).FindProperty("interiorMesh").objectReferenceValue;
        float total=Volume(interior);
        var outcomes=new System.Collections.Generic.List<string>();
        try
        {
            foreach(float angle in new[]{0f,45f,90f,140f,180f})
            {
                bottle.rotation=Quaternion.Euler(0f,0f,angle);
                update.Invoke(liquid,new object[]{true});
                var mesh=liquid.GetComponent<MeshFilter>().sharedMesh;
                float fraction=Volume(mesh)/total;
                if(Mathf.Abs(fraction-0.45f)>0.002f) throw new Exception("Liquid volume incorrect at "+angle+": "+fraction);
                var world=mesh.vertices.Select(v=>liquid.transform.TransformPoint(v)).ToArray();
                // The cap is the final triangle fan and must stay horizontal, including upside down.
                var tr=mesh.triangles; float top=world.Max(v=>v.y);
                if(Mathf.Abs(world[tr[tr.Length-1]].y-top)>0.0002f || Mathf.Abs(world[tr[tr.Length-2]].y-top)>0.0002f)
                    throw new Exception("Water surface is not level.");
                outcomes.Add(angle+"deg="+fraction.ToString("P2"));
            }
        }
        finally { bottle.rotation=initialRotation; update.Invoke(liquid,new object[]{true}); body.isKinematic=kinematic; }
        var systems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach(var system in systems.Where(s=>s.main.loop))
        {
            system.Simulate(120f,false,true,true);
            if(system.particleCount==0 || system.particleCount>system.main.maxParticles) throw new Exception("Dust failed after two minutes.");
            system.Play();
        }
        var locomotion=objects.OfType<Behaviour>().Where(c=>c.enabled && (c.GetType().Name.Contains("MoveProvider") || c.GetType().Name.Contains("TurnProvider") || c.GetType().Name.Contains("TeleportationProvider"))).Select(c=>c.GetType().Name).ToArray();
        if(locomotion.Length>0)throw new Exception("Artificial locomotion is enabled.");
        return new {result="PASS",frame=Time.frameCount,time=Time.time,origins,inputs,camera=camera.transform.position.ToString(),headClearance=true,
            bottle=bottle.position.ToString(),bottleGrab="select/release passed",liquid=outcomes.ToArray(),
            dust=systems.Select(s=>s.name+": alive="+s.particleCount+", loop="+s.main.loop+", max="+s.main.maxParticles).ToArray(),locomotion};
    }

    static float Volume(Mesh mesh)
    {
        var v=mesh.vertices;var t=mesh.triangles;float sum=0;
        for(int i=0;i<t.Length;i+=3)sum+=Vector3.Dot(v[t[i]],Vector3.Cross(v[t[i+1]],v[t[i+2]]))/6f;
        return Mathf.Abs(sum);
    }

    public static object Observe() => new {frame=Time.frameCount,time=Time.time,bottle=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>().transform.parent.position.ToString(),
        dust=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Select(s=>s.name+"="+s.particleCount).ToArray()};

    public static string PreviewFront() => Preview("ConfinedFrontPreview.png",new Vector3(0f,0.90f,0f),new Vector3(0f,0.62f,0.67f));
    public static string PreviewRear() => Preview("ConfinedRearPreview.png",new Vector3(0f,0.90f,0f),new Vector3(-0.28f,0.43f,-0.43f));
    static string Preview(string file,Vector3 position,Vector3 target)
    {
        // Inspection lighting and a temporary camera only. The saved scene and tracked HMD are untouched.
        GameObject go=null,fill=null; RenderTexture rt=null;Texture2D output=null;
        var controllerRenderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(r=>r.transform.root.name=="XR Origin (Seated)" && r.enabled).ToArray();
        var previous=RenderTexture.active;
        try
        {
            foreach(var r in controllerRenderers)r.enabled=false;
            go=new GameObject("Temporary environment inspection camera");var cam=go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position)); cam.fieldOfView=78;cam.nearClipPlane=0.02f;cam.farClipPlane=5;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
            fill=new GameObject("Temporary inspection light");var light=fill.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(0.83f,0.87f,0.93f);light.intensity=0.65f;light.range=2.2f;
            fill.transform.position=position+new Vector3(0f,0.10f,0f);light.shadows=LightShadows.Soft;
            rt=new RenderTexture(1280,900,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            output=new Texture2D(1280,900,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1280,900),0,0);output.Apply();
            var path=Path.GetFullPath("Temp/"+file);File.WriteAllBytes(path,output.EncodeToPNG());return path;
        }
        finally {foreach(var r in controllerRenderers)if(r!=null)r.enabled=true;RenderTexture.active=previous;if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(fill!=null)UnityEngine.Object.DestroyImmediate(fill);if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
    }
}
