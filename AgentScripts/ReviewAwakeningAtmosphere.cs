using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using BreathInTheRubble.Prototype;

public static class ReviewAwakeningAtmosphere
{
    static List<object> frames;
    static double lastMaximum;
    static object Save(Camera camera,string name)
    {
        Directory.CreateDirectory("Temp/AwakeningAtmosphereReview");var prior=RenderTexture.active;var old=camera.targetTexture;var rt=new RenderTexture(1200,900,24);var texture=new Texture2D(1200,900,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();
            var pixels=texture.GetPixels32();var luminance=pixels.Select(p=>(.2126*p.r+.7152*p.g+.0722*p.b)/255.0).ToArray();Array.Sort(luminance);
            var path=Path.GetFullPath("Temp/AwakeningAtmosphereReview/"+name+".png");File.WriteAllBytes(path,texture.EncodeToPNG());
            lastMaximum=luminance[luminance.Length-1];
            var record=new{name,path,mean=luminance.Average(),p95=luminance[(int)(luminance.Length*.95)],maximum=lastMaximum,visibleFraction=luminance.Count(l=>l>.012)/(double)luminance.Length};frames.Add(record);return record;
        }
        finally{camera.targetTexture=old;RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required");frames=new List<object>();
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var reveal=UnityEngine.Object.FindFirstObjectByType<AwakeningReveal>();var awakening=UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();Check(driver!=null,"Desktop test path missing");
        bool sequenceEnabled=sequence.enabled,dustEnabled=dust.enabled,revealEnabled=reveal.enabled;sequence.enabled=false;dust.enabled=false;reveal.enabled=false;
        bool desktop=driver.DesktopEnabled;if(!desktop)driver.SetDesktopMode(true);bool held=driver.IsHolding;bool lamp=driver.Phone.FlashlightEnabled;
        var camera=Camera.main;var pose=camera.transform.position;var rotation=camera.transform.rotation;
        var systems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);var renderers=systems.Select(p=>p.GetComponent<Renderer>()).ToArray();var rendererStates=renderers.Select(r=>r.enabled).ToArray();
        try
        {
            sequence.RestoreAftershock();if(!driver.IsHolding)driver.ToggleHold();driver.Phone.SetFlashlight(false);
            dust.ReplayAwakening();awakening.PlayNow();foreach(var p in systems){if(p.isPlaying)p.Simulate(1f,false,false,false);p.Pause(false);}dust.Advance(1f);reveal.Complete();
            await Task.Delay(80);Save(camera,"01_Startup_LightOff");
            driver.ToggleFlashlight();await Task.Delay(100);Save(camera,"02_Startup_LightOn");
            foreach(var r in renderers)r.enabled=false;Save(camera,"03_Startup_LightOn_NoParticles");for(int i=0;i<renderers.Length;i++)renderers[i].enabled=rendererStates[i];
            reveal.Replay();Check(reveal.Opening==0f,"Eyes are not initially closed");Save(camera,"04_Eyes_Closed");Check(lastMaximum==0,"Phone UI remains visible through closed eyes");
            reveal.Advance(.6f);Check(reveal.Opening==0f,"Initial blackout hold missing");
            reveal.Advance(.7f);Save(camera,"05_Eyes_FirstSlit");reveal.Advance(.7f);Save(camera,"06_Eyes_HalfOpen");reveal.Advance(reveal.DurationSeconds);Check(reveal.IsComplete,"Eyes never fully open");Save(camera,"07_Eyes_Open");
            sequence.BeginAftershockNow();sequence.Advance(7.4f);dust.Advance(1f);foreach(var p in systems){if(p.isPlaying)p.Simulate(.4f,false,false,false);p.Pause(false);}
            UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();driver.Phone.SetFlashlight(false);await Task.Delay(80);Save(camera,"08_Aftershock_LightOff");
            driver.Phone.SetFlashlight(true);await Task.Delay(100);Save(camera,"09_Aftershock_LightOn");
            driver.RotatePhone(new Vector2(-100f,0f));await Task.Delay(80);Save(camera,"09b_Aftershock_AimedLeft");driver.RotatePhone(new Vector2(100f,0f));await Task.Delay(80);
            foreach(var r in renderers)r.enabled=false;Save(camera,"10_Aftershock_LightOn_NoParticles");for(int i=0;i<renderers.Length;i++)renderers[i].enabled=rendererStates[i];
            dust.Advance(19f);Check(dust.IsActive&&Mathf.Abs(dust.Opacity-1f)<.001f,"Haze does not hold to 20 seconds");
            dust.Advance(5f);Save(camera,"11_Aftershock_25s");dust.Advance(5f);Check(!dust.IsActive&&systems.All(p=>p.particleCount==0),"Dust does not clear at 30s");Save(camera,"12_Aftershock_30s");
            Check(Vector3.Distance(camera.transform.position,pose)<.015f&&Quaternion.Angle(camera.transform.rotation,rotation)<2f,"Atmosphere changed the camera pose");
            var errors=ShaderUtil.GetShaderMessages(Shader.Find("BreathInTheRubble/Light Scattering Dust")).Concat(ShaderUtil.GetShaderMessages(Shader.Find("BreathInTheRubble/Awakening Eyelids"))).ToArray();Check(!errors.Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader compilation failed");
            var result=new{result="PASS",frames,particles=systems.Length,shaders=errors.Select(m=>m.message).ToArray(),openingSeconds=reveal.DurationSeconds};File.WriteAllText("Temp/AwakeningAtmosphereReview/Review.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=rendererStates[i];
            sequence.RestoreAftershock();driver.Phone.SetFlashlight(lamp);if(!held)driver.ReturnPhone();if(!desktop)driver.SetDesktopMode(false);
            reveal.Complete();reveal.enabled=revealEnabled;reveal.Complete();dust.enabled=dustEnabled;sequence.enabled=sequenceEnabled;
        }
    }
}
