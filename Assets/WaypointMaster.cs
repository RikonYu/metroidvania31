using UnityEngine;
using System.Collections.Generic;

public class WaypointMaster : MonoBehaviour
{
    public Color pathColor = Color.cyan;
    public bool loop = true;

    public List<Vector3> GetWaypoints()
    {
        List<Vector3> points = new List<Vector3>();
        foreach (Transform child in transform)
        {
            points.Add(child.position);
        }
        return points;
    }

    private void OnDrawGizmos()
    {
        List<Vector3> points = GetWaypoints();
        //if (points.Count < 2) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawSphere(points[i], 0.2f);

            if (i < points.Count - 1)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }
    }
}