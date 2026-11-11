using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Shared episode timing; independent of XR input, camera motion, and rendering.</summary>
    public static class AftershockEnvelope
    {
        public const float BuildUpEnd = 5f;
        public const float PeakEnd = 10f;
        public const float MotionEnd = 20f;
        public const float DustEnd = 30f;

        public static float Motion(float seconds)
        {
            if(seconds<=0f||seconds>=MotionEnd)return 0f;
            if(seconds<BuildUpEnd)return Mathf.SmoothStep(0f,1f,seconds/BuildUpEnd);
            if(seconds<=PeakEnd)return 1f;
            return 1f-Mathf.SmoothStep(0f,1f,(seconds-PeakEnd)/(MotionEnd-PeakEnd));
        }
        public static float Dust(float seconds)
        {
            if(seconds<=0f||seconds>=DustEnd)return 0f;
            if(seconds<BuildUpEnd)return .16f*Mathf.SmoothStep(0f,1f,seconds/BuildUpEnd);
            if(seconds<7f)return Mathf.Lerp(.16f,1f,Mathf.SmoothStep(0f,1f,(seconds-BuildUpEnd)/2f));
            if(seconds<=PeakEnd)return 1f;
            return 1f-Mathf.SmoothStep(0f,1f,(seconds-PeakEnd)/(DustEnd-PeakEnd));
        }
    }
}
