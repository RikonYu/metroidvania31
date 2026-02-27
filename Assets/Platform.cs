using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    public bool IsFreezeable;
    public float MoveSpeed;
    protected Vector3 StartPos;
    public WaypointMaster wm;
    private int currentWaypointIndex = 0;
    private List<Vector3> waypoints;
    public float waypointTolerance = 0.5f;
    private Vector3 moveInput;

    // Start is called before the first frame update
    void Start()
    {
        if (wm != null)
        {
            waypoints = wm.GetWaypoints();
        }
    }
    void CalculateMovement(Vector3 targetPos)
    {
        Vector2 direction = (targetPos - transform.position).normalized;
        moveInput = direction * MoveSpeed;

    }

    public Vector2 GetVelocity()
    {
        if (ft >= 0f) return Vector2.zero;

        return new Vector2(moveInput.x, moveInput.y);
    }
    // Update is called once per frame
    void Update()
    {
        
        if (ft >= 0f)
        {
            ft -= Time.deltaTime;
            return;

        }
        if (waypoints == null || waypoints.Count == 0) return;

        Vector3 targetPoint = waypoints[currentWaypointIndex];
        CalculateMovement(targetPoint);

        float distToWaypoint = Vector2.Distance(transform.position, targetPoint);
        if (distToWaypoint < Time.deltaTime * MoveSpeed)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
        else
            transform.position += Time.deltaTime * moveInput;
    }
    float ft;
    public void Freeze(float t)
    {
        ft = t;
    }
}
