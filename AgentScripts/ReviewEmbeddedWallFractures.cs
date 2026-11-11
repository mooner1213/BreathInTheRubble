using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BreathInTheRubble.Prototype;
using UnityEngine;
using UnityEditor;

public static class ReviewEmbeddedWallFractures
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static string Save(Camera camera,string label)
    {
        Directory.CreateDirectory("Temp/EmbeddedWallReview");var prior=RenderTexture.active;var target=new RenderTexture(1280,960,24);var texture=new Texture2D(1280,960,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,960),0,0);texture.Apply();var path=Path.GetFullPath("Temp/EmbeddedWallReview/"+label+".png");File.WriteAllBytes(path,texture.EncodeToPNG());return path;}
        finally{camera.targetTexture=null;RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool wasEnabled=sequence.enabled;sequence.enabled=false;
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var sections=UnityEngine.Object.FindObjectsByType<AftershockFractureSection>(FindObjectsSortMode.None).OrderBy(s=>s.OpensAtSeconds).ToArray();
        var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).OrderBy(c=>c.name).ToArray();
        var starts=chunks.Select(c=>c.transform.position).ToArray();var rotations=chunks.Select(c=>c.transform.rotation).ToArray();var ids=chunks.Select(c=>c.GetInstanceID()).ToArray();
        var props=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).Select(p=>p.transform).ToArray();var propStarts=props.Select(p=>p.position).ToArray();
        var main=Camera.main;var head=main.transform;Vector3 headStart=head.position;Quaternion headRotation=head.rotation;
        var go=new GameObject("Temporary embedded fracture review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.nearClipPlane=.01f;camera.fieldOfView=85f;camera.transform.SetPositionAndRotation(head.position,Quaternion.Euler(10,180,0));
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.45f;light.range=2.2f;light.shadows=LightShadows.None;
        var paths=new List<string>();var samples=new List<object>();
        try
        {
            sequence.RestoreAftershock();dust.Clear();
            Check(sections.Length==3&&chunks.Length==6,"Expected three existing wall sections and six falling skins");
            Check(chunks.All(c=>c.GetComponent<MeshCollider>().convex&&c.GetComponent<Rigidbody>().isKinematic),"Skins not attached initially");
            paths.Add(Save(camera,"01_Intact_Rear"));
            for(int i=0;i<3;i++)
            {
                var point=sections[i].transform.position;camera.transform.position=point+Vector3.forward*.58f+Vector3.up*.03f;camera.transform.rotation=Quaternion.LookRotation(point-camera.transform.position);camera.fieldOfView=65;
                paths.Add(Save(camera,"02_Intact_Wall_"+(i+1)));
            }
            camera.transform.SetPositionAndRotation(head.position,Quaternion.Euler(10,180,0));camera.fieldOfView=85;
            sequence.BeginAftershockNow();float elapsed=0;int firstFrame=Time.frameCount;
            // Drive the same elapsed-time logic across real rendered frames; PhysX remains in its normal FixedUpdate loop.
            var timepoints=new[]{9.9f,10.35f,10.8f,11.7f,13.65f,14.8f,16.45f,17.8f,18.1f,22f,31f};
            double realPrevious=Time.realtimeSinceStartupAsDouble;
            foreach(float until in timepoints)
            {
                while(elapsed<until-.0001f)
                {
                    await Task.Delay(16);double now=Time.realtimeSinceStartupAsDouble;float dt=Mathf.Min((float)(now-realPrevious),.10f);realPrevious=now;dt=Mathf.Min(dt,until-elapsed);
                    sequence.Advance(dt);elapsed+=dt;
                    UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();
                }
                paths.Add(Save(camera,"03_Rear_"+until.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)));
                samples.Add(new{elapsed,frame=Time.frameCount,particles=sections.Select(s=>s.GetComponentInChildren<ParticleSystem>().particleCount).ToArray(),chunks=chunks.Select((c,i)=>new{name=c.name,released=c.Released,drop=starts[i].y-c.transform.position.y,position=c.transform.position.ToString("F3"),rotationDegrees=Quaternion.Angle(rotations[i],c.transform.rotation),speed=c.GetComponent<Rigidbody>().linearVelocity.magnitude,visible=c.GetComponent<Renderer>().enabled}).ToArray()});
            }
            File.WriteAllText("Temp/EmbeddedWallReview/PhysicsSamples.json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));
            Check(Time.frameCount>firstFrame+50,"Real frames did not advance");
            Check(chunks.All(c=>c.Released),"Some wall chunks never detached");
            for(int i=0;i<chunks.Length;i++)Check(starts[i].y-chunks[i].transform.position.y>.07f,"Masonry suspended in air: "+chunks[i].name);
            Check(chunks.Any((c)=>Quaternion.Angle(rotations[Array.IndexOf(chunks,c)],c.transform.rotation)>30f),"No masonry tumbled");
            Check(director.DurationSeconds==18f&&!director.IsRunning&&sequence.Stage==GameStage.Isolation,"18-second timeline regression");
            Check(!dust.IsActive,"Dust not cleared by 30 seconds");
            for(int i=0;i<3;i++)
            {
                var point=sections[i].transform.position;camera.transform.position=point+Vector3.forward*.58f+Vector3.up*.03f;camera.transform.rotation=Quaternion.LookRotation(point-camera.transform.position);camera.fieldOfView=65;
                paths.Add(Save(camera,"04_Exposed_Wall_"+(i+1)));
            }
            float maxPropDrift=props.Select((p,i)=>Vector3.Distance(p.position,propStarts[i])).DefaultIfEmpty().Max();
            Check(maxPropDrift<.025f,"Grabbable prop moved during the visual event");
            Check(chunks.All(c=>props.All(p=>p.GetComponentsInChildren<Collider>().All(other=>Physics.GetIgnoreCollision(c.GetComponent<Collider>(),other)))),"Masonry collides with a grabbable prop");
            Check(sections.All(s=>s.GetComponentInChildren<ParticleSystem>().particleCount==0),"Fracture plume remained after the 30-second dust episode");
            sequence.RestoreAftershock();
            for(int i=0;i<chunks.Length;i++)Check(Vector3.Distance(chunks[i].transform.position,starts[i])<.001f&&Quaternion.Angle(chunks[i].transform.rotation,rotations[i])<.05f&&chunks[i].GetComponent<Rigidbody>().isKinematic,"Chunk reset mismatch: "+chunks[i].name);
            for(int replay=0;replay<4;replay++){sequence.BeginAftershockNow();sequence.Advance(18.1f);sequence.RestoreAftershock();}
            Check(chunks.Select(c=>c.GetInstanceID()).SequenceEqual(ids),"Chunk replay replaced objects");
            var result=new{result="PASS",firstFrame,lastFrame=Time.frameCount,maxPropDrift,headDisplacement=Vector3.Distance(head.position,headStart),headRotation=Quaternion.Angle(head.rotation,headRotation),paths,samples};
            File.WriteAllText("Temp/EmbeddedWallReview/PhysicsVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{sequence.RestoreAftershock();sequence.enabled=wasEnabled;UnityEngine.Object.DestroyImmediate(go);}
    }
}
