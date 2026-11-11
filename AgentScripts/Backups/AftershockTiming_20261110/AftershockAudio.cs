using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace BreathInTheRubble.Prototype
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AftershockAudio : MonoBehaviour
    {
        [SerializeField] HapticImpulsePlayer[] hands = System.Array.Empty<HapticImpulsePlayer>();
        [SerializeField, Min(1f)] float rumbleSeconds = 18f;
        AudioSource source;
        AudioClip rumble, impact;
        void Awake()
        {
            source=GetComponent<AudioSource>();source.playOnAwake=false;
            rumble=CreateCue("Prototype structural rumble",rumbleSeconds,57f,0.13f);
            impact=CreateCue("Prototype rubble impact",0.18f,85f,0.23f);
        }
        public void Begin(float seconds = 18f)
        {
            if(source==null)return;
            source.Stop();
            if(rumble==null || Mathf.Abs(rumble.length-seconds)>0.05f)
            {
                if(rumble!=null)Destroy(rumble);
                rumble=CreateCue("Prototype structural rumble",Mathf.Max(1f,seconds),57f,0.13f);
            }
            source.clip=rumble;source.Play();
        }
        public void Impact()
        {
            if(source!=null && impact!=null)source.PlayOneShot(impact,0.65f);
            // Never query simulated devices for haptic output during desktop testing.
            bool physicalTracked=false;
            foreach(var d in UnityEngine.InputSystem.InputSystem.devices)
                if(d is UnityEngine.InputSystem.XR.XRHMD h && !d.layout.Contains("Simulated") && h.isTracked.ReadValue()>0.5f)physicalTracked=true;
            if(physicalTracked)foreach(var player in hands)if(player!=null)player.SendHapticImpulse(0.18f,0.12f);
        }
        public void Stop(){if(source!=null)source.Stop();}
        static AudioClip CreateCue(string name,float seconds,float hz,float gain)
        {
            const int rate=22050;var data=new float[Mathf.CeilToInt(rate*seconds)];var random=new System.Random(991);
            float low=0f;
            for(int i=0;i<data.Length;i++)
            {
                float time=i/(float)rate;low=Mathf.Lerp(low,(float)random.NextDouble()*2f-1f,0.03f);
                float envelope=Mathf.Min(Mathf.Clamp01(time/0.08f),Mathf.Clamp01((seconds-time)/0.3f));
                data[i]=(Mathf.Sin(time*hz*Mathf.PI*2f)*0.45f+low)*gain*envelope;
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void OnDisable(){Stop();}
        void OnDestroy(){if(rumble!=null)Destroy(rumble);if(impact!=null)Destroy(impact);}
    }
}
