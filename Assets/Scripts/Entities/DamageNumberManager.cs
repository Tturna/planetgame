using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Entities
{
    public class DamageNumberManager : MonoBehaviour
    {
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private TMP_FontAsset flashFont, normalFont;

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
                // Every time a damage number is created, it will get an equal or higher sorting order than the previous one
                // 10 is min, 30 is max, 2 is time multiplier that can be increased to make the numbers change sorting order faster
                tmp.sortingOrder = 10 + (int)(Time.time * 2) % 20;
            }
            
            GameUtilities.instance.StartCoroutine(HandleDamageNumberLifeTime(damageNumberObject, tmp, lifeTime, startMoveStrength));
        }

        private IEnumerator HandleDamageNumberLifeTime(GameObject damageNumberObject, TextMeshPro tmp, float lifeTime, float startMoveStrength)
        {
            var tr = damageNumberObject.transform;
            tr.Translate(Random.insideUnitCircle * .5f);
            tmp.font = flashFont;
            
            var ogLifeTime = lifeTime;
            var camTr = Camera.main!.transform;
            
            while (lifeTime > 0)
            {
                tr.rotation = camTr.rotation;
                
                var nLifeTime = lifeTime / ogLifeTime;
                var moveStrength = Mathf.Lerp(0f, startMoveStrength, nLifeTime);
                tr.Translate(camTr.transform.up * (Time.deltaTime * moveStrength));
                
                var scaleX = Mathf.Lerp(1f, 1.7f, (nLifeTime - .75f) * 4f) + nLifeTime * .35f;
                var scaleY = Mathf.Lerp(1f, .35f, nLifeTime) + nLifeTime * .35f;
                tr.localScale = new Vector3(scaleX, scaleY, 1f);
               
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, nLifeTime * 2f);
                
                if (nLifeTime < .9f)
                {
                    tmp.font = normalFont;
                }
                
                lifeTime -= Time.deltaTime;
                yield return null;
            }
            
            damageNumberObject.SetActive(false);
        }
    }
}
