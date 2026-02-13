using UnityEngine;
using System.Collections.Generic;

public class WaypointMaster : MonoBehaviour
{
    public Color pathColor = Color.cyan;
    public bool loop = true;

    public List<Transform> GetWaypoints()
    {
        List<Transform> points = new List<Transform>();
        foreach (Transform child in transform)
        {
            points.Add(child);
        }
        return points;
    }

    private void OnDrawGizmos()
    {
        List<Transform> points = GetWaypoints();
        //if (points.Count < 2) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawSphere(points[i].position, 0.2f);

            if (i < points.Count - 1)
            {
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }
    }
}