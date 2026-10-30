using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class InspectBuriedViews
{
    public static object RenderAll()
    {
        var head=Camera.main.transform.position;
        var rotations=new[]{Quaternion.Euler(15f,0f,0f),Quaternion.Euler(10f,-90f,0f),Quaternion.Euler(12f,90f,0f),Quaternion.Euler(18f,180f,0f),Quaternion.Euler(78f,0f,0f)};
        var names=new[]{"front","left","right","rear","down"};
        var files=new string[5];for(int i=0;i<5;i++)files[i]=Render(head,rotations[i],names[i]);
        return new{head=head.ToString(),files};
    }
    static string Render(Vector3 position,Quaternion rotation,string name)
    {
        var go=new GameObject("Temporary buried-space inspection camera");var fill=new GameObject("Temporary inspection light");
        var hidden=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.transform.root.name=="XR Origin (Seated)").ToArray();
        var camera=go.AddComponent<Camera>();var light=fill.AddComponent<Light>();
        RenderTexture rt=null;Texture2D output=null;var previous=RenderTexture.active;
        try
        {
            foreach(var r in hidden)r.enabled=false;
            camera.transform.SetPositionAndRotation(position,rotation);camera.nearClipPlane=0.02f;camera.farClipPlane=4f;camera.fieldOfView=84f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            light.type=LightType.Point;light.intensity=0.48f;light.range=2.2f;light.shadows=LightShadows.Soft;fill.transform.position=position+new Vector3(0f,0.07f,0f);
            rt=new RenderTexture(1080,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            output=new Texture2D(1080,900,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1080,900),0,0);output.Apply();
            var path=Path.GetFullPath("Temp/Buried_"+name+".png");File.WriteAllBytes(path,output.EncodeToPNG());return path;
        }
        finally{foreach(var r in hidden)if(r!=null)r.enabled=true;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(fill);if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
    }
}
