using System;
using System.Collections;
using UnityEngine;

public class Utilities : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    
    public static Utilities Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void DelayExecute(Action action, float delay)
    {
        StartCoroutine(DelayExec(action, delay));
    }

    IEnumerator DelayExec(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Vector3 eulerAngles, Transform parent)
    {
        var thing = Instantiate(prefab, parent);
        thing.transform.position = position;
        thing.transform.eulerAngles = eulerAngles;

        return thing;
    }

    public GameObject GetProjectilePrefab()
    {
        return projectilePrefab;
    }
}