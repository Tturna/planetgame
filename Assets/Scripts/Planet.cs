using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] private float radius;
    [SerializeField] private float atmosphereRadius;
    [SerializeField] private float maxDrag;
    [SerializeField] private float maxGravityMultiplier;
    [SerializeField, Range(0, 1),
     Tooltip("Distance percentage at which max drag and gravity is reached. 0 is at the edge of the atmosphere, 1 is at the center of the core")]
    private float threshold;

    private void Start()
    {
        var atmosphereCollider = gameObject.AddComponent<CircleCollider2D>();
        atmosphereCollider.radius = 1.28f * atmosphereRadius / radius;
        atmosphereCollider.isTrigger = true;
    }

    private float GetDistancePercentage(Vector3 position)
    {
        var distanceFromCore = (position - transform.position).magnitude;
        var perc = distanceFromCore / atmosphereRadius;
        var rev = 1 - perc;
        var limited = rev / threshold;
        
        return Mathf.Clamp01(limited);
    }

    public float GetDrag(Vector3 position)
    {
        return maxDrag * GetDistancePercentage(position);
    }

    public float GetGravity(Vector3 position)
    {
        return maxGravityMultiplier * GetDistancePercentage(position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, atmosphereRadius);
    }
}
