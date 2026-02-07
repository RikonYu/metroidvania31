using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Tooltip("到达此点后的停留时间")]
    public float pauseTime = 0f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f,0f,0f,0f);
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}