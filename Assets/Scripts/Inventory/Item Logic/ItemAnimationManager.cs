using JetBrains.Annotations;
using UnityEngine;

namespace Inventory.Inventory.Item_Logic
{
    public class ItemAnimationManager : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer handLeftSr, handRightSr, equippedItemSr;
        [SerializeField] private TrailRenderer meleeTrailRenderer;
        
        private bool _canQueueAttack;
        private bool _swingEnding; // This exists to prevent the player from attacking right after the combo time window is closed
        private Animator _recoilAnimator;
        private int _attackIndex;
        private int _altIdleIndex;
        private string _lastTriggerName;
        
        public delegate void LogicCallback();
        private LogicCallback _animationEventCallback;

        public delegate void SwingStartedHandler();
        public delegate void SwingCompletedHandler();
        public event SwingStartedHandler SwingStarted;
        public event SwingCompletedHandler SwingCompleted;

        public void AttackMelee(string triggerName, LogicCallback animationEventCallback = null, bool roundRobin = false, int maxAttacks = 3)
        {
            // TODO: This might need improvement
            // There is probably still an issue if the player attacks with a round robin attack a couple times,
            // and then changes to a different weapon of the same type that also uses round robin. This would cause
            // the attack to start at the wrong index.
            if (triggerName != _lastTriggerName || !roundRobin)
            {
                _attackIndex = 0;
                _altIdleIndex = 0;
            }
            
            if (_swingEnding) return;
            
            _recoilAnimator ??= GetComponent<Animator>();
            
            // Check if the player attacked while an attack is being animated.
            // Check if the attack is timed between the attack queue opening and the end of the swing.
            // If so, set queued to true so the next animation plays automatically.
            var attackQueued = _recoilAnimator.GetBool("attackQueued");
            if (_recoilAnimator.GetBool("swinging"))
            {
                if (!_canQueueAttack || attackQueued) return;
                _recoilAnimator.SetBool("attackQueued", true);
                _recoilAnimator.SetInteger("attackIndex", _attackIndex);
                IncrementAttackIndex(maxAttacks);
                return;
            }
            
            // Prevent triggering a swing if an attack is queued
            if (attackQueued) return;

            if (roundRobin) _altIdleIndex = (_altIdleIndex + 1) % 2;
            
            _recoilAnimator.SetInteger("attackIndex", _attackIndex);
            _recoilAnimator.SetInteger("altIdleIndex", _altIdleIndex);
            _recoilAnimator.SetBool("swinging", true);
            _recoilAnimator.SetBool("attackQueued", false);
            _recoilAnimator.SetBool("roundRobin", roundRobin);
            _recoilAnimator.SetTrigger(triggerName);
            _canQueueAttack = false;
            _animationEventCallback = animationEventCallback;
            
            IncrementAttackIndex(maxAttacks);
            _lastTriggerName = triggerName;
        }

        private void IncrementAttackIndex(int maxAttacks = 3)
        {
            // Debug.Log($"Attack ({_attackIndex}) @ {Time.time}");
            _attackIndex++;
            if (_attackIndex >= maxAttacks) _attackIndex = 0;
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

        // This is designed to be called from an animation event
        // when the player's hand(s) and weapon should switch to be behind or in front of the player.
        // E.g. when swinging a sword from side to side
        [UsedImplicitly]
        public void ToggleHandsLayerOrder(int bothHands = 0)
        {
            if (handLeftSr.sortingOrder == 9)
            {
                handLeftSr.sortingOrder = 12;
                equippedItemSr.sortingOrder = 11;
                meleeTrailRenderer.sortingLayerName = "Healthbars";
            }
            else
            {
                handLeftSr.sortingOrder = 9;
                equippedItemSr.sortingOrder = 8;
                meleeTrailRenderer.sortingLayerName = "Player";
            }

            handRightSr.sortingOrder = bothHands > 0 ? handLeftSr.sortingOrder : 12;
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
