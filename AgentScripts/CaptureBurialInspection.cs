using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public static class CaptureBurialInspection
{
    public static object Run()
    {
        var main=Camera.main;var go=new GameObject("Temporary burial inspection"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.fieldOfView=90;camera.aspect=1;camera.transform.position=main.transform.position;
        camera.GetUniversalAdditionalCameraData().requiresDepthOption=CameraOverrideOption.On;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=4;light.intensity=2;light.color=new Color(1,.95f,.85f);light.shadows=LightShadows.None;
        var target=new RenderTexture(480,480,24);var pixels=new Texture2D(480,480,TextureFormat.RGB24,false);var previous=RenderTexture.active;camera.targetTexture=target;
        try
        {
            for(int i=0;i<8;i++)
            {
                camera.transform.rotation=Quaternion.Euler(-15,i*45,0);camera.Render();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,480,480),0,0);pixels.Apply();File.WriteAllBytes("AgentScripts/ReviewImages/BurialGaps/Inspect_"+i+".png",pixels.EncodeToPNG());
            }
            return "8 inspection views; temporary light, not gameplay capture";
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);}
    }
}
