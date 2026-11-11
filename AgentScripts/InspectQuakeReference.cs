using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
public static class InspectQuakeReference
{
    public static async Task<object> CheckReview()
    {
        var go=new GameObject("Temporary review video validation"){hideFlags=HideFlags.HideAndDontSave};
        var player=go.AddComponent<VideoPlayer>();player.playOnAwake=false;player.audioOutputMode=VideoAudioOutputMode.None;
        player.url=Path.GetFullPath("AgentScripts/ReviewImages/AftershockRevision/Motion.mp4");
        try
        {
            player.Prepare();var deadline=DateTime.UtcNow.AddSeconds(15);
            while(!player.isPrepared&&DateTime.UtcNow<deadline)await Task.Delay(25);
            if(!player.isPrepared)throw new Exception("Saved movie did not decode");
            var result=new{prepared=player.isPrepared,player.length,player.frameCount,player.frameRate,player.width,player.height};
            File.WriteAllText("AgentScripts/ReviewImages/AftershockRevision/Movie.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{player.Stop();UnityEngine.Object.DestroyImmediate(go);}
    }
    public static async Task<object> Run()
    {
        var names = new[]{"Unreinforced", "ReinforcedModerate", "ReinforcedFailure"};
        var results = new System.Collections.Generic.List<object>();
        foreach (var name in names)
        {
            var go = new GameObject("Temporary reference video"){hideFlags=HideFlags.HideAndDontSave};
            var player = go.AddComponent<VideoPlayer>();
            var rt = new RenderTexture(640,360,0);
            var sheet = new Texture2D(640*4,360*3,TextureFormat.RGB24,false);
            var tile = new Texture2D(640,360,TextureFormat.RGB24,false);
            string error=null; player.errorReceived += (p,e)=>error=e;
            player.playOnAwake=false; player.source=VideoSource.Url;
            player.url=Path.GetFullPath("Temp/AftershockReference/"+name+".mp4");
            player.audioOutputMode=VideoAudioOutputMode.None;
            player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=rt;
            var previous=RenderTexture.active;
            try
            {
                player.Prepare();var deadline=DateTime.UtcNow.AddSeconds(20);
                while(!player.isPrepared&&error==null&&DateTime.UtcNow<deadline)await Task.Delay(25);
                if(!player.isPrepared)throw new Exception(error??"Video preparation timed out");
                double duration=player.length;var times=new double[12];
                for(int i=0;i<12;i++)
                {
                    times[i]=duration*(i+.2)/12;
                    bool ready=false;VideoPlayer.FrameReadyEventHandler handler=(p,f)=>ready=true;
                    player.sendFrameReadyEvents=true;player.frameReady+=handler;
                    player.time=times[i];player.Play();deadline=DateTime.UtcNow.AddSeconds(5);
                    while(!ready&&DateTime.UtcNow<deadline)await Task.Delay(15);
                    player.Pause();player.frameReady-=handler;
                    if(!ready)throw new Exception("No decoded frame: "+name);
                    RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,640,360),0,0);tile.Apply();
                    sheet.SetPixels(i%4*640,(2-i/4)*360,640,360,tile.GetPixels());
                }
                sheet.Apply();File.WriteAllBytes("Temp/AftershockReference/"+name+"_Sheet.png",sheet.EncodeToPNG());
                results.Add(new{name,duration,times});
            }
            finally{player.Stop();RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(sheet);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        }
        File.WriteAllText("Temp/AftershockReference/Frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
        return results;
    }
}
