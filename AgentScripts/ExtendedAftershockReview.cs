using System;
using System.Linq;
using System.IO;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ExtendedAftershockReview
{
    public static object Capture()
    {
        if(!Application.isPlaying || DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("Desktop Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var sections=UnityEngine.Object.FindObjectsByType<AftershockFractureSection>(FindObjectsSortMode.None).OrderBy(s=>s.OpensAtSeconds).ToArray();
        bool enabled=sequence.enabled;sequence.enabled=false;
        var go=new GameObject("Temporary extended fracture inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);
        camera.transform.SetPositionAndRotation(Camera.main.transform.position,Quaternion.Euler(12f,180f,0f));camera.fieldOfView=82f;camera.nearClipPlane=0.01f;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=0.45f;light.range=2.2f;light.shadows=LightShadows.None;
        var paths=new System.Collections.Generic.List<string>();
        try
        {
            sequence.RestoreAftershock();paths.Add(Save(camera,"Before_Later_Fractures"));sequence.BeginAftershockNow();float previous=0f;
            foreach(float time in new[]{8.2f,11.1f,14.2f,17.2f,31f})
            {
                float delta=time-previous;sequence.Advance(delta);dust.Advance(delta);previous=time;
                UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();
                foreach(var system in sequence.GetComponentsInChildren<ParticleSystem>(true))if(system.particleCount>0)system.Simulate(0.1f,false,false,false);
                paths.Add(Save(camera,"Rear_"+time.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"s"));
            }
            for(int i=0;i<sections.Length;i++)
            {
                var point=sections[i].transform.position;
                camera.transform.position=point+Vector3.forward*0.43f+Vector3.up*0.025f;camera.transform.rotation=Quaternion.LookRotation(point-camera.transform.position,Vector3.up);camera.fieldOfView=55f;
                paths.Add(Save(camera,"Rebar_Closeup_"+(i+1)));
            }
            return new{paths=paths.ToArray()};
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;UnityEngine.Object.DestroyImmediate(go);}
    }
    static string Save(Camera camera,string label)
    {
        Directory.CreateDirectory("Temp/ExtendedAftershockReview");var previous=RenderTexture.active;var target=new RenderTexture(1000,800,24);var image=new Texture2D(1000,800,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();string path=Path.GetFullPath("Temp/ExtendedAftershockReview/"+label+".png");File.WriteAllBytes(path,image.EncodeToPNG());return path;}
        finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    public static object BackupAndInspect()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();
        string backup="Assets/_Project/Scenes/Backups/BeforeExtendedAftershock_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed.");
        return new{backup,steel=GameObject.Find("Rear Slab Reinforcement").GetComponent<Renderer>().sharedMaterial.name,
            materials=new[]{"Rear Inner Fracture Left","Rear Inner Fracture Center","Rear Inner Fracture Right"}.Select(n=>new{name=n,material=GameObject.Find(n).GetComponent<Renderer>().sharedMaterial.name}).ToArray(),
            pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name};
    }
}
