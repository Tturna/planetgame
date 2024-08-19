using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Entities
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerDeathManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Sprites to scatter around when player dies." +
                                 "The player's skull should be the first element")]
        private Sprite[] deathSprites;
        [SerializeField] private GameObject deathFxObjectPrefab;
        [SerializeField] private ParticleSystem deathParticles;
        
        public static PlayerDeathManager instance;
        public const int DefaultRespawnDelay = 10;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }
        }
        
        public GameObject Explode(float respawnDelay = DefaultRespawnDelay)
        {
            deathParticles.Play();
            GameObject skullObject = null;
            List<GameObject> deathFxObjects = new();
            
            for (var i = 0; i < deathSprites.Length; i++)
            {
                var iterations = i == 0 ? 1 : Random.Range(5, 10);

                // Spawns a random amount of death fx objects except for the player skull.
                // Assumes the player skull is the first element in deathSprites.
                for (var j = 0; j < iterations; j++)
                {
                    var deathFxObject = Instantiate(deathFxObjectPrefab, transform.position, transform.rotation);
                    deathFxObject.GetComponent<SpriteRenderer>().sprite = deathSprites[i];
                    deathFxObject.GetComponent<CircleCollider2D>().radius = deathSprites[i].bounds.size.x / 2;
                    deathFxObject.GetComponent<EntityController>().ToggleAutoRotation(false);
                    deathFxObject.layer = LayerMask.NameToLayer("Enemy");
                    deathFxObjects.Add(deathFxObject);
                    
                    var rb = deathFxObject.GetComponent<Rigidbody2D>();
                    rb.angularDrag = 0f;
                    rb.AddRelativeForce(Vector2.up * Random.Range(8, 16), ForceMode2D.Impulse);
                    rb.AddRelativeForce(Vector2.right * Random.Range(-10, 10), ForceMode2D.Impulse);
                    rb.AddTorque(Random.Range(-3, 3), ForceMode2D.Impulse);
                    
                    if (i == 0)
                    {
                        skullObject = deathFxObject;
                    }
                }
            }
            
            // Destroy fx objects after respawn delay + 1 so that we don't destroy the camera
            // that's attached to the skull
            GameUtilities.instance.DelayExecute(() =>
            {
                for (var i = 0; i < deathFxObjects.Count; i++)
                {
                    Destroy(deathFxObjects[i]);
                }
                
                deathFxObjects.Clear();
            }, respawnDelay + 1f);

            return skullObject;
        }
    }
}