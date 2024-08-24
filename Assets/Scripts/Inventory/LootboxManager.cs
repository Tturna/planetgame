using Cameras;
using Entities;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Inventory
{
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(Animator))]
    public class LootboxManager : MonoBehaviour
    {
        [SerializeField] private bool dropMultiple;
        [SerializeField] private LootDrop[] items;
        public bool isTrap;
        private Animator _animator;
        
        void Start()
        {
            GetComponent<Interactable>().OnInteractImmediate += OpenLootbox;
            _animator = GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            GetComponent<Interactable>().OnInteractImmediate -= OpenLootbox;
        }

        private void OpenLootbox(GameObject sourceObject)
        {
            if (isTrap)
            {
                _animator.SetTrigger("explode");
                var pfx = GetComponentInChildren<ParticleSystem>();
                var pfxObject = pfx.gameObject;
                pfxObject.transform.SetParent(null);
                
                GameUtilities.instance.DelayExecute(() =>
                {
                    pfx.Play();
                    CameraController.CameraShake(0.33f, 0.2f);
                    var distanceToPlayer = Vector3.Distance(PlayerController.instance.transform.position, transform.position);
                    
                    if (distanceToPlayer < 4f)
                    {
                        PlayerController.instance.TakeDamage(33f, transform.position);
                        PlayerController.instance.Knockback(transform.position, 20f);
                    }
                    
                    Destroy(gameObject);
                }, 1f);
                
                return;
            }
            
            foreach (var item in items)
            {
                var rng = Random.Range(0f, 100f);
                
                if (rng > item.dropChance) continue;

                var position = transform.position + (Vector3)Random.insideUnitCircle + Vector3.up;
                var itemDrop = InventoryManager.SpawnItem(new Item(item.item), position);

                if (itemDrop.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    var forceDirection = (Vector3.up + Random.insideUnitSphere).normalized;
                    rb.AddForce(forceDirection * Random.Range(1f, 5f), ForceMode2D.Impulse);
                }
                
                if (!dropMultiple) break;
            }
            
            Destroy(gameObject);
        }
        
        public void Init(LootDrop[] initItems, bool initIsTrap)
        {
            items = initItems;
            dropMultiple = true;
            isTrap = initIsTrap;

            if (isTrap)
            {
                if (!_animator)
                {
                    _animator = GetComponent<Animator>();
                }
                
                _animator.SetBool("isTrap", true);
            }
        }
    }
}
