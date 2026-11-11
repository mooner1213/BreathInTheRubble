using System;
using System.IO;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RearCollapseReview
{
    public static object InspectAndBackup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();
        string backup="Assets/_Project/Scenes/Backups/BeforeRearCollapse_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed.");
        var bottle=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>().transform.root;
        return new{backup,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,head=Camera.main.transform.position.ToString(),bottle=new{bottle.name,position=bottle.position.ToString(),parts=bottle.GetComponentsInChildren<Renderer>().Select(r=>new{r.name,bounds=r.bounds.ToString()}).ToArray()},
            rear=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.bounds.center.z< -0.25f && (r.name.Contains("Rear")||r.name.Contains("Ceiling")||r.name.Contains("Pocket"))).Select(r=>new{r.name,parent=r.transform.parent.name,position=r.transform.position.ToString(),scale=r.transform.localScale.ToString(),bounds=r.bounds.ToString()}).ToArray()};
    }
    public static object CaptureBefore(){return Capture(false);}
    public static object CaptureAfter(){return Capture(true);}
    static object Capture(bool after)
    {
        if(!Application.isPlaying || DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("Desktop Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        bool enabled=sequence.enabled;sequence.enabled=false;
        var go=new GameObject("Temporary rear inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);
        camera.transform.SetPositionAndRotation(Camera.main.transform.position,Quaternion.Euler(12f,180f,0f));
        camera.fieldOfView=82f;camera.nearClipPlane=0.01f;
        var inspectionLight=go.AddComponent<Light>();inspectionLight.type=LightType.Point;inspectionLight.intensity=0.45f;inspectionLight.range=2.2f;inspectionLight.shadows=LightShadows.None;
        try
        {
            sequence.RestoreAftershock();
            string before=Save(camera,(after?"After":"Before")+"_Rest");
            sequence.BeginAftershockNow();sequence.Advance(5.5f);dust.Advance(1f);
            UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();
            foreach(var system in sequence.GetComponentsInChildren<ParticleSystem>(true))system.Simulate(0.7f,false,false,false);
            string during=Save(camera,(after?"After":"Before")+"_Collapse");
            return new{rest=before,collapse=during};
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;UnityEngine.Object.DestroyImmediate(go);}
    }
    static string Save(Camera camera,string label)
    {
        Directory.CreateDirectory("Temp/RearCollapseReview");var previous=RenderTexture.active;var target=new RenderTexture(1000,800,24);var image=new Texture2D(1000,800,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();string path=Path.GetFullPath("Temp/RearCollapseReview/"+label+".png");File.WriteAllBytes(path,image.EncodeToPNG());return path;}
        finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
