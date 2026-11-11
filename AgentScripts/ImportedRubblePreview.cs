using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
public static class ImportedRubblePreview
{
    public static object Before() => CaptureSet("Before");
    public static object After() => CaptureSet("After");
    static object CaptureSet(string label)
    {
        Directory.CreateDirectory("Temp/ImportedRubbleReview");
        var main=Camera.main;
        var go=new GameObject("Temporary Rubble Review Camera"){hideFlags=HideFlags.HideAndDontSave};
        var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;
        camera.transform.position=main.transform.position;camera.nearClipPlane=.02f;camera.fieldOfView=85;
        var lamp=go.AddComponent<Light>();lamp.type=LightType.Point;lamp.intensity=1.8f;lamp.range=3.5f;lamp.color=new Color(1,.9f,.78f);lamp.shadows=LightShadows.Soft;
        var target=new RenderTexture(1200,900,24);var pixels=new Texture2D(1200,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        var names=new[]{"Front","Phone","Rear"};var rotations=new[]{new Vector3(4,0,0),new Vector3(30,48,0),new Vector3(10,180,0)};
        try {for(int i=0;i<names.Length;i++){
            camera.transform.rotation=Quaternion.Euler(rotations[i]);camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1200,900),0,0);pixels.Apply();File.WriteAllBytes("Temp/ImportedRubbleReview/"+label+"_"+names[i]+".png",pixels.EncodeToPNG());
        }}finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);}
        return new{label,folder=Path.GetFullPath("Temp/ImportedRubbleReview")};
    }
}
