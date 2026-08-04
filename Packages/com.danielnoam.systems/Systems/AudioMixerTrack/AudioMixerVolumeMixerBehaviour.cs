using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace DNExtensions.Systems.AudioMixerTrack
{
    public class AudioMixerVolumeMixerBehaviour : PlayableBehaviour
    {
        public bool HoldValueAfterClips = true;

        private AudioMixer _mixer;
        private string _activeParameter;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData is not AudioMixer mixer) return;
            _mixer = mixer;

            float volume = 0f;
            float totalWeight = 0f;
            float dominantWeight = 0f;
            string parameter = null;
            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var input = (ScriptPlayable<AudioMixerVolumeBehaviour>)playable.GetInput(i);
                AudioMixerVolumeBehaviour behaviour = input.GetBehaviour();
                if (behaviour.VolumeCurve == null) continue;

                double duration = input.GetDuration();
                float clipTime = duration > 0d ? (float)(input.GetTime() / duration) : 0f;

                volume += behaviour.VolumeCurve.Evaluate(clipTime) * weight;
                totalWeight += weight;

                if (weight > dominantWeight)
                {
                    dominantWeight = weight;
                    parameter = behaviour.ExposedParameter;
                }
            }

            if (totalWeight <= 0f)
            {
                if (!HoldValueAfterClips) ClearActiveParameter();
                return;
            }

            if (!string.IsNullOrEmpty(_activeParameter) && _activeParameter != parameter)
            {
                mixer.ClearFloat(_activeParameter);
            }
            _activeParameter = parameter;

            if (!string.IsNullOrEmpty(parameter))
            {
                mixer.SetFloat(parameter, LinearToDecibel(volume));
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (!Application.isPlaying)
            {
                ClearActiveParameter();
            }
        }

        private void ClearActiveParameter()
        {
            if (_mixer && !string.IsNullOrEmpty(_activeParameter))
            {
                _mixer.ClearFloat(_activeParameter);
            }
            _activeParameter = null;
        }

        private static float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        }
    }
}
