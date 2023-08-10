using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Utilities;

namespace Entities.Entities
{
    public class DamageNumberManager : MonoBehaviour
    {
        [SerializeField] private GameObject damageNumberPrefab;

        private static Guid _damageNumberPoolId = Guid.Empty;
        
        private void Start()
        {
            InitializeDamageNumberObjectPool();
        }
        
        private void InitializeDamageNumberObjectPool()
        {
            if (_damageNumberPoolId != Guid.Empty) return;
            
            _damageNumberPoolId = ObjectPooler.CreatePool(damageNumberPrefab, 20);
            Debug.Log($"Created damage number pool with ID {_damageNumberPoolId}");
        }

        public void CreateDamageNumber(float amount, float lifeTime = 1f, float startMoveStrength = 1f)
        {
            InitializeDamageNumberObjectPool();
            
            var damageNumberObject = ObjectPooler.GetObject(_damageNumberPoolId);
            
            if (damageNumberObject == null)
            {
                Debug.LogError("Damage number object is null!");
                return;
            }
            
            damageNumberObject.transform.position = transform.position;
            
            var tmp = damageNumberObject.GetComponent<TextMeshPro>();

            if (tmp == null)
            {
                Debug.LogError("Damage number object does not have a TextMeshPro component!");
            }
            else
            {
                tmp.text = amount.ToString();
            }
            
            GameUtilities.instance.StartCoroutine(HandleDamageNumberLifeTime(damageNumberObject, tmp, lifeTime, startMoveStrength));
        }

        private static IEnumerator HandleDamageNumberLifeTime(GameObject damageNumberObject, TextMeshPro tmp, float lifeTime, float startMoveStrength)
        {
            var tr = damageNumberObject.transform;
            var ogLifeTime = lifeTime;
            while (lifeTime > 0)
            {
                // TODO: More damage number effects
                // Stretch and squeeze
                // Scale?
                // Fade after a certain amount of time/distance
                // Slightly randomized position
                
                var moveStrength = Mathf.Lerp(0f, startMoveStrength, lifeTime / ogLifeTime);
                tr.Translate(tr.up * (Time.deltaTime * moveStrength));
                
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, lifeTime / ogLifeTime);
                
                lifeTime -= Time.deltaTime;
                yield return null;
            }
            
            damageNumberObject.SetActive(false);
        }
    }
}
