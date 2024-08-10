using System;
using System.Collections;
using Cameras;
using TMPro;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Entities
{
    public class DamageNumberManager : MonoBehaviour
    {
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Material flashMaterial, normalMaterial;
        
        private const string DamageNumberPoolName = "Damage Number Pool";
        private bool objectPoolCreated;

        private void Start()
        {
            InitializeDamageNumberObjectPool();
        }
        
        private void InitializeDamageNumberObjectPool()
        {
            if (objectPoolCreated) return;
            ObjectPooler.CreatePoolIfDoesntExist(DamageNumberPoolName, damageNumberPrefab, 20);
            objectPoolCreated = true;
        }

        public void CreateDamageNumber(float amount, float lifeTime = 1f, float startMoveStrength = 2f)
        {
            InitializeDamageNumberObjectPool();
            
            var damageNumberObject = ObjectPooler.GetObject(DamageNumberPoolName);
            
            if (damageNumberObject == null)
            {
                throw new NullReferenceException("Damage number object is null!");
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
            tmp.fontSharedMaterial = flashMaterial;
            
            var ogLifeTime = lifeTime;
            var camTr = CameraController.instance.mainCam.transform;
            
            while (lifeTime > 0)
            {
                tr.rotation = camTr.rotation;
                
                var nLifeTime = lifeTime / ogLifeTime;
                var moveStrength = Mathf.Lerp(0f, startMoveStrength, nLifeTime);
                tr.Translate(camTr.transform.up * (Time.deltaTime * moveStrength));
                
                var scaleX = Mathf.Lerp(1f, 2.5f, (nLifeTime - .8f) * 5f) + nLifeTime * nLifeTime * .5f;
                var scaleY = Mathf.Lerp(1f, .5f, nLifeTime) + nLifeTime * nLifeTime * .5f;
                tr.localScale = new Vector3(scaleX, scaleY, 1f);
               
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, nLifeTime * 2f);
                
                if (nLifeTime < .875f)
                {
                    tmp.fontSharedMaterial = normalMaterial;
                }
                
                lifeTime -= Time.deltaTime;
                yield return null;
            }
            
            damageNumberObject.SetActive(false);
        }
    }
}
