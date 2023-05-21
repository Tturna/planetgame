using UnityEngine;
using Utilities;

public class Planet : MonoBehaviour
{
    public  float radius;
    public Sprite surfaceCameraBackground;
    public Color surfaceBackgroundColor;
    
    [SerializeField] private float atmosphereRadius;
    [SerializeField] private float maxDrag;
    [SerializeField] private float maxGravityMultiplier;
    [SerializeField, Range(0, 1),
     Tooltip("Distance percentage at which max drag and gravity is reached. 0 is at the edge of the atmosphere, 1 is at the center of the core")]
    private float threshold;
    

    private void Start()
    {
        var atmosphereCollider = gameObject.AddComponent<CircleCollider2D>();
        // atmosphereCollider.radius = 1.28f * atmosphereRadius / radius;
        atmosphereCollider.radius = atmosphereRadius;
        atmosphereCollider.isTrigger = true;
    }

    /// <summary>
    /// Get the distance of the given position from the center of the planet. 1 = core, 0 = edge of atmosphere
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public float GetDistancePercentage(Vector3 position)
    {
        var distanceFromCore = (position - transform.position).magnitude;
        var perc = distanceFromCore / atmosphereRadius;
        var rev = 1 - perc;
        // var limited = rev / threshold;
        
        return Mathf.Clamp01(rev);
    }

    public float GetDrag(Vector3 position)
    {
        var perc = GetDistancePercentage(position);
        var limitedPerc = GameUtilities.InverseLerp(0f, threshold, perc);
        return maxDrag * limitedPerc;
    }

    public float GetGravity(Vector3 position)
    {
        var perc = GetDistancePercentage(position);
        var limitedPerc = GameUtilities.InverseLerp(0f, threshold, perc);
        return maxGravityMultiplier * limitedPerc;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, atmosphereRadius);
    }
}
