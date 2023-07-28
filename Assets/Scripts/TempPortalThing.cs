using UnityEngine;

public class TempPortalThing : MonoBehaviour
{
    [SerializeField] private GameObject outer;
    [SerializeField] private GameObject innerone;
    [SerializeField] private GameObject innertwo;
    [SerializeField] private float speed;
    [SerializeField] private float pulseSpeed;

    private Vector3 og1, og2, og3;
    
    void Start()
    {
        og1 = outer.transform.localScale;
        og2 = innerone.transform.localScale;
        og3 = innertwo.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        outer.transform.Rotate(Vector3.back, Time.deltaTime * speed);
        innerone.transform.Rotate(Vector3.back, Time.deltaTime * speed);
        innertwo.transform.Rotate(Vector3.forward, Time.deltaTime * speed);

        var sin = Mathf.Sin(Time.time * pulseSpeed);
        var cos = Mathf.Cos(Time.time * pulseSpeed);
        var sin2 = Mathf.Sin((Time.time + .5f) * pulseSpeed);

        outer.transform.localScale = og1 + Vector3.one * ((sin + 1) * .1f - .15f);
        innerone.transform.localScale = og2 + Vector3.one * (cos * .2f);
        innertwo.transform.localScale = og3 + Vector3.one * (sin2 * .1f);
    }
}
