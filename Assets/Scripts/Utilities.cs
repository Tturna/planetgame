using System;
using System.Collections;
using UnityEngine;

public class Utilities : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    
    public static Utilities instance;

    private void Awake()
    {
        instance = this;
    }

    public void DelayExecute(Action action, float delay)
    {
        StartCoroutine(DelayExec(action, delay));
    }

    private IEnumerator DelayExec(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Vector3 eulerAngles, Transform parent)
    {
        var thing = Instantiate(prefab, parent);
        thing.transform.position = position;
        thing.transform.eulerAngles = eulerAngles;

        return thing;
    }
    
    public static float InverseLerp(float a, float b, float v)
    {
        return b - a == 0 ? 0f : Mathf.Clamp01((v - a) / (b - a));
    }

    public static float Remap(float oa, float ob, float na, float nb, float v)
    {
        var t = InverseLerp(oa, ob, v);
        return Mathf.Lerp(na, nb, t);
    }

    public GameObject GetProjectilePrefab()
    {
        return projectilePrefab;
    }
}