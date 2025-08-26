using UnityEngine;

namespace DNExtensions.ControllerRumbleSystem
{
    public class ControllerRumbleEffect
    {
        public readonly float LowFrequency;
        public readonly float HighFrequency;
        public readonly float Duration;
        public readonly AnimationCurve LowFrequencyCurve;
        public readonly AnimationCurve HighFrequencyCurve;


        public float ElapsedTime { get; private set; }
        public bool IsExpired => ElapsedTime >= Duration;


        public ControllerRumbleEffect(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFrequencyCurve = null, AnimationCurve highFrequencyCurve = null)
        {
            LowFrequency = Mathf.Clamp01(lowFrequency);
            HighFrequency = Mathf.Clamp01(highFrequency);
            Duration = Mathf.Max(0f, duration);
            LowFrequencyCurve = lowFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
            HighFrequencyCurve = highFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
        }



        public void Update(float deltaTime)
        {
            ElapsedTime += deltaTime;
        }
    }
    
    
    [System.Serializable]
    public class ControllerRumbleEffectSettings
    {
        [Range(0f,1f)] public float lowFrequency = 0.3f;
        [Range(0f,1f)] public float highFrequency = 0.3f;
        [Min(0)] public float duration = 0.3f;
        public AnimationCurve lowFrequencyCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public AnimationCurve highFrequencyCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        
        public ControllerRumbleEffectSettings()
        {
        }
        
        public ControllerRumbleEffectSettings(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFrequencyCurve = null, AnimationCurve highFrequencyCurve = null)
        {
            this.lowFrequency = Mathf.Clamp01(lowFrequency);
            this.highFrequency = Mathf.Clamp01(highFrequency);
            this.duration = Mathf.Max(0f, duration);
            this.lowFrequencyCurve = lowFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
            this.highFrequencyCurve = highFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
        }
    }
}