using System;
using JetBrains.Annotations;
using UnityEngine;
using Utilities;

namespace Inventory.Item_Logic
{
    public class ItemAnimationManager : MonoBehaviour
    {
        public struct AttackMeleeParameters
        {
            public string triggerName;
            public LogicCallback animationEventCallback;
            public bool roundRobin;
            public int maxAttacks;
            public int particleIndex;
            public Vector2 particleOffset;
            public Color particleColor;
            public AudioClip[] attackSounds;

            public AttackMeleeParameters(string triggerName)
            {
                this.triggerName = triggerName;
                animationEventCallback = null;
                roundRobin = false;
                maxAttacks = 3;
                particleIndex = -1;
                particleOffset = Vector2.zero;
                particleColor = Color.white;
                attackSounds = null;
            }
        }
        
        [SerializeField] private SpriteRenderer handLeftSr, handRightSr, equippedItemSr;
        [SerializeField] private TrailRenderer meleeTrailRenderer;
        [SerializeField, Tooltip("Particles that can be spawned by items as they're used.")] private GameObject[] particlePrefabs;
        [SerializeField] private AudioSource itemAudioSource;
        
        private bool _canQueueAttack;
        private bool _swingEnding; // This exists to prevent the player from attacking right after the combo time window is closed
        private Animator _recoilAnimator;
        private int _attackIndex;
        private int _altIdleIndex;
        private string _lastTriggerName;
        private GameObject _effectObject;
        private GameObject _particleObject;
        private Vector2 _particleOffset;
        private Color _particleColor;
        private AudioClip[] _attackSounds;
        
        public delegate void LogicCallback();
        private LogicCallback _animationEventCallback;

        public delegate void SwingStartedHandler(bool trailState);
        public delegate void SwingCompletedHandler();
        public event SwingStartedHandler SwingStarted;
        public event SwingCompletedHandler SwingCompleted;

        public void AttackMelee(AttackMeleeParameters parameters)
        {
            // TODO: This might need improvement
            // There is probably still an issue if the player attacks with a round robin attack a couple times,
            // and then changes to a different weapon of the same type that also uses round robin. This would cause
            // the attack to start at the wrong index.
            if (parameters.triggerName != _lastTriggerName || !parameters.roundRobin)
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
                IncrementAttackIndex(parameters.maxAttacks);
                return;
            }
            
            // Prevent triggering a swing if an attack is queued
            if (attackQueued) return;

            if (parameters.roundRobin) _altIdleIndex = (_altIdleIndex + 1) % 2;
            
            _recoilAnimator.SetInteger("attackIndex", _attackIndex);
            _recoilAnimator.SetInteger("altIdleIndex", _altIdleIndex);
            _recoilAnimator.SetBool("swinging", true);
            _recoilAnimator.SetBool("attackQueued", false);
            _recoilAnimator.SetBool("roundRobin", parameters.roundRobin);
            _recoilAnimator.SetTrigger(parameters.triggerName);
            _canQueueAttack = false;
            _animationEventCallback = parameters.animationEventCallback;
            
            IncrementAttackIndex(parameters.maxAttacks);
            _lastTriggerName = parameters.triggerName;

            if (parameters.particleIndex >= 0)
            {
                if (!_effectObject)
                {
                    _effectObject = new GameObject("EffectObject");
                    _effectObject.transform.SetParent(transform);
                }
                
                _particleObject = particlePrefabs[parameters.particleIndex];
                _particleOffset = parameters.particleOffset;
                _particleColor = parameters.particleColor;
            }
            else
            {
                _particleObject = null;
            }

            if (parameters.attackSounds?.Length > 0)
            {
                _attackSounds = parameters.attackSounds;
            }
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
        public void SwingStart(int trailState)
        {
            _recoilAnimator.SetBool("attackQueued", false);
            SwingStarted?.Invoke(trailState != 0);
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

        // Designed to be called from an animation event
        // when a effects like particles or sound should be played.
        // E.g. when a pickaxe hits something.
        [UsedImplicitly]
        public void PlayEffects()
        {
            if (_attackSounds?.Length > 0)
            {
                var sound = _attackSounds[UnityEngine.Random.Range(0, _attackSounds.Length)];
                itemAudioSource.PlayOneShot(sound);
                _attackSounds = null;
            }
            
            if (!_particleObject) return;
            
            ObjectPooler.CreatePoolIfDoesntExist(_particleObject.name, _particleObject, 5, true);
            var particle = ObjectPooler.GetObject(_particleObject.name);
            
            if (!particle) return;

            var equippedTransform = equippedItemSr.transform;
            var position = equippedTransform.position;
            
            particle.transform.position = position;
            particle.transform.Translate(_particleOffset, Space.Self);
            particle.transform.rotation = equippedTransform.rotation;
            particle.SetActive(true);
            
            var pfx = particle.GetComponent<ParticleSystem>();
            var main = pfx.main;
            main.startColor = _particleColor;
            pfx.Play();
            
            GameUtilities.instance.DelayExecute(() =>
            {
                pfx.Stop();
                particle.SetActive(false);
            }, 5f);
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
