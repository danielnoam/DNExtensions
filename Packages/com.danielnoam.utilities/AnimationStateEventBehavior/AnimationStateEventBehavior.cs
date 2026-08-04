using UnityEngine;

namespace DNExtensions.Utilities
{
    public class AnimationStateEventBehavior : StateMachineBehaviour
    {
        public string eventName;
        public TriggerAt triggerAt = TriggerAt.During;
        [Range(0f, 1f)] public float triggerTime;
    
        public enum TriggerAt { OnEnter, During, OnExit, }
    
        private bool _hasTriggered;
        private float _previousNormalizedTime;
    
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _hasTriggered = false;
            _previousNormalizedTime = 0f;

            if (triggerAt == TriggerAt.OnEnter)
            {
                NotifyReceiver(animator);
                _hasTriggered = true;
            }
        }
    
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (triggerAt != TriggerAt.During) return;
    
            float currentTime = triggerTime >= 1f ? stateInfo.normalizedTime : stateInfo.normalizedTime % 1f;

            if (stateInfo.loop && currentTime < _previousNormalizedTime)
            {
                _hasTriggered = false;
            }

            if (!_hasTriggered && currentTime >= triggerTime)
            {
                NotifyReceiver(animator);
                _hasTriggered = true;
            }

            _previousNormalizedTime = currentTime;
        }
    
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (triggerAt == TriggerAt.OnExit)
            {
                NotifyReceiver(animator);
                _hasTriggered = true;
            }
        }

        private void NotifyReceiver(Animator animator)
        {
            AnimationEventReceiver receiver = animator.GetComponent<AnimationEventReceiver>();

            receiver?.OnAnimationEventTriggered(eventName);
        }
    }
}
