using System.IO;
using UnityEngine;
public static class PreviewEmergencyPhone
{
    public static string Render()
    {
        var phone=Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneDialerView>().transform;
        var go=new GameObject("Temporary phone inspection camera");var camera=go.AddComponent<Camera>();
        RenderTexture target=null;Texture2D output=null;var previous=RenderTexture.active;
        try
        {
            camera.transform.SetPositionAndRotation(phone.position-phone.forward*0.22f,phone.rotation);
            camera.fieldOfView=42f;camera.nearClipPlane=0.01f;camera.farClipPlane=2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            target=new RenderTexture(800,1400,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            output=new Texture2D(800,1400,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,800,1400),0,0);output.Apply();
            var path=Path.GetFullPath("Temp/EmergencyPhoneCloseup.png");File.WriteAllBytes(path,output.EncodeToPNG());return path;
        }
        finally{RenderTexture.active=previous;Object.DestroyImmediate(go);if(output!=null)Object.DestroyImmediate(output);if(target!=null){target.Release();Object.DestroyImmediate(target);}}
    }
}
