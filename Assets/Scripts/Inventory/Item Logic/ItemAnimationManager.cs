using JetBrains.Annotations;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class ItemAnimationManager : MonoBehaviour
    {
        private bool _canQueueAttack;
        private bool _swingEnding; // This exists to prevent the player from attacking right after the combo time window is closed
        private Animator _recoilAnimator;
        
        public delegate void LogicCallback();
        private LogicCallback _animationEventCallback;

        public delegate void SwingStartedHandler();
        public delegate void SwingCompletedHandler();
        public event SwingStartedHandler SwingStarted;
        public event SwingCompletedHandler SwingCompleted;

        public void AttackMelee(string triggerName, LogicCallback animationEventCallback = null)
        {
            if (_swingEnding) return;
            
            _recoilAnimator ??= GetComponent<Animator>();
            
            // Check if the player attacked while an attack is being animated.
            // Check if the attack is timed between the attack queue opening and the end of the swing.
            // If so, set queued to true so the next animation plays automatically.
            var attackQueued = _recoilAnimator.GetBool("attackQueued");
            if (_recoilAnimator.GetBool("swinging"))
            {
                if (_canQueueAttack && !attackQueued) _recoilAnimator.SetBool("attackQueued", true);
                return;
            }
            
            // Prevent triggering a swing if an attack is queued
            if (attackQueued) return;
            
            _recoilAnimator.SetBool("swinging", true);
            _recoilAnimator.SetBool("attackQueued", false);
            _recoilAnimator.SetTrigger(triggerName);
            _canQueueAttack = false;
            _animationEventCallback = animationEventCallback;
        }

        // Designed to be called from an animation event
        // when a swing starts so the queue is reset.
        [UsedImplicitly]
        public void SwingStart()
        {
            _recoilAnimator.SetBool("attackQueued", false);
            SwingStarted?.Invoke();
        }
        
        // This is designed to be called from an animation event
        // when the animation is at the point where we can start
        // registering new attacks.
        // This is so that you need to wait a tiny bit of time between attacks
        // for them to combo. You can't just click 3 times really fast
        // to execute the whole combo.
        [UsedImplicitly]
        public void AllowQueue()
        {
            _canQueueAttack = true;
        }

        // Designed to be called from an animation event
        // when an animation should invoke a callback function.
        // E.g. when a secondary swing needs to make the player lunge forwards.
        [UsedImplicitly]
        public void AnimationEventCallback()
        {
            _animationEventCallback?.Invoke();
        }

        // This is designed to be called from an animation event
        // when a melee attack is complete. This determines
        // whether to continue a combo or not.
        [UsedImplicitly]
        public void SwingComplete()
        {
            // Check if a swing is queued
            // If so, keep "swinging" as True, but clear the queue
            // Otherwise, set it to False
            if (!_recoilAnimator.GetBool("attackQueued"))
            {
                _recoilAnimator.SetBool("swinging", false);
                _swingEnding = true;
            }
            else
            {
                _canQueueAttack = false;
            }
            
            SwingCompleted?.Invoke();
        }

        // Designed to be called from an animation event
        // to reset the swing animation stuff completely.
        // This should probably be done when the idle animation starts.
        [UsedImplicitly]
        public void SwingReset()
        {
            _canQueueAttack = false;
            _swingEnding = false;
            if (!_recoilAnimator) return;
            _recoilAnimator.SetBool("swinging", false);
            _recoilAnimator.SetBool("attackQueued", false);
            _recoilAnimator.ResetTrigger("swinging");
        }
    }
}
