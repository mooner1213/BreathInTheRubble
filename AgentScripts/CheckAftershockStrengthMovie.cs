using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
public static class CheckAftershockStrengthMovie
{
    public static async Task<object> Run()
    {
        var go=new GameObject("Temporary strength movie validation"){hideFlags=HideFlags.HideAndDontSave};var video=go.AddComponent<UnityEngine.Video.VideoPlayer>();
        video.playOnAwake=false;video.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;video.url=Path.GetFullPath("AgentScripts/ReviewImages/AftershockStrength/Episode.mp4");
        try
        {
            video.Prepare();var deadline=DateTime.UtcNow.AddSeconds(12);while(!video.isPrepared&&DateTime.UtcNow<deadline)await Task.Delay(25);
            if(!video.isPrepared)throw new Exception("Saved movie did not decode");
            var result=new{video.isPrepared,video.length,video.frameCount,video.frameRate,video.width,video.height};
            File.WriteAllText("AgentScripts/ReviewImages/AftershockStrength/Movie.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{video.Stop();UnityEngine.Object.DestroyImmediate(go);}
    }
}
